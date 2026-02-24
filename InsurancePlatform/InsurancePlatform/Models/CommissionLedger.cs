using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class CommissionLedger : BaseEntity
    {
        public Guid AgentId { get; set; }

        public Guid PolicyId { get; set; }

        public decimal CommissionAmount { get; set; }

        public CommissionStatus CommissionStatus { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Navigation
        public AgentProfile Agent { get; set; }
        public Policy Policy { get; set; }
    }

    public enum CommissionStatus
    {
        Pending,
        Paid
    }
}