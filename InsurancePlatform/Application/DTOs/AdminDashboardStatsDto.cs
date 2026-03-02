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
    }
}
