using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when a customer needs to upload their physical documents
    /// (like Aadhar card or Salary slips) to prove their identity and income.
    /// </summary>
    public class SubmitPolicyDocumentsRequest
    {
        // The ID of the policy application these documents belong to
        public string PolicyApplicationId { get; set; } = string.Empty;
        
        // A list of one or more documents being uploaded at the same time
        public List<DocumentUploadDto> Documents { get; set; } = new();
    }

    /// <summary>
    /// A small class to hold a single file and its type.
    /// </summary>
    public class DocumentUploadDto
    {
        // What kind of document is this? (e.g., 'IdentityProof', 'IncomeProof')
        public string DocumentType { get; set; } = string.Empty;
        
        // The actual file (PDF or Image) being uploaded
        public IFormFile File { get; set; } = null!;
    }
}
