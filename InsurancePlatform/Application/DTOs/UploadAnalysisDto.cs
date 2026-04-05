namespace Application.DTOs
{
    /// <summary>
    /// This class is used when the system generates an AI-powered Risk Analysis report.
    /// It sends the report as a PDF file encoded in 'Base64' text.
    /// </summary>
    public class UploadAnalysisDto
    {
        // The ID of the policy application this analysis belongs to
        public string ApplicationId { get; set; } = string.Empty;
        
        // The actual PDF contents converted into a long string of text
        public string Base64Pdf { get; set; } = string.Empty;
        
        // The name of the file (usually 'analysis_report.pdf')
        public string FileName { get; set; } = "analysis_report.pdf";
    }
}
