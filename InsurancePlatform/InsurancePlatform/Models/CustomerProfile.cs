using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class CustomerProfile : BaseEntity
    {
        public Guid UserId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? AadhaarNumber { get; set; }

        public OccupationType? OccupationType { get; set; }

        public decimal? AnnualIncome { get; set; }

        public string? PrimaryCity { get; set; }

        public string? SecondaryCity { get; set; }

        public string? TertiaryCity { get; set; }

        public int? TravelFrequencyKmPerMonth { get; set; }

        public VehicleType? VehicleType { get; set; }

        public int? TrafficViolationCount { get; set; }

        public AlcoholConsumptionFrequency? AlcoholConsumptionFrequency { get; set; }

        public SmokingHabit? SmokingHabit { get; set; }

        public bool? AdventureActivityParticipation { get; set; }

        public bool? MedicalReportUploaded { get; set; }

        public decimal RiskScore { get; set; } = 0;

        public RiskCategory RiskCategory { get; set; } = RiskCategory.Low;

        // Navigation
        public User User { get; set; }
    }

    public enum OccupationType
    {
        Salaried,
        Business,
        SelfEmployed,
        FieldWorker,
        HighRiskWorker
    }

    public enum VehicleType
    {
        None,
        TwoWheeler,
        FourWheeler,
        Both
    }

    public enum AlcoholConsumptionFrequency
    {
        None,
        Occasional,
        Regular
    }

    public enum SmokingHabit
    {
        None,
        Occasional,
        Regular
    }

    public enum RiskCategory
    {
        Low,
        Standard,
        High,
        ManualReview
    }
}