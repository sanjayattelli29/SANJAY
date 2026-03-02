using System;

namespace Domain.Entities;

// this class is for documents uploaded for an insurance claim
public class ClaimDocument
{
    // unique id for the document record
    public string Id { get; set; } = Guid.NewGuid().ToString();
    // which claim this document is for
    public string ClaimId { get; set; } = string.Empty;
    
    // the id from imagekit service
    public string FileId { get; set; } = string.Empty; 
    // name of the file
    public string FileName { get; set; } = string.Empty;
    // web url to see the file
    public string FileUrl { get; set; } = string.Empty;
    // size of file in bytes
    public long FileSize { get; set; }
    // when it was uploaded
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // link to the insurance claim object
    public InsuranceClaim? Claim { get; set; }
}
