using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class ClaimDocument : BaseEntity
    {
        public Guid ClaimId { get; set; }

        public string DocumentType { get; set; }

        public string FileUrl { get; set; }

        public string AIValidationStatus { get; set; }

        // Navigation
        public Claim Claim { get; set; }
    }
}