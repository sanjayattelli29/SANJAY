using Application.DTOs;
using Domain.Entities;
using System.Text.Json;

namespace Application.Interfaces
{
    public interface IPolicyService
    {
        Task<PolicyConfiguration> GetConfigurationAsync();
        Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request);
        Task<object> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request);
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);
        
        // Admin & Agent Management
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync();
        Task<bool> AssignAgentAsync(string applicationId, string agentId);
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId);
        Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId);
        Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId);
        Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId);
        Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId);
    }

    public class AgentAnalyticsDto
    {
        public decimal TotalCoverageProvided { get; set; }
        public int ActivePolicyCount { get; set; }
        public int UniqueCustomerCount { get; set; }
        public string BestPerformingCategory { get; set; } = string.Empty;
        public string BestPerformingTier { get; set; } = string.Empty;
        public decimal TotalPremiumCollected { get; set; }
        public decimal TotalCommissionEarned { get; set; }

        public List<MonthlyDataPoint> CommissionPerformance { get; set; } = new();
        public List<CategoryDistribution> PortfolioMix { get; set; } = new();
        public List<TierDistribution> TierBreakdown { get; set; } = new();
        public List<MonthlyDataPoint> PremiumTrends { get; set; } = new();
        public List<StatusCount> PolicyStatusMetrics { get; set; } = new();
        public List<ClaimImpactData> ClaimImpact { get; set; } = new();
    }

    public class MonthlyDataPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class CategoryDistribution
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TierDistribution
    {
        public string Tier { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ClaimImpactData
    {
        public string Metric { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class AgentCommissionDto
    {
        public decimal TotalCommission { get; set; }
        public List<PolicyApplication> ActivePolicies { get; set; } = new();
    }

    public class AgentWorkloadDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentEmail { get; set; } = string.Empty;
        public int AssignedPolicyCount { get; set; }
    }
}

namespace Application.DTOs
{
    public class PolicyApplicationRequest
    {
        public string PolicyCategory { get; set; } = string.Empty;
        public string TierId { get; set; } = string.Empty;
        public ApplicantDetails? Applicant { get; set; } // For Individual
        public ApplicantDetails? PrimaryApplicant { get; set; } // For Family
        public List<FamilyMemberDetails>? FamilyMembers { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public NomineeDetails? Nominee { get; set; }
    }

    public class ApplicantDetails
    {
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Profession { get; set; } = string.Empty;
        public string AlcoholHabit { get; set; } = string.Empty;
        public string SmokingHabit { get; set; } = string.Empty;
        public int TravelKmPerMonth { get; set; }
    }

    public class FamilyMemberDetails
    {
        public string FullName { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
    }

    public class NomineeDetails
    {
        public string NomineeName { get; set; } = string.Empty;
        public string NomineeEmail { get; set; } = string.Empty;
        public string NomineePhone { get; set; } = string.Empty;
        public string NomineeBankAccountNumber { get; set; } = string.Empty;
    }
}
