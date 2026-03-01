using Microsoft.AspNetCore.Identity; // identity library for users
using System.ComponentModel.DataAnnotations; // for validation rules

namespace Domain.Entities // where our database objects live
{
    // This is the main user class and we use this for all users in the system
    public class ApplicationUser : IdentityUser
    {
        // This is for the full name of the user
        [Required] // must not be empty
        [MaxLength(100)] // can't be more than 100 characters
        public string FullName { get; set; } = string.Empty;

        // This is aadhar card number for customer verification
        [MaxLength(12)]
        public string? AadhaarNumber { get; set; }

        // This is bank account for agents and officers to get payments
        [MaxLength(16)] // max 16 digits
        public string? BankAccountNumber { get; set; } // for payments
    }
}
// finished adding user details here
