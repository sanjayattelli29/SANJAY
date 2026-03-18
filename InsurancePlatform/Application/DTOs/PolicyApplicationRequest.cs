using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class PolicyApplicationRequest
    {
        public string PolicyCategory { get; set; } = string.Empty;
        public string TierId { get; set; } = string.Empty;
        public decimal AnnualIncome { get; set; }
        public string PaymentMode { get; set; } = "Yearly";

        // For Individual
        public ApplicantDetails? Applicant { get; set; }

        // For Family
        public ApplicantDetails? PrimaryApplicant { get; set; }
        public List<FamilyMemberRequest>? FamilyMembers { get; set; }
        
        public NomineeRequest? Nominee { get; set; }
        public LocationRequest? Location { get; set; }
    }

    public class ApplicantDetails
    {
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Profession { get; set; } = string.Empty;
        public string AlcoholHabit { get; set; } = "Non-Drinker";
        public string SmokingHabit { get; set; } = "Non-Smoker";
        public int TravelKmPerMonth { get; set; }
        public string VehicleType { get; set; } = "None";
    }

    public class FamilyMemberRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string HealthConditions { get; set; } = "None";
    }

    public class NomineeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string IFSC { get; set; } = string.Empty;
        public string? AadharNumber { get; set; }
        public string? AadharCardUrl { get; set; }
    }

    public class LocationRequest
    {
        public string Address { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
