using Application.Interfaces.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Infrastructure;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ClaimProcessor : IClaimProcessor
    {
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

        public async Task<AuthResponseDto> RaiseClaimAsync(string userId, RaiseClaimRequest request)
        {
            var policy = await _policyRepository.GetByIdAsync(request.PolicyApplicationId, false);
            if (policy == null || policy.UserId != userId) throw new Exception("Policy not found.");
            if (policy.Status != "Active") throw new Exception("Claims can only be raised for Active policies.");
            if (policy.ExpiryDate < DateTime.UtcNow) throw new Exception("Policy has expired.");

            if (policy.StartDate.HasValue && request.IncidentDate.Date < policy.StartDate.Value.Date)
            {
                throw new Exception($"Incident date cannot be before the policy start date ({policy.StartDate.Value:yyyy-MM-dd}).");
            }

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

            await _claimRepository.AddAsync(claim);
            await _claimRepository.SaveChangesAsync();

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

        public async Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId)
        {
            return await _claimRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync()
        {
            return await _claimRepository.GetPendingClaimsAsync();
        }

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

        public async Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId)
        {
            return await _claimRepository.GetByOfficerIdAsync(officerId);
        }

        public async Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null || claim.AssignedClaimOfficerId != officerId) return false;

            if (status == "Approved")
            {
                if (claim.Policy == null) throw new Exception("Associated policy not found.");
                
                if (approvedAmount > claim.Policy.RemainingCoverageAmount)
                {
                    throw new Exception($"Approved amount (₹{approvedAmount}) exceeds remaining coverage (₹{claim.Policy.RemainingCoverageAmount}).");
                }

                claim.ApprovedAmount = approvedAmount;
                claim.Policy.TotalApprovedClaimsAmount += approvedAmount;
                claim.Policy.RemainingCoverageAmount -= approvedAmount;
                
                await _policyRepository.UpdateAsync(claim.Policy); 
            }

            claim.Status = status;
            claim.Remarks = remarks;
            claim.ApprovedByOfficerId = officerId;
            claim.ProcessedAt = DateTime.UtcNow;

            await _claimRepository.UpdateAsync(claim);
            await _claimRepository.SaveChangesAsync();

            string title = status == "Approved" ? "Claim Approved ✅" : "Claim Rejected ❌";
            string displayId = claimId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been approved for {approvedAmount:C}." : 
                $"Your claim for {claim.IncidentType} (ID: {displayId}...) has been rejected.";
            
            await _systemNotifier.SendNotificationAsync(claim.UserId, title, message, $"Claim:{claimId}");

            return true;
        }

        public async Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId)
        {
            return await _claimRepository.GetByAgentIdAsync(agentId);
        }

        public async Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync()
        {
            return await _claimRepository.GetAllClaimsAsync();
        }

        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            // Use projected queries or optimized counting instead of fetching full entities
            var allPolicies = await _policyRepository.GetAllApplicationsAsync();
            var allClaims = await _claimRepository.GetAllClaimsAsync();
            
            var stats = new AdminDashboardStatsDto
            {
                TotalCustomers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer).ContinueWith(t => t.Result.Count),
                TotalAgents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent).ContinueWith(t => t.Result.Count),
                TotalClaimOfficers = await _userManager.GetUsersInRoleAsync(UserRoles.ClaimOfficer).ContinueWith(t => t.Result.Count),
                
                TotalPolicies = allPolicies.Count(),
                TotalClaims = allClaims.Count(),
                
                TotalClaimedAmount = allClaims.Where(c => c.Status == "Approved" || c.Status == "Paid").Sum(c => c.ApprovedAmount),
                TotalCoverageRaised = allPolicies.Sum(pa => pa.TotalCoverageAmount),
                TotalPremiumCollected = allPolicies.Where(pa => pa.Status == "Active" || pa.Status == "AwaitingPayment").Sum(pa => pa.PaidAmount ?? 0),
                TotalCommission = allPolicies.Where(pa => pa.Status == "Active").Sum(pa => pa.CalculatedPremium * 0.10m),

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

            stats.PolicyGrowth = allPolicies
                .GroupBy(pa => pa.SubmissionDate.ToString("MMM yyyy"))
                .OrderBy(g => g.Min(pa => pa.SubmissionDate))
                .Select(g => new StatPointDto { Label = g.Key, Value = g.Count() })
                .TakeLast(6)
                .ToList();

            stats.RevenueTrends = allPolicies
                .Where(pa => pa.PaymentDate.HasValue)
                .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                .OrderBy(g => g.Min(pa => pa.PaymentDate))
                .Select(g => new StatPointDto { Label = g.Key, Value = g.Sum(pa => pa.PaidAmount ?? 0) })
                .TakeLast(6)
                .ToList();

            stats.ClaimsByCategory = allClaims
                .GroupBy(c => c.IncidentType)
                .Select(g => new CategoryStatDto { Category = g.Key, Count = g.Count() })
                .ToList();

            return stats;
        }

        public async Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId)
        {
            return await _claimRepository.GetByPolicyIdAsync(policyId);
        }
    }
}
