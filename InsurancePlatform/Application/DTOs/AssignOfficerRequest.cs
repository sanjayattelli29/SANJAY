namespace Application.DTOs
{
    /// <summary>
    /// This class is used when we want to tell the system to assign a specific person (Officer)
    /// to handle a specific insurance claim.
    /// </summary>
    public class AssignOfficerRequest
    {
        // The unique ID of the insurance claim that needs to be reviewed
        public string ClaimId { get; set; } = string.Empty;
        
        // The unique ID of the Officer who will handle this claim
        public string OfficerId { get; set; } = string.Empty;
    }
}
