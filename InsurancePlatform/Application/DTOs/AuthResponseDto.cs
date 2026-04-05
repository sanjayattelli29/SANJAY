namespace Application.DTOs
{
    /// <summary>
    /// This class is the "Receipt" or "Welcome Package" given to a user after they log in or register.
    /// It contains their security token (like a digital ID card) and their basic profile info.
    /// </summary>
    public class AuthResponseDto
    {
        // Tells us if the login was a "Success" or an "Error"
        public string? Status { get; set; }
        
        // A friendly message like "Welcome back, Sanjay!" or an error message
        public string? Message { get; set; }
        
        // The digital key (JWT Token) that allows the user to access protected parts of the app
        public string? Token { get; set; }
        
        // The date and time when this digital key will stop working
        public DateTime? Expiration { get; set; }
        
        // The user's role (e.g., 'Admin', 'Agent', 'ClaimOfficer', or 'Customer')
        public string? Role { get; set; }
        
        // The email address the user logged in with
        public string? Email { get; set; }
        
        // The unique database ID for this user
        public string? Id { get; set; }
        
        // The user's full name
        public string? FullName { get; set; }
        
        // The user's contact phone number
        public string? PhoneNumber { get; set; }
        
        // True if the user has completed their KYC (Know Your Customer) verification
        public bool IsKycVerified { get; set; }
        
        // The web link to the user's profile picture
        public string? ProfileImageUrl { get; set; }
    }
}
