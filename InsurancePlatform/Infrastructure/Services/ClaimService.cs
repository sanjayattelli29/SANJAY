using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    // this class manages all the logic for insurance claims
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;

        public ClaimService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorage,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
        }

        // code for customer to tell about a problem and ask for money
        public async Task<AuthResponseDto> RaiseClaimAsync(string userId, RaiseClaimRequest request)
        {
            // check if policy is real and belongs to user
            var policy = await _context.PolicyApplications
                .FirstOrDefaultAsync(pa => pa.Id == request.PolicyApplicationId && pa.UserId == userId);

            if (policy == null) throw new Exception("Policy not found.");
            if (policy.Status != "Active") throw new Exception("Claims can only be raised for Active policies.");
            if (policy.ExpiryDate < DateTime.UtcNow) throw new Exception("Policy has expired.");

            // create a new claim record
            var claim = new InsuranceClaim
            {
                PolicyApplicationId = request.PolicyApplicationId,
                UserId = userId,
                IncidentType = request.IncidentType,
                IncidentLocation = request.IncidentLocation,
                IncidentDate = request.IncidentDate,
                Description = request.Description,
                HospitalName = request.HospitalName,
                HospitalizationRequired = request.HospitalizationRequired,
                RequestedAmount = request.RequestedAmount,
                AffectedMemberName = request.AffectedMemberName,
                AffectedMemberRelation = request.AffectedMemberRelation,
                Status = "Pending"
            };

            _context.InsuranceClaims.Add(claim);
            await _context.SaveChangesAsync(); 

            // Notify user about claim submission
            await _notificationService.SendNotificationAsync(userId, "Claim Raised", 
                $"Your claim for {request.IncidentType} has been raised successfully.", $"CUST:Claim:{claim.Id}");

            // Get user email for display
            var user = await _userManager.FindByIdAsync(userId);
            string userEmail = user?.Email ?? userId;

            // Notify only Admins to assign an officer to this new claim
            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            
            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationAsync(admin.Id, "New Claim Raised 🏥", 
                    $"A new claim for {request.IncidentType} has been raised by {userEmail}. (ID: {claim.Id.ToString().Substring(0, 3).ToUpper()}...)", $"ADM:Claim:{claim.Id}");
            }

            // save uploaded documents if any
            if (request.Documents != null && request.Documents.Any())
            {
                foreach (var file in request.Documents)
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var uploadResult = await _fileStorage.UploadFileAsync(stream, file.FileName, $"/claims/{claim.Id}");
                        
                        var doc = new ClaimDocument
                        {
                            ClaimId = claim.Id,
                            FileId = uploadResult.FileId,
                            FileName = uploadResult.FileName,
                            FileUrl = uploadResult.FileUrl,
                            FileSize = uploadResult.FileSize
                        };
                        _context.ClaimDocuments.Add(doc);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return new AuthResponseDto { Status = "Success", Message = claim.Id.ToString() };
        }

        // customer sees all their previous and current claims
        public async Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.Policy)  // load policy details
                .Include(c => c.Documents)  // load uploaded documents
                .Include(c => c.AssignedOfficer)  // load officer details
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.SubmissionDate)  // newest first
                .ToListAsync();
        }

        // admin sees claims that haven't been assigned to anyone yet
        public async Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)  // who filed the claim
                .Include(c => c.Policy)  // which policy it's for
                .Where(c => c.Status == "Pending")  // not assigned yet
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // shows which officer has how many claims so admin can balance work
        public async Task<IEnumerable<ClaimOfficerWorkloadDto>> GetClaimOfficersWithWorkloadAsync()
        {
            // get all users who are claim officers
            var officers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer);
            var result = new List<ClaimOfficerWorkloadDto>();

            // for each officer count how many claims they're handling
            foreach (var officer in officers)
            {
                var count = await _context.InsuranceClaims.CountAsync(c => c.AssignedClaimOfficerId == officer.Id);
                result.Add(new ClaimOfficerWorkloadDto
                {
                    ClaimOfficerId = officer.Id,
                    Email = officer.Email,
                    AssignedClaimsCount = count
                });
            }

            return result;
        }

        // admin gives a claim to a specific officer to investigate
        public async Task<bool> AssignClaimOfficerAsync(string claimId, string officerId)
        {
            // find the claim in database
            var claim = await _context.InsuranceClaims.FindAsync(claimId);
            if (claim == null) return false;

            // link officer to this claim
            claim.AssignedClaimOfficerId = officerId;
            claim.Status = "Assigned";  // update status
            await _context.SaveChangesAsync();

            // get more details for notifications
            var claimDetails = await _context.InsuranceClaims
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(c => c.Id == claimId);
            
            if (claimDetails != null)
            {
                var officer = await _userManager.FindByIdAsync(officerId);
                var officerName = officer?.Email ?? "A claim officer";

                // tell customer someone is now checking their claim
                await _notificationService.SendNotificationAsync(claimDetails.UserId, "Claim Officer Assigned", 
                    $"{officerName} has been assigned to review your claim for {claimDetails.IncidentType}.", $"CUST:Claim:{claimId}");
                
                // tell officer they got a new claim to review
                await _notificationService.SendNotificationAsync(officerId, "New Claim Assignment", 
                    $"You have been assigned a new claim from {claimDetails.User?.Email ?? claimDetails.UserId} for {claimDetails.IncidentType}.", $"OFF:Claim:{claimId}");
            }

            return true;
        }

        // officer sees all claims assigned to them for review
        public async Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)  // customer who filed it
                .Include(c => c.Policy)  // the insurance policy
                    .ThenInclude(p => p.AssignedAgent)  // the agent who sold it
                .Include(c => c.Documents)  // uploaded proof documents
                .Where(c => c.AssignedClaimOfficerId == officerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // officer decides whether to approve or reject the claim
        public async Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0)
        {
            // find claim and make sure it's assigned to this officer
            var claim = await _context.InsuranceClaims
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null || claim.AssignedClaimOfficerId != officerId) return false;

            // if approving the claim
            if (status == "Approved")
            {
                if (claim.Policy == null) throw new Exception("Associated policy not found.");
                
                // make sure approved amount doesn't exceed remaining coverage
                if (approvedAmount > claim.Policy.RemainingCoverageAmount)
                {
                    throw new Exception($"Approved amount (₹{approvedAmount}) exceeds remaining coverage (₹{claim.Policy.RemainingCoverageAmount}).");
                }

                claim.ApprovedAmount = approvedAmount;
                
                // update policy's money tracking
                claim.Policy.TotalApprovedClaimsAmount += approvedAmount;
                claim.Policy.RemainingCoverageAmount -= approvedAmount;  // reduce available coverage
            }

            // save the decision
            claim.Status = status;  // Approved or Rejected
            claim.Remarks = remarks;  // officer's notes
            claim.ApprovedByOfficerId = officerId;
            claim.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // tell customer about the decision
            string title = status == "Approved" ? "Claim Approved ✅" : "Claim Rejected ❌";
            string displayId = claimId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been approved for {approvedAmount:C}." : 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been rejected.";
            
            await _notificationService.SendNotificationAsync(claim.UserId, title, message, $"Claim:{claimId}");

            return true;
        }

        // agent can see claims filed by their customers
        public async Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)  // customer details
                .Include(c => c.Policy)  // policy details
                    .ThenInclude(p => p.AssignedAgent)  // agent details
                .Include(c => c.Documents)  // claim documents
                .Include(c => c.AssignedOfficer)  // who's reviewing it
                .Where(c => c.Policy.AssignedAgentId == agentId)  // only their customers
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // admin can see every single claim in the system
        public async Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)  // who filed it
                .Include(c => c.Policy)  // which policy
                    .ThenInclude(p => p.AssignedAgent)  // which agent
                .Include(c => c.Documents)  // all documents
                .Include(c => c.AssignedOfficer)  // who's checking it
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // calculates overall statistics for admin dashboard
        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            var stats = new AdminDashboardStatsDto
            {
                // count total users by role
                TotalCustomers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer).ContinueWith(t => t.Result.Count),
                TotalAgents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent).ContinueWith(t => t.Result.Count),
                TotalClaimOfficers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer).ContinueWith(t => t.Result.Count),
                
                // system wide volume
                TotalPolicies = await _context.PolicyApplications.CountAsync(),
                TotalClaims = await _context.InsuranceClaims.CountAsync(),
                
                // financial audit numbers
                TotalClaimedAmount = await _context.InsuranceClaims
                    .Where(c => c.Status == "Approved" || c.Status == "Paid")
                    .SumAsync(c => (decimal?)c.ApprovedAmount) ?? 0,
                    
                TotalCoverageRaised = await _context.PolicyApplications
                    .SumAsync(pa => (decimal?)pa.TotalCoverageAmount) ?? 0,
                    
                TotalPremiumCollected = await _context.PolicyApplications
                    .Where(pa => pa.Status == "Active" || pa.Status == "AwaitingPayment")
                    .SumAsync(pa => (decimal?)pa.PaidAmount) ?? 0
            };
            return stats;
        }

        // finds claim details for a specific insurance policy
        public async Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)  // customer info
                .Include(c => c.Policy)  // policy info
                    .ThenInclude(p => p.AssignedAgent)  // agent info
                .Include(c => c.Documents)  // claim documents
                .Include(c => c.AssignedOfficer)  // officer info
                .FirstOrDefaultAsync(c => c.PolicyApplicationId == policyId);
        }
    }
}