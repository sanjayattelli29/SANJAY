using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// This class provides detailed "Report Card" data for an individual Agent.
    /// It helps an agent see how much they've earned and how many customers they've helped.
    /// </summary>
    public class AgentAnalyticsDto
    {
        // Total money covered across all policies sold by this agent
        public decimal TotalCoverageProvided { get; set; }
        
        // Total number of policies that are currently active
        public int ActivePolicyCount { get; set; }
        
        // Total number of unique people this agent has sold policies to
        public int UniqueCustomerCount { get; set; }
        
        // The insurance category (like 'Term Life') that this agent sells the most
        public string BestPerformingCategory { get; set; } = string.Empty;
        
        // The pricing tier (like 'Premium') that is sold the most
        public string BestPerformingTier { get; set; } = string.Empty;
        
        // Total amount of premium money collected by this agent
        public decimal TotalPremiumCollected { get; set; }
        
        // Total commission money this agent has earned
        public decimal TotalCommissionEarned { get; set; }

        // --- Data for Trends and Charts ---
        
        // Shows how commission earnings change month by month
        public List<MonthlyDataPoint> CommissionPerformance { get; set; } = new();
        
        // Shows the mix of different policy types (Health vs Life, etc.)
        public List<CategoryDistribution> PortfolioMix { get; set; } = new();
        
        // Shows how many policies are in different price tiers (Bronze, Silver, Gold)
        public List<TierDistribution> TierBreakdown { get; set; } = new();
        
        // Shows how premium collection is trending over time
        public List<MonthlyDataPoint> PremiumTrends { get; set; } = new();
        
        // Shows how many policies are pending, active, or lapsed
        public List<StatusCount> PolicyStatusMetrics { get; set; } = new();
        
        // Shows how much was paid out in claims vs how much coverage was provided
        public List<ClaimImpactData> ClaimImpact { get; set; } = new();
    }

    /// <summary>
    /// A simple class to store a data point for a specific month.
    /// </summary>
    public class MonthlyDataPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    /// <summary>
    /// A simple class to store how many items are in a category (e.g., "Life": 5).
    /// </summary>
    public class CategoryDistribution
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// A simple class to store how many policies are in a specific plan tier.
    /// </summary>
    public class TierDistribution
    {
        public string Tier { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// A simple class to count items with a specific status.
    /// </summary>
    public class StatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// A class to store metrics related to claim impacts (like loss ratios).
    /// </summary>
    public class ClaimImpactData
    {
        public string Metric { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
