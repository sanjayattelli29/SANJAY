using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// This is our main User class. It handles login details and personal info.
    /// It can be a Customer, an Agent, or a Claims Officer.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // The full name of the user as they want it to appear.
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // The user's 12-digit Aadhaar number for identity verification.
        [MaxLength(12)]
        public string? AadhaarNumber { get; set; }

        // The bank account where an Agent or Officer gets paid their commissions/salary.
        [MaxLength(16)]
        public string? BankAccountNumber { get; set; }

        // The date and time when this user first joined the platform.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // A temporary password set by the admin when creating the account.
        public string? InitialPassword { get; set; }

        // "True" if the user has successfully finished their KYC (Know Your Customer) check.
        public bool IsKycVerified { get; set; } = false;

        // A link to the user's profile picture stored in the cloud.
        public string? ProfileImageUrl { get; set; }
    }
}
