using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

// this handles the live chat between customer and agent
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

    // get all active chats for the logged in person
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

    // see old messages for a specific policy
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

    // start a chat if it is new
    [HttpPost("init")]
    public async Task<IActionResult> InitializeChat([FromBody] ChatInitRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var chat = await _chatService.GetOrCreateChatAsync(request.PolicyId, request.CustomerId, request.AgentId);
        return Ok(chat);
    }

    // tell the system i have seen the messages
    [HttpPost("{policyId}/read")]
    public async Task<IActionResult> MarkRead(string policyId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(role)) return Unauthorized();

        await _chatService.MarkMessagesAsReadAsync(policyId, role);
        return Ok();
    }
}

public class ChatInitRequest
{
    public string PolicyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}
