using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for handling authentication, user registration, and role-based user creation.
    /// Implements JWT token generation and ASP.NET Core Identity integration.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new customer with the 'Customer' role.
        /// </summary>
        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "User already exists!" };

            ApplicationUser user = new()
            {
                Email = registerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerDto.EmailId,
                FullName = registerDto.Name,
                PhoneNumber = registerDto.MobileNumber
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "User creation failed! Please check user details and try again." };

            if (!await _roleManager.RoleExistsAsync(UserRoles.Customer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Customer));

            await _userManager.AddToRoleAsync(user, UserRoles.Customer);

            return new AuthResponseDto { Status = "Success", Message = "Customer created successfully!" };
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token containing UserId, Email, and Role claims.
        /// </summary>
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.EmailId);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
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

                var token = CreateToken(authClaims);

                return new AuthResponseDto
                {
                    Status = "Success",
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    Role = userRoles.FirstOrDefault(),
                    Email = user.Email
                };
            }
            return new AuthResponseDto { Status = "Error", Message = "Invalid login attempt." };
        }

        /// <summary>
        /// Creates a new Agent. Restricted to Admin via controller.
        /// </summary>
        public async Task<object> CreateAgentAsync(CreateAgentDto agentDto)
        {
            var userExists = await _userManager.FindByEmailAsync(agentDto.EmailId);
            if (userExists != null)
                return new { Status = "Error", Message = "Agent already exists!" };

            ApplicationUser user = new()
            {
                Email = agentDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = agentDto.EmailId,
                FullName = agentDto.Name,
                BankAccountNumber = agentDto.BankAccountNumber
            };

            var result = await _userManager.CreateAsync(user, agentDto.Password);
            if (!result.Succeeded)
                return new { Status = "Error", Message = "Agent creation failed!" };

            if (!await _roleManager.RoleExistsAsync(UserRoles.Agent))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Agent));

            await _userManager.AddToRoleAsync(user, UserRoles.Agent);

            return new { Status = "Success", Message = "Agent created successfully!" };
        }

        /// <summary>
        /// Creates a new Claim Officer. Restricted to Admin via controller.
        /// </summary>
        public async Task<object> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(claimOfficerDto.EmailId);
            if (userExists != null)
                return new { Status = "Error", Message = "Claim Officer already exists!" };

            ApplicationUser user = new()
            {
                Email = claimOfficerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = claimOfficerDto.EmailId,
                FullName = claimOfficerDto.Name,
                BankAccountNumber = claimOfficerDto.BankAccountNumber
            };

            var result = await _userManager.CreateAsync(user, claimOfficerDto.Password);
            if (!result.Succeeded)
                return new { Status = "Error", Message = "Claim Officer creation failed!" };

            if (!await _roleManager.RoleExistsAsync(UserRoles.ClaimOfficer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.ClaimOfficer));

            await _userManager.AddToRoleAsync(user, UserRoles.ClaimOfficer);

            return new { Status = "Success", Message = "Claim Officer created successfully!" };
        }

        /// <summary>
        /// Returns a list of users belonging to a specific role.
        /// </summary>
        public async Task<IEnumerable<object>> GetUsersByRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            return users.Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.BankAccountNumber
            });
        }

        /// <summary>
        /// Deletes a user from the system.
        /// </summary>
        public async Task<object> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new { Status = "Error", Message = "User not found!" };

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return new { Status = "Error", Message = "User deletion failed!" };

            return new { Status = "Success", Message = "User deleted successfully!" };
        }

        /// <summary>
        /// Generates a JWT token based on user claims and settings in appsettings.json.
        /// </summary>
        private JwtSecurityToken CreateToken(List<System.Security.Claims.Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryMinutes"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}
