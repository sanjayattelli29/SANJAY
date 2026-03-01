using Application.DTOs; // data objects
using Domain.Entities;
using System.Text.Json; // json utility

namespace Application.Interfaces // interface namespace
{
    // this interface is for main policy functions like applying and paying
    public interface IPolicyService // policy logic contract
    {
        // get the settings for coverage and price from db
        Task<PolicyConfiguration> GetConfigurationAsync(); // fetch rules
        // find out how much user has to pay based on habits
        Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request);
        // user tries to buy a new policy
        Task<object> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request);
        // list all policies belonging to one customer
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);
        
        // list all applications for admin or agent dashboard
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync(); // list for staff
        // see which agents are working on how many policies
        Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync();
        // give a policy application to an agent to check
        Task<bool> AssignAgentAsync(string applicationId, string agentId);
        // list of policies an agent has to check
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        // agent gives status like approved or rejected
        Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId);
        // save the payment details once user pays bank
        Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId); // finalize sale
        // check how much money agent made this month
        Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId);
        // list all customers an agent is handling
        Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId);
        // get detailed charts and numbers for agent
        Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId);
        // list of all payments made across system
        Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync(); // report data
    }
}
// policy service interface ends
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
