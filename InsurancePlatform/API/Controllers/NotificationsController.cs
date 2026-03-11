using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Domain.Enums;

namespace API.Controllers
{
    // this handles bell icon notifications for users
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IPolicyService _policyService;
        private readonly IClaimService _claimService;
        private readonly UserManager<ApplicationUser> _userManager;

        // constructor to get services we need
        public NotificationsController(
            INotificationService notificationService, 
            IPolicyService policyService, 
            IClaimService claimService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _policyService = policyService;
            _claimService = claimService;
            _userManager = userManager;
        }

        // gets all notification alerts for the logged in user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] string? role = null)
        {
            // find out who is asking for notifications - use the most robust method
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // fallback to NameIdentifier claim if helper fails
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // also check if we can get the user object to verify identity
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            // step 1: get real notifications saved in database for this user
            var allNotifications = (await _notificationService.GetUserNotificationsAsync(userId)).ToList();

            // FALLBACK: if nothing found by ID, try finding by Name (Email) if available in claims
            if (allNotifications.Count == 0)
            {
                var userName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userName) && userName != userId)
                {
                    var fallbackNotifications = await _notificationService.GetUserNotificationsAsync(userName);
                    allNotifications.AddRange(fallbackNotifications);
                }
            }

            // Filter by role if provided
            List<Notification> notifications;
            if (!string.IsNullOrEmpty(role))
            {
                string prefix = role.ToUpper() switch {
                    "ADMIN" => "ADM:",
                    "AGENT" => "AGENT:",
                    "CUSTOMER" => "CUST:",
                    "CLAIMOFFICER" => "OFF:",
                    _ => ""
                };

                notifications = allNotifications
                    .Where(n => string.IsNullOrEmpty(prefix) || n.NotificationType.StartsWith(prefix) || n.NotificationType == "General")
                    .ToList();
            }
            else
            {
                notifications = allNotifications;
            }
            
            // step 2: add temporary notifications based on what's pending for their role
            var roles = await _userManager.GetRolesAsync(user);
            
            // Filter virtual notifications by the requested role OR user roles if no role requested
            bool checkCustomer = string.IsNullOrEmpty(role) ? roles.Contains(UserRoles.Customer) : role.Equals(UserRoles.Customer, StringComparison.OrdinalIgnoreCase);
            bool checkAdmin = string.IsNullOrEmpty(role) ? roles.Contains(UserRoles.Admin) : role.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
            bool checkAgent = string.IsNullOrEmpty(role) ? roles.Contains(UserRoles.Agent) : role.Equals(UserRoles.Agent, StringComparison.OrdinalIgnoreCase);
            bool checkOfficer = string.IsNullOrEmpty(role) ? roles.Contains(UserRoles.ClaimOfficer) : role.Equals(UserRoles.ClaimOfficer, StringComparison.OrdinalIgnoreCase);

            // if customer is logged in: show pending payments
            if (checkCustomer)
            {
                // get all policies customer has bought
                var userPolicies = await _policyService.GetUserPoliciesAsync(userId);
                // for each policy that needs payment still
                foreach (var policy in userPolicies.Where(p => p.Status == "AwaitingPayment"))
                {
                    // add a reminder notification
                    AddVirtualNotification(notifications, userId, $"CUST:Policy:{policy.Id}", "Payment Required 💳", 
                        $"Please complete your payment of {policy.CalculatedPremium:C} for {policy.TierId}. ID: {policy.Id.Substring(0, 3).ToUpper()}...", 
                        policy.ApprovedAt ?? policy.SubmissionDate);
                }
            }

            // if admin is logged in: show things that need assignment
            if (checkAdmin)
            {
                // get all policy applications that nobody is working on yet
                var allApps = await _policyService.GetAllApplicationsAsync();
                foreach (var app in allApps.Where(a => a.Status == "Pending"))
                {
                    // alert admin to assign an agent to this
                    AddVirtualNotification(notifications, userId, $"ADM:Policy:{app.Id}", "New Application 📝", 
                        $"Policy application from {app.User?.Email ?? app.UserId} requires agent assignment.", app.SubmissionDate);
                }

                // get all claims that also need someone assigned
                var allClaims = await _claimService.GetAllClaimsAsync();
                foreach (var claim in allClaims.Where(c => c.Status == "Pending"))
                {
                    // alert admin to assign an officer to check this claim
                    AddVirtualNotification(notifications, userId, $"ADM:Claim:{claim.Id}", "New Claim 🏥", 
                        $"Claim for {claim.IncidentType} from {claim.User?.Email ?? claim.UserId} requires officer assignment.", claim.SubmissionDate);
                }
            }

            // Agent Sync: Assigned & Pending Payments
            if (checkAgent)
            {
                var agentApps = await _policyService.GetAgentApplicationsAsync(userId);
                foreach (var app in agentApps.Where(a => a.Status == "Assigned" || a.Status == "AwaitingPayment"))
                {
                    string title = app.Status == "Assigned" ? "New Assignment 📁" : "Pending Order 💳";
                    string message = app.Status == "Assigned" ? 
                        $"You have been assigned a new application for {app.TierId} ({app.User?.Email})." :
                        $"Customer {app.User?.Email} is yet to pay for their {app.TierId} policy.";

                    AddVirtualNotification(notifications, userId, $"AGENT:Policy:{app.Id}", title, message, app.SubmissionDate);
                }
            }

            // Claims Officer Sync: Assigned & Pending Claims
            if (checkOfficer)
            {
                var officerClaims = await _claimService.GetOfficerClaimsAsync(userId);
                foreach (var claim in officerClaims.Where(c => c.Status == "Assigned"))
                {
                    AddVirtualNotification(notifications, userId, $"OFF:Claim:{claim.Id}", "New Claim Assignment 🔍", 
                        $"New claim for {claim.IncidentType} from {claim.User?.Email} assigned for review.", claim.SubmissionDate);
                }
            }

            return Ok(notifications.OrderByDescending(n => n.CreatedAt).Take(50));
        }

        // helper function to add a notification if it doesn't already exist
        private void AddVirtualNotification(List<Notification> currentList, string userId, string targetType, string title, string message, DateTime date)
        {
            // check if this notification (by type/id) is already in the list to avoid duplicates
            if (!currentList.Any(n => n.NotificationType == targetType))
            {
                // extract the id if possible
                string idStr = targetType.Contains(':') ? targetType.Split(':')[1] : string.Empty;
                
                // create and add the notification object
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

        // shows how many notifications user hasn't seen yet
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // count notifications where IsRead is false
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        // marks one notification as read when user clicks on it
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            // update the database to set IsRead = true
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        // marks all notifications as read at once
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // update all user's notifications to IsRead = true
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }
    }
}