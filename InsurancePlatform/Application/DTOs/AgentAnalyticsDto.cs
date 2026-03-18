using System.Collections.Generic;

namespace Application.DTOs
{
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
}
