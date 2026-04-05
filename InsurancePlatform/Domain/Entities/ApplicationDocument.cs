using System;

namespace Domain.Entities;

/// <summary>
/// This class represents a document (like a PDF or Image) uploaded for an Insurance Policy.
/// It keeps track of where the file is stored and what kind of proof it provides.
/// </summary>
public class ApplicationDocument
{
    // A unique ID for this specific document record.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the Policy Application this document belongs to.
    public string PolicyApplicationId { get; set; } = string.Empty;
    
    // The category of the document, like "IdentityProof" or "IncomeProof".
    public string DocumentType { get; set; } = string.Empty; 

    // The original name of the file (e.g., "my_aadhar.pdf").
    public string FileName { get; set; } = string.Empty;

    // The direct web link to view or download the file.
    public string FileUrl { get; set; } = string.Empty;

    // A secret key used by the cloud storage (like ImageKit) to manage the file.
    public string? CloudKey { get; set; }

    // How big the file is in bytes.
    public long FileSize { get; set; }

    // When the file was first uploaded to our system.
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // This links the document back to the full Policy Application details.
    public PolicyApplication? PolicyApplication { get; set; }
}
