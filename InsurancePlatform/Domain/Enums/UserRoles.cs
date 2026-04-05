namespace Domain.Enums
{
    /// <summary>
    /// This class defines the different "Roles" or "Jobs" a user can have in the system.
    /// Each role lets the user see different pages and buttons.
    /// </summary>
    public static class UserRoles
    {
        // "Admin" is the boss. They can manage all users and settings.
        public const string Admin = "Admin";

        // "Customer" is the person who buys insurance and files claims.
        public const string Customer = "Customer";

        // "Agent" is the person who checks policy applications and helps customers.
        public const string Agent = "Agent";

        // "ClaimOfficer" is the person who reviews bills and approves claim payments.
        public const string ClaimOfficer = "ClaimOfficer";
    }
}
