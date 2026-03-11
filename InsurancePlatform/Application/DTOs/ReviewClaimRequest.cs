namespace Application.DTOs
{
    public class ReviewClaimRequest
    {
        public string ClaimId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved or Rejected
        public string Remarks { get; set; } = string.Empty;
        public decimal ApprovedAmount { get; set; }
    }
}
