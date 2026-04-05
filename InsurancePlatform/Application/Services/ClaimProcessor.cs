using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
namespace Application.Services
{
    /// <summary>
    /// This is the "Engine" that manages all insurance claims.
    /// It handles everything from a customer asking for money (Raising a claim)
    /// to an officer approving or rejecting that request.
    /// </summary>
    public class ClaimProcessor : IClaimProcessor
    {
        // These are the tools the processor uses to talk to the database, store files, and send alerts.
        private readonly IClaimRepository _claimRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorageService _fileStorage;
        private readonly ISystemNotifier _systemNotifier;

        public ClaimProcessor(
            IClaimRepository claimRepository,
            IPolicyRepository policyRepository,
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorage,
            ISystemNotifier systemNotifier)
        {
            _claimRepository = claimRepository;
            _policyRepository = policyRepository;
            _userManager = userManager;
            _fileStorage = fileStorage;
            _systemNotifier = systemNotifier;
        }

        /// <summary>
        /// This method starts a new claim for a customer.
        /// It checks if the policy is valid and active before saving the claim details.
        /// </summary>
        public async Task<AuthResponseDto> RaiseClaimAsync(string userId, RaiseClaimRequest request)
        {
            // 1. First, find the policy and make sure it belongs to the user and is still active.
            var policy = await _policyRepository.GetByIdAsync(request.PolicyApplicationId, false);
            if (policy == null || policy.UserId != userId) throw new Exception("Policy not found.");
            if (policy.Status != "Active") throw new Exception("Claims can only be raised for Active policies.");
            if (policy.ExpiryDate < DateTime.UtcNow) throw new Exception("Policy has expired.");

            // 2. Make sure the accident/incident didn't happen before the policy even started.
            if (policy.StartDate.HasValue && request.IncidentDate.Date < policy.StartDate.Value.Date)
            {
                throw new Exception($"Incident date cannot be before the policy start date ({policy.StartDate.Value:yyyy-MM-dd}).");
            }

            // 3. Create the claim record with all the details about what happened.
            var claim = new InsuranceClaim
            {
                PolicyApplicationId = request.PolicyApplicationId,
                UserId = userId,
                IncidentType = request.IncidentType,
                IncidentLocation = request.IncidentLocation,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IncidentDate = request.IncidentDate,
                Description = request.Description,
                HospitalName = request.HospitalName,
                HospitalizationRequired = request.HospitalizationRequired,
                RequestedAmount = request.RequestedAmount,
                AffectedMemberName = request.AffectedMemberName,
                AffectedMemberRelation = request.AffectedMemberRelation,
                IncidentTime = request.IncidentTime,
                AccidentCause = request.AccidentCause,
                PoliceCaseFiled = request.PoliceCaseFiled,
                FirNumber = request.FirNumber,
                InjuryType = request.InjuryType,
                BodyPartInjured = request.BodyPartInjured,
                AdmissionDate = request.AdmissionDate,
                DischargeDate = request.DischargeDate,
                EstimatedMedicalCost = request.EstimatedMedicalCost,
                HospitalBill = request.HospitalBill,
                Medicines = request.Medicines,
                OtherExpenses = request.OtherExpenses,
                Status = "Pending"
            };

            // 4. Save the claim to the database.
            await _claimRepository.AddAsync(claim);
            await _claimRepository.SaveChangesAsync();

            // 5. Send a notification to the customer and all admins about the new claim.
            await _systemNotifier.SendNotificationAsync(userId, "Claim Raised", 
                $"Your claim for {request.IncidentType} has been raised successfully.", $"CUST:Claim:{claim.Id}");

            var user = await _userManager.FindByIdAsync(userId);
            string userEmail = user?.Email ?? userId;

            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            foreach (var admin in admins)
            {
                await _systemNotifier.SendNotificationAsync(admin.Id, "New Claim Raised 🏥", 
                    $"A new claim for {request.IncidentType} has been raised by {userEmail}. (ID: {claim.Id.ToString().Substring(0, 3).ToUpper()}...)", $"ADM:Claim:{claim.Id}");
            }

            // 6. If the user uploaded any photos or PDFs (like hospital bills), save them now.
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
                        await _claimRepository.AddDocumentAsync(doc);
                    }
                }
                await _claimRepository.SaveChangesAsync();
            }

            return new AuthResponseDto { Status = "Success", Message = claim.Id.ToString() };
        }

        // Get all claims for a specific customer.
        public async Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId)
        {
            return await _claimRepository.GetByUserIdAsync(userId);
        }

        // Get all claims that are waiting for an officer to be assigned.
        public async Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync()
        {
            return await _claimRepository.GetPendingClaimsAsync();
        }

        /// <summary>
        /// This method finds all Claim Officers and checks how many claims each one is currently handling.
        /// This helps the admin pick the officer with the least work.
        /// </summary>
        public async Task<IEnumerable<ClaimOfficerWorkloadDto>> GetClaimOfficersWithWorkloadAsync()
        {
            var officers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer);
            var result = new List<ClaimOfficerWorkloadDto>();

            foreach (var officer in officers)
            {
                var count = await _claimRepository.GetCountByOfficerAsync(officer.Id);
                result.Add(new ClaimOfficerWorkloadDto
                {
                    ClaimOfficerId = officer.Id,
                    Email = officer.Email,
                    AssignedClaimsCount = count
                });
            }

            return result;
        }

        /// <summary>
        /// This method assigns a specific Claim Officer to look at a claim.
        /// It also sends alerts to both the customer and the officer.
        /// </summary>
        public async Task<bool> AssignClaimOfficerAsync(string claimId, string officerId)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null) return false;

            claim.AssignedClaimOfficerId = officerId;
            claim.Status = "Assigned";
            
            await _claimRepository.UpdateAsync(claim);
            await _claimRepository.SaveChangesAsync();

            var officer = await _userManager.FindByIdAsync(officerId);
            var officerName = officer?.Email ?? "A claim officer";

            await _systemNotifier.SendNotificationAsync(claim.UserId, "Claim Officer Assigned", 
                $"{officerName} has been assigned to review your claim for {claim.IncidentType}.", $"CUST:Claim:{claimId}");
            
            await _systemNotifier.SendNotificationAsync(officerId, "New Claim Assignment", 
                $"You have been assigned a new claim from {claim.User?.Email ?? claim.UserId} for {claim.IncidentType}.", $"OFF:Claim:{claimId}");

            return true;
        }

        // Get all claims assigned to a specific officer.
        public async Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId)
        {
            return await _claimRepository.GetByOfficerIdAsync(officerId);
        }

        /// <summary>
        /// This is where the Officer makes the final decision to Approve or Reject a claim.
        /// If approved, it automatically deducts the money from the policy's remaining coverage.
        /// </summary>
        public async Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null || claim.AssignedClaimOfficerId != officerId) return false;

            if (status == "Approved")
            {
                if (claim.Policy == null) throw new Exception("Associated policy not found.");
                
                // Make sure we aren't paying out more money than the policy has left.
                if (approvedAmount > claim.Policy.RemainingCoverageAmount)
                {
                    throw new Exception($"Approved amount (₹{approvedAmount}) exceeds remaining coverage (₹{claim.Policy.RemainingCoverageAmount}).");
                }

                // Update the policy coverage numbers.
                claim.ApprovedAmount = approvedAmount;
                claim.Policy.TotalApprovedClaimsAmount += approvedAmount;
                claim.Policy.RemainingCoverageAmount -= approvedAmount;
                
                await _policyRepository.UpdateAsync(claim.Policy); 
            }

            // Save the officer's decision and notes.
            claim.Status = status;
            claim.Remarks = remarks;
            claim.ApprovedByOfficerId = officerId;
            claim.ProcessedAt = DateTime.UtcNow;

            await _claimRepository.UpdateAsync(claim);
            await _claimRepository.SaveChangesAsync();

            // Notify the customer about the decision.
            string title = status == "Approved" ? "Claim Approved ✅" : "Claim Rejected ❌";
            string displayId = claimId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been approved for {approvedAmount:C}." : 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been rejected.";
            
            await _systemNotifier.SendNotificationAsync(claim.UserId, title, message, $"Claim:{claimId}");

            return true;
        }

        // Get all claims filed by customers of a specific agent.
        public async Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId)
        {
            return await _claimRepository.GetByAgentIdAsync(agentId);
        }

        // Get every single claim in the system.
        public async Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync()
        {
            return await _claimRepository.GetAllClaimsAsync();
        }

        /// <summary>
        /// This method calculates all the "Big Board" statistics for the Admin dashboard.
        /// It counts customers, calculates total money paid, and prepares data for charts.
        /// </summary>
        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            // Fetch the raw data from the database.
            var allPolicies = await _policyRepository.GetAllApplicationsAsync();
            var allClaims = await _claimRepository.GetAllClaimsAsync();
            
            var customers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer);
            var agents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent);
            var claimOfficers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer);

            // Simple Counts and Sums.
            var stats = new AdminDashboardStatsDto
            {
                TotalCustomers = customers.Count,
                TotalAgents = agents.Count,
                TotalClaimOfficers = claimOfficers.Count,
                
                TotalPolicies = allPolicies.Count(),
                TotalClaims = allClaims.Count(),
                
                TotalClaimedAmount = allClaims.Where(c => c.Status == "Approved" || c.Status == "Paid").Sum(c => c.ApprovedAmount),
                TotalCoverageRaised = allPolicies.Sum(pa => pa.TotalCoverageAmount),
                TotalPremiumCollected = allPolicies.Where(pa => pa.Status == "Active" || pa.Status == "AwaitingPayment").Sum(pa => pa.PaidAmount ?? 0),
                TotalCommission = allPolicies.Where(pa => pa.Status == "Active").Sum(pa => pa.CalculatedPremium * 0.10m),

                // Breakdown how many items are in each status (Active, Pending, etc.)
                PolicyStatusDistribution = allPolicies.GroupBy(p => p.Status)
                    .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() }).ToList(),
                
                ClaimStatusDistribution = allClaims.GroupBy(c => c.Status)
                    .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() }).ToList(),

                PolicyCategoryDistribution = allPolicies.GroupBy(p => p.PolicyCategory)
                    .Select(g => new CategoryStatDto { Category = g.Key, Count = g.Count() }).ToList(),

                AgentPerformance = allPolicies.Where(p => p.AssignedAgent != null)
                    .GroupBy(p => p.AssignedAgent!.Email.Split('@', StringSplitOptions.None)[0])
                    .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(5)
                    .ToList()
            };

            // Prepare the "Policy Growth" chart data (last 6 months).
            stats.PolicyGrowth = allPolicies
                .GroupBy(pa => pa.SubmissionDate.ToString("MMM yyyy"))
                .OrderBy(g => g.Min(pa => pa.SubmissionDate))
                .Select(g => new StatPointDto { Label = g.Key, Value = g.Count() })
                .TakeLast(6)
                .ToList();

            // Prepare the "Revenue Trend" chart data (last 6 months).
            stats.RevenueTrends = allPolicies
                .Where(pa => pa.PaymentDate.HasValue)
                .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                .OrderBy(g => g.Min(pa => pa.PaymentDate))
                .Select(g => new StatPointDto { Label = g.Key, Value = g.Sum(pa => pa.PaidAmount ?? 0) })
                .TakeLast(6)
                .ToList();

            // Breakdown claims by what category they fall into.
            stats.ClaimsByCategory = allClaims
                .GroupBy(c => c.IncidentType)
                .Select(g => new CategoryStatDto { Category = g.Key, Count = g.Count() })
                .ToList();

            return stats;
        }

        // Find a claim using its Policy ID.
        public async Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId)
        {
            return await _claimRepository.GetByPolicyIdAsync(policyId);
        }

        // Save a web link to an AI-generated analysis of the claim.
        public async Task UpdateAnalysisUrlAsync(string claimId, string analysisUrl)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim != null)
            {
                claim.AnalysisReportUrl = analysisUrl;
                await _claimRepository.UpdateAsync(claim);
                await _claimRepository.SaveChangesAsync();
            }
        }
    }
}
