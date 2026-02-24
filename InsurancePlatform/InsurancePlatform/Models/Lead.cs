using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class Lead : BaseEntity
    {
        public Guid CustomerId { get; set; }

        public Guid AssignedAgentId { get; set; }

        public decimal LeadScore { get; set; }

        public LeadStatus LeadStatus { get; set; }

        public LeadSource Source { get; set; }

        // Navigation
        public CustomerProfile Customer { get; set; }
        public AgentProfile AssignedAgent { get; set; }
    }

    public enum LeadStatus
    {
        New,
        Contacted,
        Interested,
        Negotiation,
        Converted,
        Lost
    }

    public enum LeadSource
    {
        AIChatbot,
        DirectWebsite
    }
}