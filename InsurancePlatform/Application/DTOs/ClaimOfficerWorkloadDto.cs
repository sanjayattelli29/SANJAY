namespace Application.DTOs
{
    /// <summary>
    /// This class shows how many insurance claims a specific Officer is currently reviewing.
    /// It helps the system balance the workload so no one has too many claims at once.
    /// </summary>
    public class ClaimOfficerWorkloadDto
    {
        // The unique ID of the Claim Officer
        public string ClaimOfficerId { get; set; } = string.Empty;
        
        // The email address of the Officer
        public string? Email { get; set; }
        
        // The total number of pending claims this Officer is assigned to
        public int AssignedClaimsCount { get; set; }
    }
}
