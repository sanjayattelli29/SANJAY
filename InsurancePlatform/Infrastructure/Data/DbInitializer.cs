using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Data
{
    /// <summary>
    /// Static class for seeding initial data into the database, such as roles and a default admin user.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            var roles = new[] { UserRoles.Admin, UserRoles.Customer, UserRoles.Agent, UserRoles.ClaimOfficer };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@insurance.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                }
            }

            // Seed a Claim Officer (to ensure the dropdown is not empty)
            var officerEmail = "officer@insurance.com";
            var officerUser = await userManager.FindByEmailAsync(officerEmail);

            if (officerUser == null)
            {
                officerUser = new ApplicationUser
                {
                    UserName = officerEmail,
                    Email = officerEmail,
                    FullName = "Default Claim Officer",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(officerUser, "Officer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(officerUser, UserRoles.ClaimOfficer);
                }
            }
        }
    }
}
