using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Domain.Enums;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IPolicyService _policyService;
        private readonly IClaimService _claimService;

        public NotificationsController(INotificationService notificationService, IPolicyService policyService, IClaimService claimService)
        {
            _notificationService = notificationService;
            _policyService = policyService;
            _claimService = claimService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 1. Get stored notifications for THIS user only
            var notifications = (await _notificationService.GetUserNotificationsAsync(userId)).ToList();
            
            // 2. Add role-specific virtual notifications (Sync State)
            
            // Customer Sync: Awaiting Payment
            if (User.IsInRole(UserRoles.Customer))
            {
                var userPolicies = await _policyService.GetUserPoliciesAsync(userId);
                foreach (var policy in userPolicies.Where(p => p.Status == "AwaitingPayment"))
                {
                    AddVirtualNotification(notifications, userId, $"Policy:{policy.Id}", "Payment Required 💳", 
                        $"Please complete your payment of {policy.CalculatedPremium:C} for {policy.TierId}. ID: {policy.Id.Substring(0, 3).ToUpper()}...", 
                        policy.ApprovedAt ?? policy.SubmissionDate);
                }
            }

            // Admin Sync: Unassigned Items
            if (User.IsInRole(UserRoles.Admin))
            {
                var allApps = await _policyService.GetAllApplicationsAsync();
                foreach (var app in allApps.Where(a => a.Status == "Pending"))
                {
                    AddVirtualNotification(notifications, userId, $"Policy:{app.Id}", "New Application 📝", 
                        $"Policy application from {app.User?.Email ?? app.UserId} requires agent assignment.", app.SubmissionDate);
                }

                var allClaims = await _claimService.GetAllClaimsAsync();
                foreach (var claim in allClaims.Where(c => c.Status == "Pending"))
                {
                    AddVirtualNotification(notifications, userId, $"Claim:{claim.Id}", "New Claim 🏥", 
                        $"Claim for {claim.IncidentType} from {claim.User?.Email ?? claim.UserId} requires officer assignment.", claim.SubmissionDate);
                }
            }

            // Agent Sync: Assigned Policies
            if (User.IsInRole(UserRoles.Agent))
            {
                var agentApps = await _policyService.GetAgentApplicationsAsync(userId);
                foreach (var app in agentApps.Where(a => a.Status == "Assigned"))
                {
                    AddVirtualNotification(notifications, userId, $"Policy:{app.Id}", "New Assignment 📁", 
                        $"You have been assigned a new application for {app.TierId} ({app.User?.Email}).", app.SubmissionDate);
                }
            }

            // Claims Officer Sync: Assigned & Pending Claims
            if (User.IsInRole(UserRoles.ClaimOfficer))
            {
                var officerClaims = await _claimService.GetOfficerClaimsAsync(userId);
                foreach (var claim in officerClaims.Where(c => c.Status == "Assigned"))
                {
                    AddVirtualNotification(notifications, userId, $"Claim:{claim.Id}", "New Claim Assignment 🔍", 
                        $"New claim for {claim.IncidentType} from {claim.User?.Email} assigned for review.", claim.SubmissionDate);
                }

                var pendingClaims = await _claimService.GetPendingClaimsAsync();
                foreach (var claim in pendingClaims)
                {
                    AddVirtualNotification(notifications, userId, $"Claim:{claim.Id}", "Pending Claim 🏥", 
                        $"A new claim for {claim.IncidentType} from {claim.User?.Email} is awaiting assignment.", claim.SubmissionDate);
                }
            }

            return Ok(notifications.OrderByDescending(n => n.CreatedAt).Take(50));
        }

        private void AddVirtualNotification(List<Notification> currentList, string userId, string targetType, string title, string message, DateTime date)
        {
            if (!currentList.Any(n => n.NotificationType == targetType))
            {
                string idStr = targetType.Split(':')[1];
                currentList.Add(new Notification
                {
                    Id = Guid.TryParse(idStr, out var g) ? g : Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    CreatedAt = date,
                    IsRead = false,
                    NotificationType = targetType
                });
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }
    }
}