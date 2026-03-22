using System.Collections.Generic;
using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Xunit;

namespace Application.Tests;

public class ApplicationDtoTests
{
    // ─── AuthResponseDto Tests ───

    [Fact]
    public void AuthResponseDto_CanSetStatus()
    {
        var dto = new AuthResponseDto { Status = "Success" };
        Assert.Equal("Success", dto.Status);
    }

    [Fact]
    public void AuthResponseDto_CanSetToken()
    {
        var dto = new AuthResponseDto { Token = "jwt-token" };
        Assert.Equal("jwt-token", dto.Token);
    }

    [Fact]
    public void AuthResponseDto_CanSetRole()
    {
        var dto = new AuthResponseDto { Role = "Customer" };
        Assert.Equal("Customer", dto.Role);
    }

    [Fact]
    public void AuthResponseDto_DefaultsAreNull()
    {
        var dto = new AuthResponseDto();
        Assert.Null(dto.Token);
        Assert.Null(dto.Email);
    }

    // ─── PolicyApplicationRequest Tests ───

    [Fact]
    public void PolicyApplicationRequest_DefaultCategory_IsEmpty()
    {
        var req = new PolicyApplicationRequest();
        Assert.Equal(string.Empty, req.PolicyCategory);
    }

    [Fact]
    public void PolicyApplicationRequest_CanSetCategory()
    {
        var req = new PolicyApplicationRequest { PolicyCategory = "INDIVIDUAL" };
        Assert.Equal("INDIVIDUAL", req.PolicyCategory);
    }

    [Fact]
    public void PolicyApplicationRequest_CanSetApplicant()
    {
        var req = new PolicyApplicationRequest { Applicant = new ApplicantDetails { Age = 30 } };
        Assert.Equal(30, req.Applicant?.Age);
    }

    // ─── ApplicantDetails Tests ───

    [Fact]
    public void ApplicantDetails_DefaultAge_IsZero()
    {
        var app = new ApplicantDetails();
        Assert.Equal(0, app.Age);
    }

    [Fact]
    public void ApplicantDetails_CanSetProfession()
    {
        var app = new ApplicantDetails { Profession = "Engineer" };
        Assert.Equal("Engineer", app.Profession);
    }

    // ─── RaiseClaimRequest Tests ───

    [Fact]
    public void RaiseClaimRequest_DefaultPolicyId_IsEmpty()
    {
        var req = new RaiseClaimRequest();
        Assert.Equal(string.Empty, req.PolicyApplicationId);
    }

    [Fact]
    public void RaiseClaimRequest_DefaultDocuments_IsNull()
    {
        var req = new RaiseClaimRequest();
        Assert.Null(req.Documents);
    }

    [Fact]
    public void RaiseClaimRequest_CanSetIncidentType()
    {
        var req = new RaiseClaimRequest { IncidentType = "Accident" };
        Assert.Equal("Accident", req.IncidentType);
    }

    // ─── UserListingDto Tests ───

    [Fact]
    public void UserListingDto_CanSetEmail()
    {
        var dto = new UserListingDto { Email = "user@test.com" };
        Assert.Equal("user@test.com", dto.Email);
    }

    [Fact]
    public void UserListingDto_DefaultId_IsEmpty()
    {
        var dto = new UserListingDto();
        Assert.Equal(string.Empty, dto.Id);
    }

    // ─── ClaimOfficerWorkloadDto Tests ───

    [Fact]
    public void ClaimOfficerWorkloadDto_CanSetCount()
    {
        var dto = new ClaimOfficerWorkloadDto { AssignedClaimsCount = 5 };
        Assert.Equal(5, dto.AssignedClaimsCount);
    }

    // ─── AgentAnalyticsDto Tests ───

    [Fact]
    public void AgentAnalyticsDto_ListsDefaultToEmpty()
    {
        var dto = new AgentAnalyticsDto();
        Assert.NotNull(dto.PortfolioMix);
        Assert.NotNull(dto.TierBreakdown);
        Assert.NotNull(dto.CommissionPerformance);
    }

    [Fact]
    public void AgentAnalyticsDto_CanSetTotals()
    {
        var dto = new AgentAnalyticsDto { TotalPremiumCollected = 5000, TotalCommissionEarned = 500, TotalCoverageProvided = 100000 };
        Assert.Equal(5000, dto.TotalPremiumCollected);
        Assert.Equal(500, dto.TotalCommissionEarned);
        Assert.Equal(100000, dto.TotalCoverageProvided);
    }

    // ─── AdminDashboardStatsDto Tests ───

    [Fact]
    public void AdminDashboardStatsDto_DefaultCount_IsZero()
    {
        var stats = new AdminDashboardStatsDto();
        Assert.Equal(0, stats.TotalPolicies);
        Assert.Equal(0, stats.TotalClaims);
    }

    [Fact]
    public void AdminDashboardStatsDto_CanSetAllStats()
    {
        var stats = new AdminDashboardStatsDto
        {
            TotalCustomers = 10,
            TotalAgents = 5,
            TotalPolicies = 20,
            TotalClaims = 7,
            TotalPremiumCollected = 50000
        };
        Assert.Equal(10, stats.TotalCustomers);
        Assert.Equal(5, stats.TotalAgents);
        Assert.Equal(20, stats.TotalPolicies);
        Assert.Equal(7, stats.TotalClaims);
        Assert.Equal(50000m, stats.TotalPremiumCollected);
    }
}
