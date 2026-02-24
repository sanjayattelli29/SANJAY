using System;

namespace InsurancePlatform.DTOs.Claim
{
    public class ClaimResponseDto
    {
        public Guid ClaimId { get; set; }
        public string ClaimNumber { get; set; }
        public string PolicyNumber { get; set; }
        public string Description { get; set; }
        public decimal RequestedAmount { get; set; }
        public string Status { get; set; }
        public decimal FraudRiskScore { get; set; }
        public string AssignedOfficerName { get; set; }
        public DateTime SLAStartTime { get; set; }
        public DateTime SLADeadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
