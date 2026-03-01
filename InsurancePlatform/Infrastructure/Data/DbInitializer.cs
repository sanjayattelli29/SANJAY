using Domain.Entities; // for user class
using Domain.Enums; // for role names
using Microsoft.AspNetCore.Identity; // for identity managers

namespace Infrastructure.Data
{
    // this class creates the first set of data when website starts
    public static class DbInitializer
    {
        // this method runs to setup roles and admin user
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // setup all user roles like Admin and Customer
            // we loop through these strings
            var roles = new[] { UserRoles.Admin, UserRoles.Customer, UserRoles.Agent, UserRoles.ClaimOfficer };

            foreach (var role in roles)
            {
                // check if role already exists
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // create a main admin user if not already there
            var adminEmail = "admin@insurance.com"; // login email
            var adminUser = await userManager.FindByEmailAsync(adminEmail); // look for admin

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail, // set email
                    FullName = "System Administrator", // set name
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                // password for first login is Admin@123
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin); // set admin role
                }
            }
        }
    }
}
// end of database seeder
