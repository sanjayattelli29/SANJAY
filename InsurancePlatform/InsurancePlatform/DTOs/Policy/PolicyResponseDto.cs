using InsurancePlatform.Models;

namespace InsurancePlatform.DTOs.Policy
{
    public class PolicyResponseDto
    {
        public Guid PolicyId { get; set; }
        public string PolicyNumber { get; set; }
        public string CustomerName { get; set; }
        public string AgentName { get; set; }
        public decimal PremiumAmount { get; set; }
        public decimal SumInsured { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
