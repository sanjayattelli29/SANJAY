using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when an Admin needs to create a new Claim Officer.
    /// Claim Officers are responsible for reviewing and approving insurance claims.
    /// </summary>
    public class CreateClaimOfficerDto
    {
        // The full name of the new claim officer
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The email address for the officer's login
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // The initial login password for the officer
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // The 16-digit bank account number for the officer's salary deposits
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Bank account number must be 16 digits.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Bank account number must be 16 digits.")]
        public string BankAccountNumber { get; set; } = string.Empty;
    }
}
