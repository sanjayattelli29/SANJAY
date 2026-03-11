using System;

namespace Domain.Entities;

public class ApplicationDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PolicyApplicationId { get; set; } = string.Empty;
    
    public string DocumentType { get; set; } = string.Empty; // IdentityProof, AgeProof, etc.
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? CloudKey { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PolicyApplication? PolicyApplication { get; set; }
}
