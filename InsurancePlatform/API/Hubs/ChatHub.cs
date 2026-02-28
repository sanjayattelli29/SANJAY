using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task JoinRoom(string policyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, policyId);
    }

    public async Task SendMessage(string policyId, string senderId, string senderRole, string message)
    {
        // Save to DB
        var savedMessage = await _chatService.SaveMessageAsync(policyId, senderId, senderRole, message);

        // Broadcast to group
        await Clients.Group(policyId).SendAsync("ReceiveMessage", new
        {
            senderRole = savedMessage.SenderRole,
            message = savedMessage.Message,
            timestamp = savedMessage.Timestamp
        });
    }
}
