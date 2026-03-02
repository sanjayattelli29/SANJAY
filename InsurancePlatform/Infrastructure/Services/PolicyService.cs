using Application.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Domain.Enums;
using System.Text.Json;

namespace Infrastructure.Services
{
    // this class manages everything about policies like plans and premiums
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private static PolicyConfiguration? _cachedConfig;
        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "policy-config.json");

        public PolicyService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // code to read the configuration from json file
        public async Task<PolicyConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfig != null) return _cachedConfig;

            // find the path to policy-config.json
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "policy-config.json");
            
            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "policy-config.json");
            }

            var json = await File.ReadAllTextAsync(path);
            // turn json text into c# object
            _cachedConfig = JsonSerializer.Deserialize<PolicyConfiguration>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return _cachedConfig!;
        }

        // math to find how much money policy costs
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

            // Age Multiplier
            var ageM = config.RiskFactors.AgeMultipliers.FirstOrDefault(m => applicant.Age >= m.MinAge && applicant.Age <= m.MaxAge);
            multiplier *= ageM?.Multiplier ?? 1.2; // Default to mid-range if not found

            // Profession Multiplier
            var profM = config.RiskFactors.ProfessionMultipliers.FirstOrDefault(m => m.Profession == applicant.Profession);
            multiplier *= profM?.Multiplier ?? 1.6;

            // Alcohol Multiplier
            multiplier *= applicant.AlcoholHabit.ToLower() switch
            {
                "nondrinker" => config.RiskFactors.AlcoholMultiplier.NonDrinker,
                "occasional" => config.RiskFactors.AlcoholMultiplier.Occasional,
                "regular" => config.RiskFactors.AlcoholMultiplier.Regular,
                _ => config.RiskFactors.AlcoholMultiplier.Others
            };

            // Smoking Multiplier
            multiplier *= applicant.SmokingHabit.ToLower() switch
            {
                "nonsmoker" => config.RiskFactors.SmokingMultiplier.NonSmoker,
                "occasional" => config.RiskFactors.SmokingMultiplier.Occasional,
                "regular" => config.RiskFactors.SmokingMultiplier.Regular,
                _ => config.RiskFactors.SmokingMultiplier.Others
            };

            // Travel Multiplier
            var travelM = config.RiskFactors.TravelFrequencyMultiplier
                .OrderBy(m => m.MaxKmPerMonth)
                .FirstOrDefault(m => applicant.TravelKmPerMonth <= m.MaxKmPerMonth) 
                ?? config.RiskFactors.TravelFrequencyMultiplier.Last();
            multiplier *= travelM.Multiplier;

            // Payment Mode Multiplier
            multiplier *= request.PaymentMode.ToLower() switch
            {
                "monthly" => config.PaymentModeMultipliers.Monthly,
                "halfyearly" => config.PaymentModeMultipliers.HalfYearly,
                "yearly" => config.PaymentModeMultipliers.Yearly,
                _ => 1.0
            };

            return tier.BasePremiumAmount * (decimal)multiplier;
        }

        public async Task<object> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request)
        {
            // Validation: Check if the user already has an active policy in this category
            var alreadyHasActivePolicy = await _context.PolicyApplications
                .AnyAsync(pa => pa.UserId == userId && 
                                pa.PolicyCategory == request.PolicyCategory && 
                                pa.Status == "Active");

            if (alreadyHasActivePolicy)
            {
                throw new Exception("You already have an active policy in this category.");
            }

            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == request.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == request.TierId);

            if (tier == null) throw new Exception("Invalid policy tier.");

            var premium = await CalculatePremiumAsync(request);

            var application = new PolicyApplication
            {
                UserId = userId,
                PolicyCategory = request.PolicyCategory,
                TierId = request.TierId,
                CalculatedPremium = premium,
                SubmissionDate = DateTime.UtcNow,
                Status = "Pending", // Set to Pending for manual assignment
                ApplicationDataJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            _context.PolicyApplications.Add(application);
            await _context.SaveChangesAsync();

            // Send notification to user
            await _notificationService.SendNotificationAsync(userId, "Application Submitted", 
                $"Your {request.TierId} policy application has been submitted successfully.", $"Policy:{application.Id}");

            // Notify all Admins to assign an agent
            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationAsync(admin.Id, "New Policy Application", 
                    $"New application from {userId} for {request.TierId} ({tier.BaseCoverageAmount:C}). Please assign an agent.", "Policy");
            }

            return new { Status = "Success", ApplicationId = application.Id, Premium = premium };
        }

        public async Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId)
        {
            return await _context.PolicyApplications
                .Where(pa => pa.UserId == userId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync()
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.AssignedAgent)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync()
        {
            // Get all users in the 'Agent' role using UserManager
            var agents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent);
            
            var workloads = new List<AgentWorkloadDto>();
            foreach (var agent in agents)
            {
                var count = await _context.PolicyApplications.CountAsync(pa => pa.AssignedAgentId == agent.Id);
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
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null) return false;

            var agent = await _userManager.FindByIdAsync(agentId);
            if (agent == null) return false;

            app.AssignedAgentId = agentId;
            app.Status = "Assigned";
            
            await _context.SaveChangesAsync();

            // Notify user about agent assignment
            await _notificationService.SendNotificationAsync(app.UserId, "Agent Assigned", 
                $"Agent {agent.Email} has been assigned to your {app.TierId} policy application.", "Policy");
            
            // Notify agent about new assignment
            await _notificationService.SendNotificationAsync(agentId, "New Application Assigned", 
                $"You have been assigned a new application for {app.TierId}.", "Policy");

            return true;
        }

        public async Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)
                .Where(pa => pa.AssignedAgentId == agentId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        public async Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId)
        {
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null || app.AssignedAgentId != agentId) return false;

            if (status == "Approved")
            {
                app.ApprovedByAgentId = agentId;
                app.ApprovedAt = DateTime.UtcNow;
                app.Status = "AwaitingPayment"; // Per requirement: Approved -> AwaitingPayment
            }
            else
            {
                app.Status = "Rejected";
            }

            await _context.SaveChangesAsync();

            // Notify user about review result
            string title = status == "Approved" ? "Payment Required 💳" : "Application Rejected ❌";
            string displayId = applicationId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Please complete your payment of {app.CalculatedPremium:C} for {app.TierId}. ID: {displayId}..." : 
                $"Your application for {app.TierId} (ID: {displayId}...) has been rejected.";
            
            await _notificationService.SendNotificationAsync(app.UserId, title, message, $"Policy:{applicationId}");

            return true;
        }

        public async Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId)
        {
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null || app.Status != "AwaitingPayment") return false;

            // Update status and payment details
            app.Status = "Active";
            app.PaidAmount = amount;
            app.PaymentDate = DateTime.UtcNow;
            app.TransactionId = transactionId;
            app.StartDate = DateTime.UtcNow;

            // Calculate ExpiryDate based on tier from config
            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == app.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == app.TierId);
            
            int validityYears = tier?.ValidityInYears ?? 1; 
            app.ExpiryDate = app.StartDate.Value.AddYears(validityYears);
            
            // Extract PaymentMode and calculate NextPaymentDate
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

            // Financial Initialization
            app.TotalCoverageAmount = tier?.BaseCoverageAmount ?? 0;
            app.RemainingCoverageAmount = app.TotalCoverageAmount;
            app.TotalApprovedClaimsAmount = 0;

            await _context.SaveChangesAsync();

            // Notify user about activation
            await _notificationService.SendNotificationAsync(app.UserId, "Policy Activated", 
                $"Your {app.TierId} policy is now ACTIVE. Coverage starts from today.", $"Policy:{applicationId}");

            return true;
        }

        public async Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId)
        {
            var activePolicies = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Where(pa => pa.AssignedAgentId == agentId && pa.Status == "Active")
                .OrderByDescending(pa => pa.PaymentDate)
                .ToListAsync();

            decimal totalCommission = activePolicies.Sum(pa => pa.CalculatedPremium * 0.10m);

            return new AgentCommissionDto
            {
                TotalCommission = totalCommission,
                ActivePolicies = activePolicies
            };
        }

        public async Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)
                .Where(pa => pa.AssignedAgentId == agentId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        public async Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId)
        {
            var allAssigned = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Where(pa => pa.AssignedAgentId == agentId)
                .ToListAsync();

            var activePolicies = allAssigned.Where(pa => pa.Status == "Active").ToList();
            
            // Total Claims for these policies
            var policyIds = allAssigned.Select(pa => pa.Id).ToList();
            var claims = await _context.InsuranceClaims
                .Where(c => policyIds.Contains(c.PolicyApplicationId))
                .ToListAsync();

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

                // Portfolio Mix (Category Distribution)
                PortfolioMix = allAssigned.GroupBy(pa => pa.PolicyCategory)
                    .Select(g => new CategoryDistribution { Category = g.Key, Count = g.Count() })
                    .ToList(),

                // Tier Breakdown
                TierBreakdown = allAssigned.GroupBy(pa => pa.TierId)
                    .Select(g => new TierDistribution { Tier = g.Key, Count = g.Count() })
                    .ToList(),

                // Status Metrics
                PolicyStatusMetrics = allAssigned.GroupBy(pa => pa.Status)
                    .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                    .ToList(),

                // Commission & Premium Trends (Last 6 Months)
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

                // Claim Impact
                ClaimImpact = new List<ClaimImpactData>
                {
                    new ClaimImpactData { Metric = "Premium Collected", Value = activePolicies.Sum(pa => pa.PaidAmount ?? 0) },
                    new ClaimImpactData { Metric = "Approved Claims", Value = claims.Where(c => c.Status == "Approved").Sum(c => c.ApprovedAmount) },
                    new ClaimImpactData { Metric = "Pending Claims", Value = claims.Where(c => c.Status == "Pending" || c.Status == "Assigned").Sum(c => c.RequestedAmount) }
                }
            };

            return analytics;
        }

        public async Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync()
        {
            var reports = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.AssignedAgent)
                .Select(pa => new UnifiedPaymentDto
                {
                    ApplicationId = pa.Id,
                    CustomerEmail = pa.User != null ? pa.User.Email : "Unknown",
                    AgentEmail = pa.AssignedAgent != null ? pa.AssignedAgent.Email : "Unassigned",
                    // We take the first assigned officer from any associated claim for this policy
                    ClaimsOfficerEmail = _context.InsuranceClaims
                        .Include(c => c.AssignedOfficer)
                        .Where(c => c.PolicyApplicationId == pa.Id && c.AssignedClaimOfficerId != null)
                        .Select(c => c.AssignedOfficer.Email)
                        .FirstOrDefault() ?? "N/A",
                    PolicyCategory = pa.PolicyCategory,
                    TierId = pa.TierId,
                    TotalCoverage = pa.TotalCoverageAmount,
                    CurrentCoverage = pa.RemainingCoverageAmount,
                    PremiumAmount = pa.CalculatedPremium,
                    PaidAmount = pa.PaidAmount,
                    NextPaymentDate = pa.NextPaymentDate,
                    LastPaymentDate = pa.PaymentDate,
                    TransactionId = pa.TransactionId,
                    PaymentMode = pa.PaymentMode,
                    Status = pa.Status
                })
                .ToListAsync();

            return reports;
        }
    }
}