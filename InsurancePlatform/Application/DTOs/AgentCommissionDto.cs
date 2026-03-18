using System.Collections.Generic;
using Domain.Entities;

namespace Application.DTOs
{
    public class AgentCommissionDto
    {
        public decimal TotalCommission { get; set; }
        public List<PolicyApplication> ActivePolicies { get; set; } = new();
    }
}
