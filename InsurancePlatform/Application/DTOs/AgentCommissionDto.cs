using System.Collections.Generic;
using Domain.Entities;

namespace Application.DTOs
{
    /// <summary>
    /// This class holds information about an agent's earnings.
    /// It shows the total money they've made and a list of the policies that helped them earn it.
    /// </summary>
    public class AgentCommissionDto
    {
        // The total amount of commission money earned by the agent
        public decimal TotalCommission { get; set; }
        
        // A list of all active policy applications associated with this agent
        public List<PolicyApplication> ActivePolicies { get; set; } = new();
    }
}
