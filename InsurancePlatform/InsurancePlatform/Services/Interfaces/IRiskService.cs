using InsurancePlatform.Models;

namespace InsurancePlatform.Services.Interfaces
{
    public interface IRiskService
    {
        decimal CalculateRiskScore(CustomerProfile profile);
        decimal CalculatePremium(decimal sumInsured, decimal riskScore, PolicyType type);
        RiskCategory GetRiskCategory(decimal riskScore);
        decimal CalculateClaimFraudScore(Claim claim);
    }
}
