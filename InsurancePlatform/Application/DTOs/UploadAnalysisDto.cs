namespace Application.DTOs
{
    public class UploadAnalysisDto
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Base64Pdf { get; set; } = string.Empty;
        public string FileName { get; set; } = "analysis_report.pdf";
    }
}
