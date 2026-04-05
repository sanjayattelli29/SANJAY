using System;

namespace Domain.Entities;


/// <summary>
/// This class represents a "Medical Bill" or "Evidence" uploaded for an Insurance Claim.
/// It links the actual file stored in the cloud (ImageKit) to the user's claim record.
/// </summary>
public class ClaimDocument
{
    // A unique ID identifying this specific document.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the Insurance Claim this document belongs to.
    public string ClaimId { get; set; } = string.Empty;
    
    // The secret ID given by the cloud storage provider (ImageKit).
    public string FileId { get; set; } = string.Empty; 

    // The name of the file (e.g., "hospital_receipt.jpg").
    public string FileName { get; set; } = string.Empty;

    // The direct web link to view the document.
    public string FileUrl { get; set; } = string.Empty;

    // How large the file is in bytes.
    public long FileSize { get; set; }

    // The date and time when the document was uploaded.
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // This links the document back to the full Claim details.
    public InsuranceClaim? Claim { get; set; }
}
