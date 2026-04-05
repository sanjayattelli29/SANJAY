using System;

namespace Application.DTOs;

/// <summary>
/// This class is the "Common Record" for all payments and policy statuses.
/// It helps the system track who paid, how much, and who helped them (Agent/Officer).
/// </summary>
public class UnifiedPaymentDto
{
    // The unique ID of the insurance application
    public string ApplicationId { get; set; } = string.Empty;
    
    // The email address of the customer who bought the policy
    public string CustomerEmail { get; set; } = string.Empty;
    
    // The email of the Agent who sold this policy (if any)
    public string? AgentEmail { get; set; }
    
    // The email of the Claim Officer who handled this policy's claims (if any)
    public string? ClaimsOfficerEmail { get; set; }
    
    // The category of insurance (e.g., 'Health', 'Life')
    public string PolicyCategory { get; set; } = string.Empty;
    
    // The plan tier (e.g., 'Gold', 'Silver')
    public string TierId { get; set; } = string.Empty;
    
    // The total maximum money this policy can pay out
    public decimal TotalCoverage { get; set; }
    
    // The remaining coverage money available for future claims
    public decimal CurrentCoverage { get; set; }
    
    // The cost of the insurance policy (the price of the plan)
    public decimal PremiumAmount { get; set; }
    
    // How much money the user has actually paid so far
    public decimal? PaidAmount { get; set; }
    
    // The date when the user last made a payment
    public DateTime? LastPaymentDate { get; set; }
    
    // The unique transaction ID from the bank or payment gateway
    public string? TransactionId { get; set; }
    
    // How the user paid (e.g., 'Monthly', 'Yearly', 'OneTime')
    public string? PaymentMode { get; set; }
    
    // Current status of the payment (e.g., 'Success', 'Pending', 'Failed')
    public string Status { get; set; } = string.Empty;
}
