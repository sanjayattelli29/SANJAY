using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class Claim : BaseEntity
    {
        public Guid PolicyId { get; set; }

        public Guid AssignedClaimsOfficerId { get; set; }

        public ClaimType ClaimType { get; set; }

        public decimal ClaimAmountCalculated { get; set; }

        public decimal FraudRiskScore { get; set; }

        public string ClaimNumber { get; set; }
        public string Description { get; set; }
        public decimal RequestedAmount { get; set; }
        public DateTime IncidentDate { get; set; }
        public ClaimStatus Status { get; set; }

        public DateTime SLAStartTime { get; set; }

        public DateTime SLADeadline { get; set; }

        public DateTime? DecisionTime { get; set; }

        // Navigation
        public Policy Policy { get; set; }
        public ClaimsOfficerProfile AssignedClaimsOfficer { get; set; }
    }

    public enum ClaimType
    {
        AccidentalDeath,
        PTD,
        PPD
    }

    public enum ClaimStatus
    {
        Submitted,
        UnderReview,
        Approved,
        Rejected,
        Settled
    }
}