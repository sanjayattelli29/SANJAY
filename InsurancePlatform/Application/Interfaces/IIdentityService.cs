using Application.DTOs;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface handles everything related to Users and Security.
    /// It covers things like Signing Up, Logging In, and managing user roles.
    /// </summary>
    public interface IIdentityService
    {
        // Allow a new customer to create an account
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);

        // Check if a user's email and password are correct and let them in
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        // Allow an Admin to create a new Agent profile
        Task<AuthResponseDto> CreateAgentAsync(CreateAgentDto agentDto);

        // Allow an Admin to create a new Claim Officer profile
        Task<AuthResponseDto> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto);

        // Get a list of all users who have a specific job (like all 'Agents')
        Task<IEnumerable<UserListingDto>> GetUsersByRoleAsync(string role);

        // Get a list of every single person registered in the system
        Task<IEnumerable<UserListingDto>> GetAllUsersAsync();

        // Completely remove a user from the system
        Task<AuthResponseDto> DeleteUserAsync(string userId);

        // Mark a user as having successfully completed their ID verification (KYC)
        Task<AuthResponseDto> CompleteKycAsync(string userId);

        // Change a user's profile picture link
        Task<AuthResponseDto> UpdateProfileImageAsync(string userId, string imageUrl);

        // Change a user's password (used when they forget it)
        Task<AuthResponseDto> ResetPasswordAsync(string email, string newPassword);
    }

    /// <summary>
    /// This class is used to show a summary of a user's information in a list.
    /// </summary>
    public class UserListingDto
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? Role { get; set; }
        public IEnumerable<string>? Roles { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? InitialPassword { get; set; }
    }
}
