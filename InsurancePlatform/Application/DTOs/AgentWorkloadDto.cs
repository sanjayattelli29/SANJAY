namespace Application.DTOs
{
    /// <summary>
    /// This class shows how "busy" an agent is.
    /// It helps the admin decide if an agent can handle more work.
    /// </summary>
    public class AgentWorkloadDto
    {
        // The unique ID of the agent
        public string AgentId { get; set; } = string.Empty;
        
        // The email address of the agent
        public string AgentEmail { get; set; } = string.Empty;
        
        // How many insurance policy applications are currently assigned to this agent
        public int AssignedPolicyCount { get; set; }
    }
}
