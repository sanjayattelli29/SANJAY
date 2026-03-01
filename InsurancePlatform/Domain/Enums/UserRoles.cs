namespace Domain.Enums // enum folder
{
    // this class has all the roles we use in the website
    // we use these strings everywhere for security
    public static class UserRoles
    {
        // the boss who can do everything
        // high power role
        public const string Admin = "Admin";
        // the normal person who buys insurance
        // standard user role
        public const string Customer = "Customer";
        // the person who sells and checks policies
        // sales role
        public const string Agent = "Agent";
        // the person who checks and pays claims
        // verification role
        public const string ClaimOfficer = "ClaimOfficer";
    }
}
// end of roles
