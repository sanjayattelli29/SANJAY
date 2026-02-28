using System;

namespace Application.DTOs;

public class UnifiedPaymentDto
{
    public string ApplicationId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? AgentEmail { get; set; }
    public string? ClaimsOfficerEmail { get; set; }
    public string PolicyCategory { get; set; } = string.Empty;
    public string TierId { get; set; } = string.Empty;
    public decimal TotalCoverage { get; set; }
    public decimal CurrentCoverage { get; set; }
    public decimal PremiumAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentMode { get; set; }
    public string Status { get; set; } = string.Empty;
}
