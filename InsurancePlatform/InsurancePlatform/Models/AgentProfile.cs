using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class AgentProfile : BaseEntity
    {
        public Guid UserId { get; set; }

        public string EmployeeCode { get; set; }

        public string PANNumber { get; set; }

        public string LicenseNumber { get; set; }

        public int YearsOfExperience { get; set; }

        public string Qualification { get; set; }

        public string BankAccountNumber { get; set; }

        public string IFSCCode { get; set; }

        public int ActiveCustomerCount { get; set; } = 0;

        public int ActiveLeadCount { get; set; } = 0;

        public decimal CommissionPercentage { get; set; }

        public bool IsApprovedByAdmin { get; set; } = false;

        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public User User { get; set; }
    }
}