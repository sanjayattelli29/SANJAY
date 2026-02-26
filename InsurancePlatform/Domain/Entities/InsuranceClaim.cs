using System;

namespace Domain.Entities;

public class InsuranceClaim
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PolicyApplicationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    
    // Detailed Incident Info
    public string IncidentType { get; set; } = string.Empty;
    public string IncidentLocation { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public bool HospitalizationRequired { get; set; }

    // Family Info (if applicable)
    public string? AffectedMemberName { get; set; }
    public string? AffectedMemberRelation { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending, Assigned, Approved, Rejected, Paid
    
    // Assignment & Processing
    public string? Remarks { get; set; }
    public string? AssignedClaimOfficerId { get; set; }
    public string? ApprovedByOfficerId { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Relations
    public PolicyApplication? Policy { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? AssignedOfficer { get; set; }
    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
}
