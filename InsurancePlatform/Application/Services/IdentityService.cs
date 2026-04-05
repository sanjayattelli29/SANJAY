using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    /// This service handles all User Account and Security actions.
    /// It covers Sign-ups, Log-ins, and managing different types of users (Agents, Officers, etc.).
    public class IdentityService : IIdentityService
    {
        // Tools for managing users, roles, tokens, and making phone calls.
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IVapiService _vapiService;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IVapiService vapiService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _vapiService = vapiService;
        }

        /// This method creates a new Customer account.
        /// It also schedules an automated "Welcome Call" to the user's phone.
        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto)
        {
            // 1. Check if a user with this email already exists.
            var userExists = await _userManager.FindByEmailAsync(registerDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "User already exists!" };

            // 2. Prepare the user profile.
            ApplicationUser user = new()
            {
                Email = registerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerDto.EmailId,
                FullName = registerDto.Name,
                PhoneNumber = registerDto.MobileNumber
            };

            // 3. Save the user to the database with their chosen password.
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "User creation failed! Please check user details and try again." };

            // 4. Assign the 'Customer' role to the user.
            if (!await _roleManager.RoleExistsAsync(UserRoles.Customer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Customer));

            await _userManager.AddToRoleAsync(user, UserRoles.Customer);

            // 5. Start a background task to call the user's phone in 90 seconds.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(90));
                    await _vapiService.TriggerWelcomeCallAsync(user.PhoneNumber!, user.FullName!);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Vapi Call Trigger failed: {ex.Message}");
                }
            });

            return new AuthResponseDto { Status = "Success", Message = "Customer created successfully!" };
        }

        /// This method checks if the email and password are correct.
        /// If yes, it creates a "Security Badge" (JWT Token) so the user can use the app.
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // 1. Find the user and verify their password.
            var user = await _userManager.FindByEmailAsync(loginDto.EmailId);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                // 2. Collect the user's role and details to put inside the token.
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                // 3. Generate the actual security token.
                var token = _tokenService.CreateToken(authClaims);

                // 4. Return the token and profile details back to the user.
                return new AuthResponseDto
                {
                    Status = "Success",
                    Token = _tokenService.WriteToken(token),
                    Expiration = token.ValidTo,
                    Role = userRoles.FirstOrDefault(),
                    Email = user.Email,
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    IsKycVerified = user.IsKycVerified,
                    ProfileImageUrl = user.ProfileImageUrl
                };
            }
            return new AuthResponseDto { Status = "Error", Message = "Invalid login attempt." };
        }

        // Create a new Agent profile (Admin only).
        public async Task<AuthResponseDto> CreateAgentAsync(CreateAgentDto agentDto)
        {
            var userExists = await _userManager.FindByEmailAsync(agentDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "Agent already exists!" };

            ApplicationUser user = new()
            {
                Email = agentDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = agentDto.EmailId,
                FullName = agentDto.Name,
                BankAccountNumber = agentDto.BankAccountNumber,
                InitialPassword = agentDto.Password
            };

            var result = await _userManager.CreateAsync(user, agentDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Agent creation failed!" };

            if (!await _roleManager.RoleExistsAsync(UserRoles.Agent))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Agent));

            await _userManager.AddToRoleAsync(user, UserRoles.Agent);

            return new AuthResponseDto { Status = "Success", Message = "Agent created successfully!" };
        }

        // Create a new Claim Officer profile (Admin only).
        public async Task<AuthResponseDto> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(claimOfficerDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "Claim Officer already exists!" };

            ApplicationUser user = new()
            {
                Email = claimOfficerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = claimOfficerDto.EmailId,
                FullName = claimOfficerDto.Name,
                BankAccountNumber = claimOfficerDto.BankAccountNumber,
                InitialPassword = claimOfficerDto.Password
            };

            var result = await _userManager.CreateAsync(user, claimOfficerDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Claim Officer creation failed!" };

            if (!await _roleManager.RoleExistsAsync(UserRoles.ClaimOfficer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.ClaimOfficer));

            await _userManager.AddToRoleAsync(user, UserRoles.ClaimOfficer);

            return new AuthResponseDto { Status = "Success", Message = "Claim Officer created successfully!" };
        }

        // Get a list of all users with a specific job (like 'Agents').
        public async Task<IEnumerable<UserListingDto>> GetUsersByRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            return users.Select(u => new UserListingDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                BankAccountNumber = u.BankAccountNumber
            });
        }

        // Get a list of every single user in the system.
        public async Task<IEnumerable<UserListingDto>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserListingDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserListingDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    BankAccountNumber = user.BankAccountNumber,
                    Roles = roles,
                    Role = roles.FirstOrDefault() ?? "Partner",
                    CreatedDate = user.CreatedAt,
                    InitialPassword = user.InitialPassword
                });
            }

            return result;
        }

        // Remove a user account.
        public async Task<AuthResponseDto> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "User deletion failed!" };

            return new AuthResponseDto { Status = "Success", Message = "User deleted successfully!" };
        }

        // Mark a customer as ID-Verified.
        public async Task<AuthResponseDto> CompleteKycAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            user.IsKycVerified = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to update KYC status!" };

            return new AuthResponseDto { Status = "Success", Message = "KYC Verified!" };
        }

        // Change the user's profile image link.
        public async Task<AuthResponseDto> UpdateProfileImageAsync(string userId, string imageUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            user.ProfileImageUrl = imageUrl;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to update profile image!" };

            return new AuthResponseDto { Status = "Success", Message = "Profile image updated!", ProfileImageUrl = imageUrl };
        }

        // Reset a user's password to a brand new one.
        public async Task<AuthResponseDto> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to reset password!" };

            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to set new password!" };

            return new AuthResponseDto { Status = "Success", Message = "Password reset successfully!" };
        }
    }
}
