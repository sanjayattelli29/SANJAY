namespace Application.DTOs
{
    /// <summary>
    /// This class is used by a Claim Officer when they have finished checking a claim.
    /// They use this to tell the system if the claim was approved or rejected.
    /// </summary>
    public class ReviewClaimRequest
    {
        // The ID of the claim being reviewed
        public string ClaimId { get; set; } = string.Empty;
        
        // The decision: must be 'Approved' or 'Rejected'
        public string Status { get; set; } = string.Empty;
        
        // Any notes or reasons for the decision (especially if rejected)
        public string Remarks { get; set; } = string.Empty;
        
        // The final amount of money the insurance company will pay (may be less than requested)
        public decimal ApprovedAmount { get; set; }
    }
}
