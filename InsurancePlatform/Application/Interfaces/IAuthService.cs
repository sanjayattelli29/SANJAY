using Application.DTOs;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for authentication and authorization services.
    /// Defines methods for user registration, login, and administrative user creation.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new customer in the system.
        /// </summary>
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);

        /// <summary>
        /// Authenticates a user and returns a JWT token if successful.
        /// </summary>
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Creates a new Agent. Restricted to Admin users.
        /// </summary>
        Task<object> CreateAgentAsync(CreateAgentDto agentDto);

        /// <summary>
        /// Creates a new Claim Officer. Restricted to Admin users.
        /// </summary>
        Task<object> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto);

        /// <summary>
        /// Returns a list of users in a specified role.
        /// </summary>
        Task<IEnumerable<object>> GetUsersByRoleAsync(string role);

        /// <summary>
        /// Deletes a user by their unique identifier.
        /// </summary>
        Task<object> DeleteUserAsync(string userId);
    }
}
