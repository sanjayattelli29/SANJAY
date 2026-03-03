using System;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

// these tests show how to use InMemory database for integration testing
public class ApplicationDbContextTests
{
    [Fact]
    // this test checks if we can save and read a policy from the database
    public async Task CanAddAndRetrievePolicy()
    {
        // arrange - setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_AddPolicy")
            .Options;

        using (var context = new ApplicationDbContext(options))
        {
            var policy = new PolicyApplication
            {
                Id = "test-policy-1",
                PolicyCategory = "Term Life",
                Status = "Active",
                UserId = "user-1"
            };

            context.PolicyApplications.Add(policy);
            await context.SaveChangesAsync();
        }

        // act & assert - check if it's there
        using (var context = new ApplicationDbContext(options))
        {
            var retrievedPolicy = await context.PolicyApplications.FindAsync("test-policy-1");
            Assert.NotNull(retrievedPolicy);
            Assert.Equal("Term Life", retrievedPolicy.PolicyCategory);
        }
    }
}
