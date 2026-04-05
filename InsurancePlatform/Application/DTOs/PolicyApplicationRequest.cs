using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// This is the main "Application Form" that a customer fills out 
    /// when they want to buy a new insurance policy.
    /// </summary>
    public class PolicyApplicationRequest
    {
        // The type of insurance (e.g., 'Health', 'Life', 'Auto')
        public string PolicyCategory { get; set; } = string.Empty;
        
        // The specific plan level (e.g., 'Bronze', 'Silver', 'Gold')
        public string TierId { get; set; } = string.Empty;
        
        // How much money the person makes in a year
        public decimal AnnualIncome { get; set; }
        
        // How often they want to pay (e.g., 'Monthly', 'Yearly')
        public string PaymentMode { get; set; } = "Yearly";

        // Details if only one person is being insured
        public ApplicantDetails? Applicant { get; set; }

        // Details for the main person if it's a family plan
        public ApplicantDetails? PrimaryApplicant { get; set; }
        
        // A list of other family members to be included in the policy
        public List<FamilyMemberRequest>? FamilyMembers { get; set; }
        
        // The person who gets the money if something happens to the insured person
        public NomineeRequest? Nominee { get; set; }
        
        // Where the primary applicant lives
        public LocationRequest? Location { get; set; }
    }

    /// <summary>
    /// Details about the person applying for insurance.
    /// </summary>
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

    /// <summary>
    /// Information needed for each family member in a group policy.
    /// </summary>
    public class FamilyMemberRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string HealthConditions { get; set; } = "None";
        public string? AadharNumber { get; set; }
        public string? AadharCardUrl { get; set; }
    }

    /// <summary>
    /// Information about the nominee (the beneficiary).
    /// </summary>
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

    /// <summary>
    /// Physical address and map location of the applicant.
    /// </summary>
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
