namespace Application.DTOs
{
    /// <summary>
    /// This class is used to send a Payment Invoice (Receipt) to the user.
    /// It sends the invoice as a PDF file encoded in 'Base64' text.
    /// </summary>
    public class UploadInvoiceDto
    {
        // The ID of the policy application this invoice is for
        public string ApplicationId { get; set; } = string.Empty;
        
        // The actual PDF contents of the invoice converted into text
        public string Base64Pdf { get; set; } = string.Empty;
        
        // The name given to the invoice file
        public string FileName { get; set; } = "invoice.pdf";
    }
}
