using System;

namespace Domain.Entities;

// this class handles insurance claims filed by customers
public class InsuranceClaim
{
    // unique id for the claim
    public string Id { get; set; } = Guid.NewGuid().ToString();
    // the policy this claim is against
    public string PolicyApplicationId { get; set; } = string.Empty;
    // the user who is filing the claim
    public string UserId { get; set; } = string.Empty;
    
    // how much money the user wants
    public decimal RequestedAmount { get; set; }
    // how much money the officer approved
    public decimal ApprovedAmount { get; set; }
    // details of what happened
    public string Description { get; set; } = string.Empty;
    // when the incident happened
    public DateTime IncidentDate { get; set; }
    // when they submitted the claim
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    
    // why they are claiming like accident or sickness
    public string IncidentType { get; set; } = string.Empty;
    // where it happened
    public string IncidentLocation { get; set; } = string.Empty;
    // name of hospital if they went there
    public string HospitalName { get; set; } = string.Empty;
    // check if they were admitted to hospital
    public bool HospitalizationRequired { get; set; }

    // name of the family member who is hurt
    public string? AffectedMemberName { get; set; }
    // relation like wife or son
    public string? AffectedMemberRelation { get; set; }
    
    // status like pending or approved
    public string Status { get; set; } = "Pending"; 
    
    // notes from the officer
    public string? Remarks { get; set; }
    // who is checking this claim
    public string? AssignedClaimOfficerId { get; set; }
    // who finally approved it
    public string? ApprovedByOfficerId { get; set; }
    // when it was finished processing
    public DateTime? ProcessedAt { get; set; }

    // links to other objects
    public PolicyApplication? Policy { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? AssignedOfficer { get; set; }
    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
}
