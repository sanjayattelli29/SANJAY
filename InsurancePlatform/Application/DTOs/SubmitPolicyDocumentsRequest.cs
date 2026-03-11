using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class SubmitPolicyDocumentsRequest
    {
        public string PolicyApplicationId { get; set; } = string.Empty;
        public List<DocumentUploadDto> Documents { get; set; } = new();
    }

    public class DocumentUploadDto
    {
        public string DocumentType { get; set; } = string.Empty; // IdentityProof, AgeProof, IncomeProof, MedicalReport
        public IFormFile File { get; set; } = null!;
    }
}
