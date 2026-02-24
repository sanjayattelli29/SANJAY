using InsurancePlatform.Models;
using BCrypt.Net;

namespace InsurancePlatform.Data
{
    public static class DbSeeder
    {
        public static void SeedAdmin(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.Role == UserRole.Admin))
            {
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@insurance.com",
                    PhoneNumber = "9999999999",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }
    }
}