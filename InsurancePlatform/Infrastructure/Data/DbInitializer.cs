using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Data
{
    /// <summary>
    /// This class runs when the website first starts.
    /// It makes sure the database has the right "Roles" and a "Super Admin" account to begin with.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. Define the four basic jobs/roles in our system.
            var roles = new[] { UserRoles.Admin, UserRoles.Customer, UserRoles.Agent, UserRoles.ClaimOfficer };

            // 2. Check if these roles exist. If not, create them.
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Create the very first "Main Admin" user if they are not already there.
            var adminEmail = "admin@insurance.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Set up the admin profile details.
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                // Save the admin to the database with a default password.
                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                // If created successfully, give them the "Admin" role so they have full power.
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                }
            }
        }
    }
}

// this is the comeplte code