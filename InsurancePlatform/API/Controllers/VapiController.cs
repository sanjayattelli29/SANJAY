using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using API.Models.Vapi;

namespace API.Controllers
{
    /// <summary>
    /// This controller serves as a "Tool" (Webhook) for Vapi.ai.
    /// When the AI assistant needs information about the user's specific policies or claims,
    /// it will call this endpoint automatically.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VapiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public VapiController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Handles the tool call from Vapi.
        /// It analyzes the user's spoken query and returns the relevant data from our database.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> HandleVapiTool([FromBody] System.Text.Json.JsonElement body)
        {
            string? userId = null;
            string? userQuery = null;

            // 1. Attempt to extract from your "Direct" schema (the one in your screenshot)
            if (body.TryGetProperty("userId", out var uNode))
            {
                userId = uNode.GetString();
                userQuery = body.TryGetProperty("query", out var qNode) ? qNode.GetString() : "";
            }
            // 2. Fallback to the default Vapi "Tool Call" wrapped structure
            else if (body.TryGetProperty("message", out var mNode))
            {
                var toolCall = mNode.TryGetProperty("toolCall", out var tcNode) ? tcNode :
                               mNode.TryGetProperty("toolCalls", out var tcsNode) && tcsNode.GetArrayLength() > 0 ? tcsNode[0] : 
                               default;

                if (toolCall.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    var args = toolCall.GetProperty("function").GetProperty("arguments");
                    userId = args.GetProperty("userId").GetString();
                    userQuery = args.GetProperty("query").GetString();
                }
            }

            if (string.IsNullOrEmpty(userId) || userId == "guest")
            {
                return Ok(new { response = "I don't have your user identity. Please make sure you are logged in so I can check your account details." });
            }

            userQuery = (userQuery ?? "").ToLower();
            string responseContent = "";

            try
            {
                // 2. Process based on the intent (Claim, Policy, or Payment)
                if (userQuery.Contains("claim"))
                {
                    var latestClaim = await _db.InsuranceClaims
                        .Where(c => c.UserId == userId)
                        .OrderByDescending(c => c.SubmissionDate)
                        .FirstOrDefaultAsync();

                    if (latestClaim != null)
                    {
                        responseContent = $"I found your latest claim for {latestClaim.IncidentType}. The status is currently '{latestClaim.Status}'. The requested amount was {latestClaim.RequestedAmount:C}.";
                        if (latestClaim.Status == "Approved")
                        {
                            responseContent += $" Good news! It has been approved for {latestClaim.ApprovedAmount:C}.";
                        }
                    }
                    else
                    {
                        responseContent = "I couldn't find any claims filed under your account. Would you like me to help you raise a new one?";
                    }
                }
                else if (userQuery.Contains("policy") || userQuery.Contains("plan") || userQuery.Contains("portfolio"))
                {
                    var activePolicy = await _db.PolicyApplications
                        .Where(p => p.UserId == userId && p.Status == "Active")
                        .OrderByDescending(p => p.StartDate)
                        .FirstOrDefaultAsync();

                    if (activePolicy != null)
                    {
                        responseContent = $"You have an active {activePolicy.PolicyCategory} policy on the {activePolicy.TierId} tier. Your total coverage is {activePolicy.TotalCoverageAmount:C}.";
                    }
                    else
                    {
                        var pendingPolicy = await _db.PolicyApplications
                            .Where(p => p.UserId == userId && p.Status != "Active")
                            .OrderByDescending(p => p.SubmissionDate)
                            .FirstOrDefaultAsync();

                        if (pendingPolicy != null)
                        {
                            responseContent = $"I see you have an application for a {pendingPolicy.PolicyCategory} policy which is currently in '{pendingPolicy.Status}' status.";
                        }
                        else
                        {
                            responseContent = "It looks like you don't have any active or pending policies yet. I can recommend some plans based on your needs!";
                        }
                    }
                }
                else if (userQuery.Contains("payment") || userQuery.Contains("bill") || userQuery.Contains("due"))
                {
                    var policy = await _db.PolicyApplications
                        .Where(p => p.UserId == userId)
                        .OrderByDescending(p => p.SubmissionDate)
                        .FirstOrDefaultAsync();

                    if (policy != null && policy.PaidAmount.HasValue)
                    {
                        responseContent = $"Your last payment of {policy.PaidAmount.Value:C} was processed on {policy.PaymentDate?.ToShortDateString() ?? "recently"}.";
                        if (policy.NextPaymentDate.HasValue)
                        {
                            responseContent += $" Your next payment is scheduled for {policy.NextPaymentDate.Value.ToShortDateString()}.";
                        }
                    }
                    else
                    {
                        responseContent = "I couldn't find any recent payment records for your account. Please check your billing section for more details.";
                    }
                }
                else
                {
                    // Generic fallback if the intent isn't clear
                    responseContent = "I can help you with your claim status, policy details, or payment information. What specifically would you like to know?";
                }

                // 3. Return the response in a format Vapi understands (must have a 'response' or 'result' field)
                return Ok(new { response = responseContent });
            }
            catch (Exception ex)
            {
                return Ok(new { response = "I encountered an error while fetching your details. Please try again in a moment." });
            }
        }
    }
}
