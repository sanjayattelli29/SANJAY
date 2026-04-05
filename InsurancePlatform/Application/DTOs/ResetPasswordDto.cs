namespace Application.DTOs
{
    /// <summary>
    /// This class is used when a user has forgotten their password or wants to change it.
    /// It holds the email of the person and the brand new password they want.
    /// </summary>
    public class ResetPasswordDto
    {
        // The email address of the user who wants to reset their password
        public string Email { get; set; } = string.Empty;
        
        // The new password they want to use from now on
        public string NewPassword { get; set; } = string.Empty;
    }
}
