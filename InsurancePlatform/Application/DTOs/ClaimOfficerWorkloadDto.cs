namespace Application.DTOs
{
    public class ClaimOfficerWorkloadDto
    {
        public string ClaimOfficerId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int AssignedClaimsCount { get; set; }
    }
}
