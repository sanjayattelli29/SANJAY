namespace Application.DTOs // folder for data objects
{
    // this class is for showing general numbers on admin dashboard
    // helps admin see how the business is doing
    public class AdminDashboardStatsDto
    {
        // total number of people registered
        // count of all users with Customer role
        public int TotalCustomers { get; set; }
        // total number of policies bought
        // count of all active and pending policies
        public int TotalPolicies { get; set; }
        // total number of claims filed
        // count of all insurance claims in system
        public int TotalClaims { get; set; }
        // total money given for all claims
        // sum of all approved claim amounts
        public decimal TotalClaimedAmount { get; set; }
    }
}
// stats object ends
