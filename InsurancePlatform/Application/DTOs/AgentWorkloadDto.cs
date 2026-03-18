namespace Application.DTOs
{
    public class AgentWorkloadDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentEmail { get; set; } = string.Empty;
        public int AssignedPolicyCount { get; set; }
    }
}
