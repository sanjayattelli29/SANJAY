using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class Policy : BaseEntity
    {
        public string PolicyNumber { get; set; }

        public Guid CustomerId { get; set; }

        public Guid AssignedAgentId { get; set; }

        public decimal SumInsured { get; set; }

        public decimal PremiumAmount { get; set; }

        public PaymentMode PaymentMode { get; set; }

        public DateTime PolicyStartDate { get; set; }

        public DateTime PolicyEndDate { get; set; }

        public PolicyType Type { get; set; }
        public PolicyStatus Status { get; set; }

        public decimal RiskScoreAtIssuance { get; set; }

        // Navigation
        public User Customer { get; set; }
        public User AssignedAgent { get; set; }
    }

    public enum PaymentMode
    {
        Yearly,
        HalfYearly,
        Monthly
    }

    public enum PolicyType
    {
        TermLife,
        AccidentalInsurance,
        CriticalIllness
    }

    public enum PolicyStatus
    {
        Active,
        Expired,
        Cancelled,
        ActiveOrderDraft,
        Rejected
    }
}