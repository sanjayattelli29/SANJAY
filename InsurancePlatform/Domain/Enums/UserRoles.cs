namespace Domain.Enums
{
    // this class has all the roles we use in the website
    public static class UserRoles
    {
        // the boss who can do everything
        public const string Admin = "Admin";
        // the normal person who buys insurance
        public const string Customer = "Customer";
        // the person who sells and checks policies
        public const string Agent = "Agent";
        // the person who checks and pays claims
        public const string ClaimOfficer = "ClaimOfficer";
    }
}
