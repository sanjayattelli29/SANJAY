using Domain.Entities;
using Xunit;

namespace Domain.Tests;

// these tests verify that our entities behave correctly
public class InsuranceClaimTests
{
    [Fact]
    // this test checks if a new claim has the pending status by default
    public void NewClaim_ShouldHavePendingStatus()
    {
        // arrange
        var claim = new InsuranceClaim();

        // assert
        Assert.Equal("Pending", claim.Status);
    }

    [Fact]
    // this test checks if a new claim gets a unique id automatically
    public void NewClaim_ShouldHaveUniqueId()
    {
        // arrange
        var claim1 = new InsuranceClaim();
        var claim2 = new InsuranceClaim();

        // assert
        Assert.NotNull(claim1.Id);
        Assert.NotEmpty(claim1.Id);
        Assert.NotEqual(claim1.Id, claim2.Id);
    }
}
