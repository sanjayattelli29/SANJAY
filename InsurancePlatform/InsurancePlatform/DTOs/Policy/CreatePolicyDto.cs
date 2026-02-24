using InsurancePlatform.Models;
using System;

namespace InsurancePlatform.DTOs.Policy
{
    public class CreatePolicyDto
    {
        public decimal SumInsured { get; set; }
        public PolicyType Type { get; set; }
        public PaymentMode PaymentMode { get; set; }

        // Risk Assessment Data
        public int TrafficViolationCount { get; set; }
        public AlcoholConsumptionFrequency AlcoholConsumptionFrequency { get; set; }
        public SmokingHabit SmokingHabit { get; set; }
        public bool AdventureActivityParticipation { get; set; }
        public bool MedicalReportUploaded { get; set; }
        public int TravelFrequencyKmPerMonth { get; set; }
        public VehicleType VehicleType { get; set; }
        public string PrimaryCity { get; set; }
    }
}
