using System.Collections.Generic;

namespace Application.DTOs
{
    public class VapiToolResponse
    {
        public List<VapiToolResult> Results { get; set; } = new List<VapiToolResult>();
    }

    public class VapiToolResult
    {
        public string ToolCallId { get; set; }
        public object Result { get; set; }
    }
}
