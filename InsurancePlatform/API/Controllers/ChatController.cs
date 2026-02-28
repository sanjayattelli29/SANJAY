using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetChatList()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized();

        var chats = await _chatService.GetUserChatListAsync(userId, role);
        return Ok(chats);
    }

    [HttpGet("{policyId}")]
    public async Task<IActionResult> GetChatHistory(string policyId)
    {
        var chat = await _chatService.GetChatHistoryAsync(policyId);
        if (chat == null) return NotFound();

        return Ok(new
        {
            policyId = chat.PolicyId,
            customerEmail = chat.CustomerEmail,
            agentEmail = chat.AgentEmail,
            messages = chat.Messages.Select(m => new
            {
                senderRole = m.SenderRole,
                message = m.Message,
                timestamp = m.Timestamp
            })
        });
    }

    [HttpPost("init")]
    public async Task<IActionResult> InitializeChat([FromBody] ChatInitRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Customer initializes, or Agent joins. 
        // We expect policyId, customerId, agentId to be provided or resolved.
        var chat = await _chatService.GetOrCreateChatAsync(request.PolicyId, request.CustomerId, request.AgentId);
        return Ok(chat);
    }
}

public class ChatInitRequest
{
    public string PolicyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}
