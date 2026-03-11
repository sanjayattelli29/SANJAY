using System;

namespace Domain.Entities;

public class NomineeDetails
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PolicyApplicationId { get; set; } = string.Empty;

    public string NomineeName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string NomineePhone { get; set; } = string.Empty;
    public string NomineeEmail { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string IFSC { get; set; } = string.Empty;

    // Navigation
    public PolicyApplication? PolicyApplication { get; set; }
}
