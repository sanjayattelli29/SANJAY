namespace Domain.Enums
{
    /// <summary>
    /// Static class containing the names of the user roles available in the system.
    /// Used for role-based authorization in controllers.
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
        public const string Agent = "Agent";
        public const string ClaimOfficer = "ClaimOfficer";
    }
}
