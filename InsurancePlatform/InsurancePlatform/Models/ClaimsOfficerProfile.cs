using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class ClaimsOfficerProfile : BaseEntity
    {
        public Guid UserId { get; set; }

        public string EmployeeCode { get; set; }

        public string Qualification { get; set; }

        public int YearsOfExperience { get; set; }

        public int ActiveClaimCount { get; set; } = 0;

        public bool IsApprovedByAdmin { get; set; } = false;

        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public User User { get; set; }
    }
}