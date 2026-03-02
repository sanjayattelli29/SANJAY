using System;
using System.Collections.Generic;

namespace Domain.Entities;

// this class is for policy applications submitted by customers
public class PolicyApplication
{
    // unique id for the application
    public string Id { get; set; } = Guid.NewGuid().ToString();
    // the user who wants the policy
    public string UserId { get; set; } = string.Empty;
    // type of policy like individual or family
    public string PolicyCategory { get; set; } = string.Empty; 
    // which tier like gold or silver
    public string TierId { get; set; } = string.Empty;
    // cost of the policy calculated by system
    public decimal CalculatedPremium { get; set; }
    // when it was submitted
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    // status like pending or approved
    public string Status { get; set; } = "Pending"; 

    // all form data saved as json string
    public string ApplicationDataJson { get; set; } = string.Empty;

    // who is handling this application
    public string? AssignedAgentId { get; set; }
    // who approved the application
    public string? ApprovedByAgentId { get; set; }
    // when it was approved
    public DateTime? ApprovedAt { get; set; }

    // total money covered by this policy
    public decimal TotalCoverageAmount { get; set; }
    // how much coverage is left
    public decimal RemainingCoverageAmount { get; set; }
    // total amount of claims already paid
    public decimal TotalApprovedClaimsAmount { get; set; }

    // how they pay like monthly or yearly
    public string? PaymentMode { get; set; }
    // when the next payment is due
    public DateTime? NextPaymentDate { get; set; }
    // when policy starts working
    public DateTime? StartDate { get; set; }
    // when policy ends
    public DateTime? ExpiryDate { get; set; }
    // how much they actually paid
    public decimal? PaidAmount { get; set; }
    // when they made the payment
    public DateTime? PaymentDate { get; set; }
    // id from the bank or payment gateway
    public string? TransactionId { get; set; }

    // links to other objects
    public ApplicationUser? User { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }
}
