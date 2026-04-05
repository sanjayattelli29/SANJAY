using System;

namespace Domain.Entities;


/// <summary>
/// This class represents a "Nominee" (the person who gets the insurance money if the owner dies).
/// It contains their contact and bank details for smooth payouts.
/// </summary>
public class NomineeDetails
{
    // A unique ID identifying this nominee record.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // The ID of the Policy Application this nominee is linked to.
    public string PolicyApplicationId { get; set; } = string.Empty;

    // Full name of the nominee as per their Aadhaar card.
    public string NomineeName { get; set; } = string.Empty;

    // Relationship to the main user (e.g., "Mother", "Wife", "Child").
    public string Relationship { get; set; } = string.Empty;

    // The phone number to contact the nominee.
    public string NomineePhone { get; set; } = string.Empty;

    // The email address of the nominee.
    public string NomineeEmail { get; set; } = string.Empty;

    // The bank account number where claim money will be sent for this nominee.
    public string BankAccountNumber { get; set; } = string.Empty;

    // The IFSC code of the nominee's bank.
    public string IFSC { get; set; } = string.Empty;
    
    // Their 12-digit Aadhaar number for identity verification.
    public string AadharNumber { get; set; } = string.Empty;

    // A web link to a picture of their Aadhaar card.
    public string AadharCardUrl { get; set; } = string.Empty;

    // Links back to the full Policy Application details.
    public PolicyApplication? PolicyApplication { get; set; }
}
