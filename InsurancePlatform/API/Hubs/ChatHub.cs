using Application.Interfaces; // for chat service
using Microsoft.AspNetCore.SignalR; // for real-time hub

namespace API.Hubs;

// this code handles the real-time chat connection
public class ChatHub : Hub // signalr hub class
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService; // set service
    }

    // joins a specific chat group based on policy id
    public async Task JoinRoom(string policyId) // room id is policy id
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, policyId); // join group
    }

    // sends a message to everyone in the chat group
    public async Task SendMessage(string policyId, string senderId, string senderRole, string message) // send out text
    {
        // save the message to our database first
        var savedMessage = await _chatService.SaveMessageAsync(policyId, senderId, senderRole, message);

        // send it out to everyone currently online in this chat
        await Clients.Group(policyId).SendAsync("ReceiveMessage", new
        {
            senderRole = savedMessage.SenderRole,
            message = savedMessage.Message,
            timestamp = savedMessage.Timestamp // time sent
        });
    }
}
