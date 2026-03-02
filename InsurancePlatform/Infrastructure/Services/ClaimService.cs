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

        public ClaimService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorage)
        {
            _context = context;
            _userManager = userManager;
            _fileStorage = fileStorage;
        }

        // code for customer to tell about a problem and ask for money
        public async Task<object> RaiseClaimAsync(string userId, RaiseClaimRequest request)
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

            return new { Status = "Success", ClaimId = claim.Id };
        }

        public async Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.Policy)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetClaimOfficersWithWorkloadAsync()
        {
            var officers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer);
            var result = new List<object>();

            foreach (var officer in officers)
            {
                var count = await _context.InsuranceClaims.CountAsync(c => c.AssignedClaimOfficerId == officer.Id);
                result.Add(new
                {
                    ClaimOfficerId = officer.Id,
                    Email = officer.Email,
                    AssignedClaimsCount = count
                });
            }

            return result;
        }

        public async Task<bool> AssignClaimOfficerAsync(string claimId, string officerId)
        {
            var claim = await _context.InsuranceClaims.FindAsync(claimId);
            if (claim == null) return false;

            claim.AssignedClaimOfficerId = officerId;
            claim.Status = "Assigned";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Where(c => c.AssignedClaimOfficerId == officerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0)
        {
            var claim = await _context.InsuranceClaims
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null || claim.AssignedClaimOfficerId != officerId) return false;

            if (status == "Approved")
            {
                if (claim.Policy == null) throw new Exception("Associated policy not found.");
                
                // Validate against remaining coverage
                if (approvedAmount > claim.Policy.RemainingCoverageAmount)
                {
                    throw new Exception($"Approved amount (₹{approvedAmount}) exceeds remaining coverage (₹{claim.Policy.RemainingCoverageAmount}).");
                }

                claim.ApprovedAmount = approvedAmount;
                
                // Update Policy Financials
                claim.Policy.TotalApprovedClaimsAmount += approvedAmount;
                claim.Policy.RemainingCoverageAmount -= approvedAmount;
            }

            claim.Status = status; // Approved or Rejected
            claim.Remarks = remarks;
            claim.ApprovedByOfficerId = officerId;
            claim.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .Where(c => c.Policy.AssignedAgentId == agentId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            var stats = new AdminDashboardStatsDto
            {
                TotalCustomers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer).ContinueWith(t => t.Result.Count),
                TotalPolicies = await _context.PolicyApplications.CountAsync(),
                TotalClaims = await _context.InsuranceClaims.CountAsync(),
                TotalClaimedAmount = await _context.InsuranceClaims
                    .Where(c => c.Status == "Approved" || c.Status == "Paid")
                    .SumAsync(c => (decimal?)c.ApprovedAmount) ?? 0
            };
            return stats;
        }

        public async Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .FirstOrDefaultAsync(c => c.PolicyApplicationId == policyId);
        }
    }
}
