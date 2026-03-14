using System;
using System.Collections.Generic;

namespace Domain.Entities;

// this class is for policy applications submitted by customers
public class PolicyApplication
{
    // unique id for the application
    public string Id { get; set; } = Guid.NewGuid().ToString();
    // the user who wants the policy
    public string UserId { get; set; } = string.Empty;
    // type of policy like individual or family
    public string PolicyCategory { get; set; } = string.Empty; 
    // which tier like gold or silver
    public string TierId { get; set; } = string.Empty;
    // cost of the policy calculated by system
    public decimal CalculatedPremium { get; set; }
    // when it was submitted
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    // status like pending or approved
    public string Status { get; set; } = "Pending"; 

    // structured underwriting data
    public int Age { get; set; }
    public string Profession { get; set; } = string.Empty;
    public decimal AnnualIncome { get; set; }
    public string AlcoholHabit { get; set; } = string.Empty;
    public string SmokingHabit { get; set; } = string.Empty;
    public int TravelKmPerMonth { get; set; }
    public string VehicleType { get; set; } = "None";

    // Location components
    public string? Address { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
    public string? Pincode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // all form data saved as json string (backup/summary)
    public string ApplicationDataJson { get; set; } = string.Empty;

    // who is handling this application
    public string? AssignedAgentId { get; set; }
    public string? ApprovedByAgentId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // ... (rest of the properties stay same)
    public decimal TotalCoverageAmount { get; set; }
    public decimal RemainingCoverageAmount { get; set; }
    public decimal TotalApprovedClaimsAmount { get; set; }

    public string? PaymentMode { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? InvoiceUrl { get; set; }


    // relational links
    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public NomineeDetails? Nominee { get; set; }
    public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();

    // links to other objects
    public ApplicationUser? User { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }
}
