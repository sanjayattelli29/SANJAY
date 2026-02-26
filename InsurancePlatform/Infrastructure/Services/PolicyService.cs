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
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private static PolicyConfiguration? _cachedConfig;
        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "policy-config.json");

        public PolicyService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<PolicyConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfig != null) return _cachedConfig;

            // In a real app, this might come from a DB table or a distributed cache
            // For now, we load from the JSON file created in the Data folder
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "policy-config.json");
            
            if (!File.Exists(path))
            {
                // Fallback for different environments
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "policy-config.json");
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

            app.AssignedAgentId = agentId;
            app.Status = "Assigned";
            
            await _context.SaveChangesAsync();
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
    }
}
