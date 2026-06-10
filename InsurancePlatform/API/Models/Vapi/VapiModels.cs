using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace API.Models.Vapi
{
    /// <summary>
    /// DTO for the incoming request from Vapi.ai Tool call.
    /// Vapi sends this when it needs to fetch external data to answer a user.
    /// </summary>
    public class VapiToolRequest
    {
        public Message? Message { get; set; }
    }

    /// <summary>
    /// This matches the exact JSON structure in your Vapi dashboard screenshot
    /// (Fields are directly in the root of the JSON body)
    /// </summary>
    public class DirectVapiToolRequest
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("toolCalls")]
        public List<ToolCall>? ToolCalls { get; set; }

        [JsonPropertyName("toolCall")]
        public ToolCall? ToolCall { get; set; }
    }

    public class ToolCall
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public ToolCallFunction? Function { get; set; }
    }

    public class ToolCallFunction
    {
        public string? Name { get; set; }
        public ToolCallArguments? Arguments { get; set; }
    }

    /// <summary>
    /// The actual arguments sent by Vapi based on your Tool definition.
    /// Make sure "query" and "userId" match your Vapi tool parameter names.
    /// </summary>
    public class ToolCallArguments
    {
        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
    }
}
