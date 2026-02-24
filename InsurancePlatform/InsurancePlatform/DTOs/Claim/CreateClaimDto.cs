namespace InsurancePlatform.DTOs.Claim
{
    public class CreateClaimDto
    {
        public Guid PolicyId { get; set; }
        public string Description { get; set; }
        public decimal RequestedAmount { get; set; }
        public DateTime IncidentDate { get; set; }
    }
}
