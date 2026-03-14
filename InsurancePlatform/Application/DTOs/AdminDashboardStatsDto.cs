using System.Collections.Generic;

namespace Application.DTOs
{
    // this class is for showing general numbers on admin dashboard
    public class AdminDashboardStatsDto
    {
        // total number of people registered
        public int TotalCustomers { get; set; }
        // total number of policies bought
        public int TotalPolicies { get; set; }
        // total number of claims filed
        public int TotalClaims { get; set; }
        // total money given for all claims
        public decimal TotalClaimedAmount { get; set; }
        // NEW: total system agents
        public int TotalAgents { get; set; }
        // NEW: total system officers
        public int TotalClaimOfficers { get; set; }
        // NEW: total coverage amount raised across all policies
        public decimal TotalCoverageRaised { get; set; }
        // NEW: total premium collection sum
        public decimal TotalPremiumCollected { get; set; }
        // NEW: total commission for all agents combined
        public decimal TotalCommission { get; set; }

        // Chart Data
        public List<StatPointDto> PolicyGrowth { get; set; } = new();
        public List<StatPointDto> RevenueTrends { get; set; } = new();
        public List<CategoryStatDto> ClaimsByCategory { get; set; } = new();
    }

    public class StatPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class CategoryStatDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
