using System.Collections.Generic;

namespace Domain.Entities;

public class PolicyConfiguration
{
    public List<PolicyCategory> PolicyCategories { get; set; } = new();
    public RiskFactors RiskFactors { get; set; } = new();
    public PaymentModeMultipliers PaymentModeMultipliers { get; set; } = new();
    public ApplicationStructure ApplicationStructure { get; set; } = new();
    public string PremiumFormula { get; set; } = string.Empty;
}

public class PolicyCategory
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int MaxMembersAllowed { get; set; }
    public string? PremiumBasedOn { get; set; }
    public List<PolicyTier> Tiers { get; set; } = new();
}

public class PolicyTier
{
    public string TierId { get; set; } = string.Empty;
    public string TierName { get; set; } = string.Empty;
    public decimal BaseCoverageAmount { get; set; }
    public decimal BasePremiumAmount { get; set; }
    public int ValidityInYears { get; set; }
    public List<string> Benefits { get; set; } = new();
}

public class RiskFactors
{
    public List<AgeMultiplier> AgeMultipliers { get; set; } = new();
    public List<ProfessionMultiplier> ProfessionMultipliers { get; set; } = new();
    public AlcoholMultiplier AlcoholMultiplier { get; set; } = new();
    public SmokingMultiplier SmokingMultiplier { get; set; } = new();
    public List<TravelFrequencyMultiplier> TravelFrequencyMultiplier { get; set; } = new();
}

public class AgeMultiplier
{
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public double Multiplier { get; set; }
}

public class ProfessionMultiplier
{
    public string Profession { get; set; } = string.Empty;
    public double Multiplier { get; set; }
}

public class AlcoholMultiplier
{
    public double NonDrinker { get; set; }
    public double Occasional { get; set; }
    public double Regular { get; set; }
    public double Others { get; set; }
}

public class SmokingMultiplier
{
    public double NonSmoker { get; set; }
    public double Occasional { get; set; }
    public double Regular { get; set; }
    public double Others { get; set; }
}

public class TravelFrequencyMultiplier
{
    public int MaxKmPerMonth { get; set; }
    public double Multiplier { get; set; }
    public string? Label { get; set; }
}

public class PaymentModeMultipliers
{
    public double Monthly { get; set; }
    public double HalfYearly { get; set; }
    public double Yearly { get; set; }
}

public class ApplicationStructure
{
    public IndividualApplicationStructure Individual { get; set; } = new();
    public FamilyApplicationStructure Family { get; set; } = new();
}

public class IndividualApplicationStructure
{
    public List<string> RequiredFields { get; set; } = new();
    public bool NomineeRequired { get; set; }
}

public class FamilyApplicationStructure
{
    public List<string> PrimaryApplicantFields { get; set; } = new();
    public List<string> FamilyMembersFields { get; set; } = new();
    public int MaxMembersAllowed { get; set; }
    public bool NomineeRequired { get; set; }
    public string PremiumCalculatedUsing { get; set; } = string.Empty;
}
