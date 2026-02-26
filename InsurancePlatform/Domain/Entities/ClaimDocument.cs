using System;

namespace Domain.Entities;

public class ClaimDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClaimId { get; set; } = string.Empty;
    
    public string FileId { get; set; } = string.Empty; // ImageKit FileID
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public InsuranceClaim? Claim { get; set; }
}
