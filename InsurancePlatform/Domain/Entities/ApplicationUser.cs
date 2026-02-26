using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Represents the custom application user which extends the default IdentityUser.
    /// Includes additional properties for full name, Aadhaar number, and bank account details.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Aadhaar Number is only required for Customer users. optional for others.
        /// </summary>
        [MaxLength(12)]
        public string? AadhaarNumber { get; set; }

        /// <summary>
        /// Bank Account Number is required for Agent and ClaimOfficer.
        /// Must support 16 digits.
        /// </summary>
        [MaxLength(16)]
        public string? BankAccountNumber { get; set; }
    }
}
