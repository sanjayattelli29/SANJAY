using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    // This is the main user class and we use this for all users in the system
    public class ApplicationUser : IdentityUser
    {
        // This is for the full name of the user
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // This is aadhar card number for customer verification
        [MaxLength(12)]
        public string? AadhaarNumber { get; set; }

        // This is bank account for agents and officers to get payments
        [MaxLength(16)]
        public string? BankAccountNumber { get; set; }

        // This is when the user was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // This stores the plain-text password created by admin for onboarding
        public string? InitialPassword { get; set; }

        public bool IsKycVerified { get; set; } = false;

        // This stores the ImageKit CDN URL of the user's profile photo
        public string? ProfileImageUrl { get; set; }
    }
}
