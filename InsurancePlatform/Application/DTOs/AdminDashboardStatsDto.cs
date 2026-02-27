namespace Application.DTOs
{
    public class AdminDashboardStatsDto
    {
        public int TotalCustomers { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalClaimedAmount { get; set; }
    }
}
