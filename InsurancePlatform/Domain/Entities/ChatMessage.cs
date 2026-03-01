using System; // system namespace

namespace Domain.Entities; // entity namespace

// this class stores a single message in a chat
public class ChatMessage
{
    // unique id for this message
    public string Id { get; set; } = Guid.NewGuid().ToString(); // id generation
    // which chat this message belongs to
    public string ChatId { get; set; } = string.Empty;
    // who sent the message
    public string SenderId { get; set; } = string.Empty;
    // role of sender like customer or agent
    public string SenderRole { get; set; } = string.Empty; // customer/agent
    // the actual text message
    public string Message { get; set; } = string.Empty;
    // when the message was sent
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    // if the other person has seen this message
    public bool IsRead { get; set; } = false; // default is not read

    // link to the full chat object
    public Chat? Chat { get; set; } // navigation property
}
// message record ends here
