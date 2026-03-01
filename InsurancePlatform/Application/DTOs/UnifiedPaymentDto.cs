using System; // system namespace

namespace Application.DTOs; // dto namespace

// this class is used for tracking all transactions and payments
public class UnifiedPaymentDto
{
    // unique id of policy application
    public string ApplicationId { get; set; } = string.Empty; // internal policy id
    // email of the customer who paid
    public string CustomerEmail { get; set; } = string.Empty;
    // email of agent if involved
    public string? AgentEmail { get; set; }
    // email of officer if involved
    public string? ClaimsOfficerEmail { get; set; }
    // category like health or life
    public string PolicyCategory { get; set; } = string.Empty; // category name
    // tier like gold or silver
    public string TierId { get; set; } = string.Empty;
    // total amount the policy covers
    public decimal TotalCoverage { get; set; }
    // current money left in policy
    public decimal CurrentCoverage { get; set; } // remaining money
    // cost of policy premium
    public decimal PremiumAmount { get; set; } // cost of plan
    // how much they actually paid
    public decimal? PaidAmount { get; set; }
    // when next payment is due
    public DateTime? NextPaymentDate { get; set; }
    // when they last made a payment
    public DateTime? LastPaymentDate { get; set; }
    // reference id from bank
    public string? TransactionId { get; set; } // bank reference
    // how they paid like monthly
    public string? PaymentMode { get; set; }
    // status of payment like success
    public string Status { get; set; } = string.Empty; // success/fail
}
// unified payment data ends
