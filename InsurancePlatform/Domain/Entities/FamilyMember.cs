using System;

namespace Domain.Entities;

public class FamilyMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PolicyApplicationId { get; set; } = string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? ExistingHealthConditions { get; set; }
    
    [System.ComponentModel.DataAnnotations.MaxLength(12)]
    public string? AadhaarNumber { get; set; }
    public string? AadharCardUrl { get; set; }
    
    // Navigation
    public PolicyApplication? PolicyApplication { get; set; }
}
