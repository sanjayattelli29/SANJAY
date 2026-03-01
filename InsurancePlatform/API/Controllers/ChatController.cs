using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // web toolkit
using System.Security.Claims; // identity toolkit

namespace API.Controllers;

// this handles the live chat between customer and agent
[Authorize] // must be logged in
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService; // set chat logic
    }

    // get all active chats for the logged in person
    [HttpGet("list")] // get request for chats
    public async Task<IActionResult> GetChatList() // my conversations
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized();

        var chats = await _chatService.GetUserChatListAsync(userId, role);
        return Ok(chats);
    }

    // see old messages for a specific policy
    [HttpGet("{policyId}")] // get specific chat
    public async Task<IActionResult> GetChatHistory(string policyId) // policy based history
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
    [HttpPost("init")] // start new connection
    public async Task<IActionResult> InitializeChat([FromBody] ChatInitRequest request) // setup chat link
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var chat = await _chatService.GetOrCreateChatAsync(request.PolicyId, request.CustomerId, request.AgentId);
        return Ok(chat);
    }

    // tell the system i have seen the messages
    [HttpPost("{policyId}/read")] // mark seen
    public async Task<IActionResult> MarkRead(string policyId) // read receipt
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(role)) return Unauthorized();

        await _chatService.MarkMessagesAsReadAsync(policyId, role);
        return Ok();
    }
}
// chat endpoint logic ends

public class ChatInitRequest
{
    public string PolicyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}
