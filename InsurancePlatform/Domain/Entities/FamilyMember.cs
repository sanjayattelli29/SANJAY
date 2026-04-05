using System;

namespace Domain.Entities;


/// <summary>
/// This class stores details about a "Family Member" (like a Spouse or Child).
/// They are added to "Family" insurance plans so they are also covered.
/// </summary>
public class FamilyMember
{
    // A unique ID for this family member record.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the Policy Application they are included in.
    public string PolicyApplicationId { get; set; } = string.Empty;
    
    // Their full name as per government ID.
    public string FullName { get; set; } = string.Empty;

    // Their relationship to the main user (e.g., "Wife", "Husband", "Son").
    public string Relation { get; set; } = string.Empty;

    // Their date of birth (used to check eligibility or extra costs).
    public DateTime DateOfBirth { get; set; }

    // Any illnesses or health issues they already have (Pre-existing conditions).
    public string? ExistingHealthConditions { get; set; }
    
    // Their 12-digit Aadhaar number for verification.
    [System.ComponentModel.DataAnnotations.MaxLength(12)]
    public string? AadhaarNumber { get; set; }

    // A web link to a picture of their Aadhaar card.
    public string? AadharCardUrl { get; set; }
    
    // Links back to the full Policy Application details.
    public PolicyApplication? PolicyApplication { get; set; }
}
