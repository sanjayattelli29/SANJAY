using InsurancePlatform.Models;
using InsurancePlatform.Services.Interfaces;

namespace InsurancePlatform.Services
{
    public class RiskService : IRiskService
    {
        public decimal CalculateRiskScore(CustomerProfile profile)
        {
            decimal score = 0;

            // Age factor (Simulated)
            var dob = profile.DateOfBirth ?? DateTime.UtcNow.AddYears(-30); // Default to 30 if null
            var age = DateTime.UtcNow.Year - dob.Year;
            if (age < 25 || age > 60) score += 20;
            else score += 10;

            // Occupation factor
            score += (profile.OccupationType ?? OccupationType.Salaried) switch
            {
                OccupationType.HighRiskWorker => 40,
                OccupationType.FieldWorker => 25,
                _ => 10
            };

            // Habits
            if (profile.SmokingHabit.HasValue && profile.SmokingHabit != SmokingHabit.None) score += 15;
            if (profile.AlcoholConsumptionFrequency == AlcoholConsumptionFrequency.Regular) score += 15;

            // Traffic violations
            score += (profile.TrafficViolationCount ?? 0) * 5;

            return score;
        }

        public decimal CalculatePremium(decimal sumInsured, decimal riskScore, PolicyType type)
        {
            decimal baseRate = 0.02m; // 2% base

            if (riskScore > 50) baseRate += 0.015m;
            else if (riskScore > 30) baseRate += 0.005m;

            if (type == PolicyType.AccidentalInsurance) baseRate += 0.005m;

            return sumInsured * baseRate;
        }

        public RiskCategory GetRiskCategory(decimal riskScore)
        {
            if (riskScore > 70) return RiskCategory.High;
            if (riskScore > 40) return RiskCategory.Standard;
            return RiskCategory.Low;
        }

        public decimal CalculateClaimFraudScore(Claim claim)
        {
            decimal score = 0;
            // Rule-based placeholder
            if (claim.RequestedAmount > 50000) score += 20;
            if (claim.IncidentDate > DateTime.UtcNow.AddYears(-1)) score += 5; // Simplified logic
            
            return score;
        }
    }
}
