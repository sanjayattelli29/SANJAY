using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used to gather all the "Big Numbers" and "Charts" for the Admin's main dashboard.
    /// It gives the Admin a quick overview of how the whole system is doing.
    /// </summary>
    public class AdminDashboardStatsDto
    {
        // Total number of customers who have signed up
        public int TotalCustomers { get; set; }
        
        // Total number of insurance policies created in the system
        public int TotalPolicies { get; set; }
        
        // Total number of insurance claims submitted by customers
        public int TotalClaims { get; set; }
        
        // The total sum of money requested across all claims
        public decimal TotalClaimedAmount { get; set; }
        
        // The number of active insurance agents in the system
        public int TotalAgents { get; set; }
        
        // The number of claim officers who review claims
        public int TotalClaimOfficers { get; set; }
        
        // The total amount of insurance coverage (money) provided to all users
        public decimal TotalCoverageRaised { get; set; }
        
        // The total amount of money collected from customers for their policies
        public decimal TotalPremiumCollected { get; set; }
        
        // The total commission money earned by all agents
        public decimal TotalCommission { get; set; }

        // --- Data for Charts ---
        
        // Shows how the number of policies has grown over time
        public List<StatPointDto> PolicyGrowth { get; set; } = new();
        
        // Shows how much money (revenue) is coming in over time
        public List<StatPointDto> RevenueTrends { get; set; } = new();
        
        // Breaks down claims by their category (Life, Health, etc.)
        public List<CategoryStatDto> ClaimsByCategory { get; set; } = new();
        
        // Shows how many policies are 'Active', 'Pending', or 'Closed'
        public List<StatusCountDto> PolicyStatusDistribution { get; set; } = new();
        
        // Shows how many claims are 'Approved', 'Rejected', or 'Under Review'
        public List<StatusCountDto> ClaimStatusDistribution { get; set; } = new();
        
        // Shows which types of insurance policies are most popular
        public List<CategoryStatDto> PolicyCategoryDistribution { get; set; } = new();
        
        // Compares how different agents are performing
        public List<StatusCountDto> AgentPerformance { get; set; } = new();
    }

    /// <summary>
    /// A simple class to hold a 'Status' label and its count (e.g., "Active": 10).
    /// </summary>
    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// A simple class to hold a 'Label' and a decimal value (useful for money trends).
    /// </summary>
    public class StatPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    /// <summary>
    /// A simple class to hold a 'Category' name and its count.
    /// </summary>
    public class CategoryStatDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
