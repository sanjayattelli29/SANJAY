using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class RiskAssessment : BaseEntity
    {
        public Guid CustomerId { get; set; }

        public decimal AgeScore { get; set; }

        public decimal OccupationScore { get; set; }

        public decimal BehaviorScore { get; set; }

        public decimal TravelExposureScore { get; set; }

        public decimal LocationScore { get; set; }

        public decimal MedicalScore { get; set; }

        public decimal TotalRiskScore { get; set; }

        // Navigation
        public CustomerProfile Customer { get; set; }
    }
}