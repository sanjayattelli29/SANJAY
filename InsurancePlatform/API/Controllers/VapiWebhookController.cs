using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Application.DTOs;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VapiWebhookController : ControllerBase
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly INotificationRepository _notificationRepository;

        public VapiWebhookController(
            IClaimRepository claimRepository,
            IPolicyRepository policyRepository,
            INotificationRepository notificationRepository)
        {
            _claimRepository = claimRepository;
            _policyRepository = policyRepository;
            _notificationRepository = notificationRepository;
        }

        [HttpPost("Process")]
        public async Task<IActionResult> Process([FromBody] JsonElement request)
        {
            // Vapi sends a "message" object
            if (request.TryGetProperty("message", out var message))
            {
                var messageType = message.GetProperty("type").GetString();

                if (messageType == "tool-calls")
                {
                    var toolCalls = message.GetProperty("toolCalls").EnumerateArray();
                    var results = new List<object>();

                    // We need the userId. Vapi can pass it in metadata or variableValues.
                    // Assuming it's in metadata.userId or passed as an argument.
                    string userId = "guest";
                    if (message.TryGetProperty("call", out var call))
                    {
                        if (call.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty("userId", out var metadataUserId))
                        {
                            userId = metadataUserId.GetString();
                        }
                    }

                    foreach (var toolCall in toolCalls)
                    {
                        var toolName = toolCall.GetProperty("function").GetProperty("name").GetString();
                        var toolCallId = toolCall.GetProperty("id").GetString();
                        
                        // Extract arguments if they contain a specific userId override
                        var arguments = toolCall.GetProperty("function").GetProperty("arguments");
                        if (arguments.TryGetProperty("userId", out var argUserId))
                        {
                            userId = argUserId.GetString();
                        }

                        object result = null;

                        switch (toolName)
                        {
                            case "getClaims":
                                var claimsResult = await _claimRepository.GetByUserIdAsync(userId);
                                result = claimsResult.Select(c => new {
                                    c.Id,
                                    c.IncidentType,
                                    c.Status,
                                    Amount = c.RequestedAmount,
                                    Approved = c.ApprovedAmount,
                                    Date = c.IncidentDate.ToShortDateString(),
                                    Summary = c.Description
                                });
                                break;
                            case "getPolicies":
                                var policiesResult = await _policyRepository.GetUserPoliciesAsync(userId);
                                result = policiesResult.Select(p => new {
                                    p.Id,
                                    p.PolicyCategory,
                                    p.Status,
                                    Coverage = p.TotalCoverageAmount,
                                    Remaining = p.RemainingCoverageAmount,
                                    Expiry = p.ExpiryDate?.ToShortDateString() ?? "N/A"
                                });
                                break;
                            case "getNotifications":
                                var notificationsResult = await _notificationRepository.GetByUserIdAsync(userId);
                                result = notificationsResult.Select(n => new {
                                    n.Id,
                                    n.Message,
                                    Date = n.CreatedAt.ToShortDateString(),
                                    Unread = !n.IsRead
                                });
                                break;
                            default:
                                result = new { error = "Tool not found" };
                                break;
                        }

                        results.Add(new
                        {
                            toolCallId = toolCallId,
                            result = result
                        });
                    }

                    return Ok(new { results = results });
                }
            }

            // Return 200 OK for other message types to acknowledge receipt
            return Ok();
        }
    }
}
