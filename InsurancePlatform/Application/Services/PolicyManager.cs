using Application.Interfaces.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Infrastructure;
using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Domain.Enums;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;

namespace Application.Services
{
    public class PolicyManager : IPolicyManager
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISystemNotifier _systemNotifier;
        private readonly IFileStorageService _fileStorage;
        private static PolicyConfiguration? _cachedConfig;

        public PolicyManager(
            IPolicyRepository policyRepository, 
            UserManager<ApplicationUser> userManager, 
            ISystemNotifier systemNotifier,
            IFileStorageService fileStorage)
        {
            _policyRepository = policyRepository;
            _userManager = userManager;
            _systemNotifier = systemNotifier;
            _fileStorage = fileStorage;
        }

        public async Task<PolicyConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfig != null) return _cachedConfig;

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "policy-config.json");
            
            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "policy-config.json");
            }

            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "policy-config.json");
            }

            var json = await File.ReadAllTextAsync(path);
            _cachedConfig = JsonSerializer.Deserialize<PolicyConfiguration>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return _cachedConfig!;
        }

        public async Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request)
        {
            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == request.PolicyCategory);
            if (category == null) throw new Exception("Invalid category");

            var tier = category.Tiers.FirstOrDefault(t => t.TierId == request.TierId);
            if (tier == null) throw new Exception("Invalid tier");

            var applicant = request.PolicyCategory == "INDIVIDUAL" ? request.Applicant : request.PrimaryApplicant;
            if (applicant == null) throw new Exception("Applicant details missing");

            double multiplier = 1.0;

            var ageM = config.RiskFactors.AgeMultipliers.FirstOrDefault(m => applicant.Age >= m.MinAge && applicant.Age <= m.MaxAge);
            multiplier *= ageM?.Multiplier ?? 1.2;

            var profM = config.RiskFactors.ProfessionMultipliers.FirstOrDefault(m => string.Equals(m.Profession, applicant.Profession, StringComparison.OrdinalIgnoreCase));
            multiplier *= profM?.Multiplier ?? 1.0;

            var incomeM = config.RiskFactors.IncomeMultiplier.FirstOrDefault(m => request.AnnualIncome >= m.MinIncome && request.AnnualIncome <= m.MaxIncome);
            multiplier *= incomeM?.Multiplier ?? 1.0;

            var alcoholKey = applicant.AlcoholHabit.ToLower().Replace(" ", "");
            multiplier *= alcoholKey switch
            {
                "nondrinker" => config.RiskFactors.AlcoholMultiplier.NonDrinker,
                "occasional" => config.RiskFactors.AlcoholMultiplier.Occasional,
                "regular" => config.RiskFactors.AlcoholMultiplier.Regular,
                _ => 1.0
            };

            var smokingKey = applicant.SmokingHabit.ToLower().Replace(" ", "");
            multiplier *= smokingKey switch
            {
                "nonsmoker" => config.RiskFactors.SmokingMultiplier.NonSmoker,
                "occasional" => config.RiskFactors.SmokingMultiplier.Occasional,
                "regular" => config.RiskFactors.SmokingMultiplier.Regular,
                _ => 1.0
            };

            var travelM = config.RiskFactors.TravelFrequencyMultiplier
                .OrderBy(m => m.MaxKmPerMonth)
                .FirstOrDefault(m => applicant.TravelKmPerMonth <= m.MaxKmPerMonth) 
                ?? config.RiskFactors.TravelFrequencyMultiplier.OrderByDescending(m => m.MaxKmPerMonth).First();
            multiplier *= travelM.Multiplier;
            
            var vehicleM = config.RiskFactors.VehicleTypeMultiplier.FirstOrDefault(m => string.Equals(m.VehicleType, applicant.VehicleType, StringComparison.OrdinalIgnoreCase));
            multiplier *= vehicleM?.Multiplier ?? 1.0;


            return tier.BasePremiumAmount * (decimal)multiplier;
        }

        public async Task<AuthResponseDto> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request)
        {
            var alreadyHasActivePolicy = await _policyRepository.HasActivePolicyAsync(userId, request.PolicyCategory);
            if (alreadyHasActivePolicy)
            {
                throw new Exception("You already have an active policy in this category.");
            }

            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == request.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == request.TierId);

            if (tier == null) throw new Exception("Invalid policy tier.");

            var premium = await CalculatePremiumAsync(request);
            var applicant = request.PolicyCategory == "INDIVIDUAL" ? request.Applicant! : request.PrimaryApplicant!;

            var application = new PolicyApplication
            {
                UserId = userId,
                PolicyCategory = request.PolicyCategory,
                TierId = request.TierId,
                CalculatedPremium = premium,
                SubmissionDate = DateTime.UtcNow,
                Status = "Pending",
                
                Age = applicant.Age,
                Profession = applicant.Profession,
                AnnualIncome = request.AnnualIncome,
                AlcoholHabit = applicant.AlcoholHabit,
                SmokingHabit = applicant.SmokingHabit,
                TravelKmPerMonth = applicant.TravelKmPerMonth,
                VehicleType = applicant.VehicleType,

                Address = request.Location?.Address,
                State = request.Location?.State,
                District = request.Location?.District,
                Pincode = request.Location?.Pincode,
                Latitude = request.Location?.Latitude,
                Longitude = request.Location?.Longitude,

                ApplicationDataJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            await _policyRepository.AddAsync(application);
            await _policyRepository.SaveChangesAsync(); 

            if (request.Nominee != null)
            {
                var nominee = new NomineeDetails
                {
                    PolicyApplicationId = application.Id,
                    NomineeName = request.Nominee.Name,
                    Relationship = request.Nominee.Relationship,
                    NomineePhone = request.Nominee.Phone,
                    NomineeEmail = request.Nominee.Email,
                    BankAccountNumber = request.Nominee.BankAccount,
                    IFSC = request.Nominee.IFSC,
                    AadharNumber = request.Nominee.AadharNumber ?? string.Empty,
                    AadharCardUrl = request.Nominee.AadharCardUrl ?? string.Empty
                };
                await _policyRepository.AddNomineeAsync(nominee);
            }

            if (request.PolicyCategory == "FAMILY" && request.FamilyMembers != null)
            {
                foreach (var fm in request.FamilyMembers)
                {
                    var member = new FamilyMember
                    {
                        PolicyApplicationId = application.Id,
                        FullName = fm.FullName,
                        Relation = fm.Relation,
                        DateOfBirth = fm.DateOfBirth,
                        ExistingHealthConditions = fm.HealthConditions
                    };
                    await _policyRepository.AddFamilyMemberAsync(member);
                }
            }

            await _policyRepository.SaveChangesAsync();

            await _systemNotifier.SendNotificationAsync(userId, "Application Submitted", 
                $"Your {request.TierId} policy application has been submitted successfully.", $"Policy:{application.Id}");

            var user = await _userManager.FindByIdAsync(userId);
            string userEmail = user?.Email ?? userId;

            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            foreach (var admin in admins)
            {
                await _systemNotifier.SendNotificationAsync(admin.Id, "New Policy Application 📝", 
                    $"New {request.TierId} application from {userEmail} needs agent assignment.", $"ADM:Policy:{application.Id}");
            }

            return new AuthResponseDto { Status = "Success", Message = application.Id };
        }

        public async Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId)
        {
            return await _policyRepository.GetUserPoliciesAsync(userId);
        }

        public async Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync()
        {
            return await _policyRepository.GetAllApplicationsAsync();
        }

        public async Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync()
        {
            var agents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent);
            
            var workloads = new List<AgentWorkloadDto>();
            foreach (var agent in agents)
            {
                var count = await _policyRepository.GetCountByAgentAsync(agent.Id);
                workloads.Add(new AgentWorkloadDto
                {
                    AgentId = agent.Id,
                    AgentEmail = agent.Email ?? string.Empty,
                    AssignedPolicyCount = count
                });
            }
            return workloads;
        }

        public async Task<bool> AssignAgentAsync(string applicationId, string agentId)
        {
            var app = await _policyRepository.GetByIdAsync(applicationId);
            if (app == null) return false;

            var agent = await _userManager.FindByIdAsync(agentId);
            if (agent == null) return false;

            app.AssignedAgentId = agentId;
            app.Status = "Assigned";
            
            await _policyRepository.UpdateAsync(app);
            await _policyRepository.SaveChangesAsync();

            await _systemNotifier.SendNotificationAsync(app.UserId, "Agent Assigned", 
                $"Agent {agent.Email} has been assigned to your {app.TierId} policy application.", $"Policy:{applicationId}");
            
            var applicantUser = await _userManager.FindByIdAsync(app.UserId);
            string applicantEmail = applicantUser?.Email ?? app.UserId;

            await _systemNotifier.SendNotificationAsync(agentId, "New Assignment 📁", 
                $"You have been assigned a new policy application from {applicantEmail}.", $"AGENT:Policy:{applicationId}");

            return true;
        }

        public async Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId)
        {
            return await _policyRepository.GetAgentApplicationsAsync(agentId);
        }

        public async Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId)
        {
            var app = await _policyRepository.GetByIdAsync(applicationId);
            if (app == null || app.AssignedAgentId != agentId) return false;

            if (status == "Approved")
            {
                app.ApprovedByAgentId = agentId;
                app.ApprovedAt = DateTime.UtcNow;
                app.Status = "AwaitingPayment";
            }
            else
            {
                app.Status = "Rejected";
            }

            await _policyRepository.UpdateAsync(app);
            await _policyRepository.SaveChangesAsync();

            string title = status == "Approved" ? "Payment Required 💳" : "Application Rejected ❌";
            string displayId = applicationId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Please complete your payment of {app.CalculatedPremium:C} for {app.TierId}. ID: {displayId}..." : 
                $"Your application for {app.TierId} (ID: {displayId}...) has been rejected.";
            
            await _systemNotifier.SendNotificationAsync(app.UserId, title, message, $"Policy:{applicationId}");

            return true;
        }

        public async Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId)
        {
            var app = await _policyRepository.GetByIdAsync(applicationId);
            if (app == null || app.Status != "AwaitingPayment") return false;

            app.Status = "Active";
            app.PaidAmount = amount;
            app.PaymentDate = DateTime.UtcNow;
            app.TransactionId = transactionId;
            app.StartDate = DateTime.UtcNow;

            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == app.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == app.TierId);
            
            int validityYears = tier?.ValidityInYears ?? 1; 
            app.ExpiryDate = app.StartDate.Value.AddYears(validityYears);
            
            try 
            {
                var appData = JsonDocument.Parse(app.ApplicationDataJson);
                if (appData.RootElement.TryGetProperty("paymentMode", out var modeProp))
                {
                    app.PaymentMode = modeProp.GetString();
                }
            }
            catch {}

            if (string.IsNullOrEmpty(app.PaymentMode)) app.PaymentMode = "yearly";

            app.NextPaymentDate = app.PaymentMode.ToLower() switch
            {
                "monthly" => app.StartDate.Value.AddMonths(1),
                "halfyearly" => app.StartDate.Value.AddMonths(6),
                "yearly" => app.ExpiryDate,
                _ => app.ExpiryDate
            };

            app.TotalCoverageAmount = tier?.BaseCoverageAmount ?? 0;
            app.RemainingCoverageAmount = app.TotalCoverageAmount;
            app.TotalApprovedClaimsAmount = 0;

            await _policyRepository.UpdateAsync(app);
            await _policyRepository.SaveChangesAsync();

            await _systemNotifier.SendNotificationAsync(app.UserId, "Policy Activated", 
                $"Your {app.TierId} policy is now ACTIVE. Coverage starts from today.", $"CUST:Policy:{applicationId}");

            var customerUser = await _userManager.FindByIdAsync(app.UserId);
            string customerEmail = customerUser?.Email ?? app.UserId;

            if (!string.IsNullOrEmpty(app.AssignedAgentId))
            {
                decimal commission = app.CalculatedPremium * 0.10m;
                await _systemNotifier.SendNotificationAsync(app.AssignedAgentId, "Commission Earned 💰", 
                    $"Customer {customerEmail} has paid for their {app.TierId} policy. You earned {commission:C} commission!", $"AGENT:Comm:{applicationId}");
            }
            else
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
                foreach (var admin in admins)
                {
                    await _systemNotifier.SendNotificationAsync(admin.Id, "Direct Payment Received", 
                        $"Customer {customerEmail} has paid directly for {app.TierId}.", $"ADM:Policy:{applicationId}");
                }
            }

            return true;
        }

        public async Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId)
        {
            var activePolicies = (await _policyRepository.GetAgentApplicationsAsync(agentId))
                .Where(pa => pa.Status == "Active")
                .ToList();

            decimal totalCommission = activePolicies.Sum(pa => pa.CalculatedPremium * 0.10m);

            return new AgentCommissionDto
            {
                TotalCommission = totalCommission,
                ActivePolicies = activePolicies
            };
        }

        public async Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId)
        {
            return await _policyRepository.GetAgentApplicationsAsync(agentId);
        }

        public async Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId)
        {
            var allAssigned = (await _policyRepository.GetApplicationsForAnalyticsAsync(agentId)).ToList();

            var activePolicies = allAssigned.Where(pa => pa.Status == "Active").ToList();
            
            var analytics = new AgentAnalyticsDto
            {
                TotalCoverageProvided = activePolicies.Sum(pa => pa.TotalCoverageAmount),
                ActivePolicyCount = activePolicies.Count,
                UniqueCustomerCount = allAssigned.Select(pa => pa.UserId).Distinct().Count(),
                TotalPremiumCollected = activePolicies.Sum(pa => pa.PaidAmount ?? 0),
                TotalCommissionEarned = activePolicies.Sum(pa => pa.CalculatedPremium * 0.10m),

                BestPerformingCategory = allAssigned.GroupBy(pa => pa.PolicyCategory)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "N/A",

                BestPerformingTier = allAssigned.GroupBy(pa => pa.TierId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "N/A",

                PortfolioMix = allAssigned.GroupBy(pa => pa.PolicyCategory)
                    .Select(g => new CategoryDistribution { Category = g.Key, Count = g.Count() })
                    .ToList(),

                TierBreakdown = allAssigned.GroupBy(pa => pa.TierId)
                    .Select(g => new TierDistribution { Tier = g.Key, Count = g.Count() })
                    .ToList(),

                PolicyStatusMetrics = allAssigned.GroupBy(pa => pa.Status)
                    .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                    .ToList(),

                CommissionPerformance = activePolicies
                    .Where(pa => pa.PaymentDate.HasValue)
                    .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                    .OrderBy(g => g.Min(pa => pa.PaymentDate))
                    .Select(g => new MonthlyDataPoint { Month = g.Key, Value = g.Sum(pa => pa.CalculatedPremium * 0.10m) })
                    .TakeLast(6)
                    .ToList(),

                PremiumTrends = activePolicies
                    .Where(pa => pa.PaymentDate.HasValue)
                    .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                    .OrderBy(g => g.Min(pa => pa.PaymentDate))
                    .Select(g => new MonthlyDataPoint { Month = g.Key, Value = g.Sum(pa => pa.PaidAmount ?? 0) })
                    .TakeLast(6)
                    .ToList(),

                ClaimImpact = new List<ClaimImpactData>() 
            };

            return analytics;
        }

        public async Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync()
        {
            var reports = (await _policyRepository.GetAllApplicationsAsync())
                .Select(pa => new UnifiedPaymentDto
                {
                    ApplicationId = pa.Id,
                    CustomerEmail = pa.User != null ? pa.User.Email : "Unknown",
                    AgentEmail = pa.AssignedAgent != null ? pa.AssignedAgent.Email : "Unassigned",
                    ClaimsOfficerEmail = "N/A", 
                    PolicyCategory = pa.PolicyCategory,
                    TierId = pa.TierId,
                    TotalCoverage = pa.TotalCoverageAmount,
                    CurrentCoverage = pa.RemainingCoverageAmount,
                    PremiumAmount = pa.CalculatedPremium,
                    PaidAmount = pa.PaidAmount,
                    LastPaymentDate = pa.PaymentDate,
                    TransactionId = pa.TransactionId,
                    PaymentMode = pa.PaymentMode,
                    Status = pa.Status
                })
                .ToList();

            return reports;
        }

        public async Task<AuthResponseDto> SubmitPolicyDocumentsAsync(SubmitPolicyDocumentsRequest request)
        {
            var application = await _policyRepository.GetByIdAsync(request.PolicyApplicationId);
            if (application == null) throw new Exception("Application not found.");

            foreach (var doc in request.Documents)
            {
                using var stream = doc.File.OpenReadStream();
                var uploadResult = await _fileStorage.UploadFileAsync(stream, doc.File.FileName, "policy-docs");

                var policyDoc = new ApplicationDocument
                {
                    PolicyApplicationId = application.Id,
                    DocumentType = doc.DocumentType,
                    FileName = uploadResult.FileName,
                    FileUrl = uploadResult.FileUrl,
                    FileSize = uploadResult.FileSize,
                    UploadedAt = DateTime.UtcNow
                };
                await _policyRepository.AddDocumentAsync(policyDoc);
            }

            application.Status = "PendingReview";
            await _policyRepository.UpdateAsync(application);
            await _policyRepository.SaveChangesAsync();

            return new AuthResponseDto { Status = "Success", Message = "Documents uploaded and application is now under review." };
        }
    
        public async Task<bool> UpdateInvoiceUrlAsync(string applicationId, string invoiceUrl)
        {
            var app = await _policyRepository.GetByIdAsync(applicationId);
            if (app == null) return false;

            app.InvoiceUrl = invoiceUrl;
            await _policyRepository.UpdateAsync(app);
            await _policyRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAnalysisUrlAsync(string applicationId, string analysisUrl)
        {
            var app = await _policyRepository.GetByIdAsync(applicationId);
            if (app == null) return false;

            app.AnalysisReportUrl = analysisUrl;
            await _policyRepository.UpdateAsync(app);
            await _policyRepository.SaveChangesAsync();
            return true;
        }

        public async Task<string> UploadGeneralFileAsync(Stream fileStream, string fileName, string folder)
        {
            var result = await _fileStorage.UploadFileAsync(fileStream, fileName, folder);
            return result.FileUrl;
        }
    }
}
