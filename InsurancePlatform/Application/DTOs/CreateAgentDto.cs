using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when an Admin needs to create a new Insurance Agent in the system.
    /// It ensures all required information is provided before the agent is saved.
    /// </summary>
    public class CreateAgentDto
    {
        // The full name of the new agent
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The email address that the agent will use for logging in
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // The initial password for the agent's account
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // The 16-digit bank account number where the agent's commissions will be sent
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Bank account number must be 16 digits.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Bank account number must be 16 digits.")]
        public string BankAccountNumber { get; set; } = string.Empty;
    }
}
