using System;
using System.Collections.Generic;
using Domain.Entities;
using Xunit;

namespace Domain.Tests;

public class DomainEntityTests
{
    // ─── PolicyApplication Tests ───

    [Fact]
    public void PolicyApplication_DefaultStatus_IsPending()
    {
        var app = new PolicyApplication();
        Assert.Equal("Pending", app.Status);
    }

    [Fact]
    public void PolicyApplication_DefaultId_IsGuid()
    {
        var app = new PolicyApplication();
        Assert.True(Guid.TryParse(app.Id, out _));
    }

    [Fact]
    public void PolicyApplication_SubmissionDate_IsSet()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var app = new PolicyApplication();
        Assert.True(app.SubmissionDate >= before);
    }

    [Fact]
    public void PolicyApplication_PaidAmount_DefaultsToNull()
    {
        var app = new PolicyApplication();
        Assert.Null(app.PaidAmount);
    }

    [Fact]
    public void PolicyApplication_CanSetStatus()
    {
        var app = new PolicyApplication { Status = "Active" };
        Assert.Equal("Active", app.Status);
    }

    // ─── InsuranceClaim Tests ───

    [Fact]
    public void InsuranceClaim_DefaultStatus_IsPending()
    {
        var claim = new InsuranceClaim();
        Assert.Equal("Pending", claim.Status);
    }

    [Fact]
    public void InsuranceClaim_DefaultId_IsGuid()
    {
        var claim = new InsuranceClaim();
        Assert.True(Guid.TryParse(claim.Id, out _));
    }

    [Fact]
    public void InsuranceClaim_Documents_DefaultsToEmptyList()
    {
        var claim = new InsuranceClaim();
        Assert.NotNull(claim.Documents);
        Assert.Empty(claim.Documents);
    }

    [Fact]
    public void InsuranceClaim_SubmissionDate_IsSet()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var claim = new InsuranceClaim();
        Assert.True(claim.SubmissionDate >= before);
    }

    [Fact]
    public void InsuranceClaim_CanSetApprovedAmount()
    {
        var claim = new InsuranceClaim { ApprovedAmount = 5000 };
        Assert.Equal(5000m, claim.ApprovedAmount);
    }

    // ─── Notification Tests ───

    [Fact]
    public void Notification_DefaultIsRead_IsFalse()
    {
        var n = new Notification();
        Assert.False(n.IsRead);
    }

    [Fact]
    public void Notification_CanMarkAsRead()
    {
        var n = new Notification { IsRead = true };
        Assert.True(n.IsRead);
    }

    [Fact]
    public void Notification_DefaultId_IsGuid()
    {
        var n = new Notification();
        Assert.NotEqual(Guid.Empty, n.Id);
    }

    // ─── ApplicationUser Tests ───

    [Fact]
    public void ApplicationUser_CanSetFullName()
    {
        var user = new ApplicationUser { FullName = "John Doe" };
        Assert.Equal("John Doe", user.FullName);
    }

    [Fact]
    public void ApplicationUser_CreatedAt_IsSet()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = new ApplicationUser();
        Assert.True(user.CreatedAt >= before);
    }

    // ─── Chat Tests ───

    [Fact]
    public void Chat_Messages_DefaultsToEmptyList()
    {
        var chat = new Chat();
        Assert.NotNull(chat.Messages);
    }

    [Fact]
    public void Chat_CanAddMessage()
    {
        var chat = new Chat();
        chat.Messages = new List<ChatMessage> { new ChatMessage { Message = "Hello" } };
        Assert.Single(chat.Messages);
    }

    // ─── ClaimDocument Tests ───

    [Fact]
    public void ClaimDocument_DefaultId_IsGuid()
    {
        var doc = new ClaimDocument();
        Assert.True(Guid.TryParse(doc.Id.ToString(), out _));
    }

    [Fact]
    public void ClaimDocument_CanSetFileUrl()
    {
        var doc = new ClaimDocument { FileUrl = "https://test.com/file.pdf" };
        Assert.Equal("https://test.com/file.pdf", doc.FileUrl);
    }

    // ─── Two Entities Can Have Unique IDs ───

    [Fact]
    public void PolicyApplication_TwoInstances_HaveDifferentIds()
    {
        var a = new PolicyApplication();
        var b = new PolicyApplication();
        Assert.NotEqual(a.Id, b.Id);
    }
}
