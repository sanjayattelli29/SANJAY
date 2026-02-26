using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class PolicyApplication
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string PolicyCategory { get; set; } = string.Empty; // INDIVIDUAL or FAMILY
    public string TierId { get; set; } = string.Empty;
    public decimal CalculatedPremium { get; set; }
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Assigned, Approved, Rejected

    // JSON blob of the submission details for flexibility
    public string ApplicationDataJson { get; set; } = string.Empty;

    // Assignment details
    public string? AssignedAgentId { get; set; }
    public string? ApprovedByAgentId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Financial Tracking
    public decimal TotalCoverageAmount { get; set; }
    public decimal RemainingCoverageAmount { get; set; }
    public decimal TotalApprovedClaimsAmount { get; set; }

    // Payment & Activation Details
    public string? PaymentMode { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? TransactionId { get; set; }

    // Relations
    public ApplicationUser? User { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }
}
