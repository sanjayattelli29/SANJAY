using InsurancePlatform.Data;
using InsurancePlatform.DTOs.Auth;
using InsurancePlatform.Helpers;
using InsurancePlatform.Models;
using InsurancePlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InsurancePlatform.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<string> RegisterCustomerAsync(RegisterCustomerDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    throw new Exception("Email already exists");

                var user = new User
                {
                    Id = Guid.NewGuid(), // Using inherited Id
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = PasswordHasher.HashPassword(dto.Password),
                    Role = UserRole.Customer,
                    IsActive = true,
                    IsApproved = true,
                    RequiresPasswordChange = false
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var customerProfile = new CustomerProfile
                {
                    Id = Guid.NewGuid(), // Using inherited Id
                    UserId = user.Id, // Mapping to standard Id
                    AadhaarNumber = dto.AadhaarNumber,
                    DateOfBirth = dto.DateOfBirth,
                    OccupationType = dto.OccupationType.HasValue ? (OccupationType)dto.OccupationType.Value : null,
                    AnnualIncome = dto.AnnualIncome,
                    RiskScore = 0,
                    RiskCategory = RiskCategory.Low
                };

                await _context.CustomerProfiles.AddAsync(customerProfile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return _jwtService.GenerateToken(user);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                throw new Exception("Invalid credentials");

            if (!PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            if (!user.IsActive)
                throw new Exception("Account is inactive");

            if (user.Role != UserRole.Customer && !user.IsApproved)
                throw new Exception("Account not approved by admin");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return _jwtService.GenerateToken(user);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!PasswordHasher.VerifyPassword(dto.OldPassword, user.PasswordHash))
                throw new Exception("Invalid old password");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new Exception("Passwords do not match");

            user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);
            user.RequiresPasswordChange = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}