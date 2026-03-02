using System.Collections.Generic;

namespace Domain.Entities;

// this class holds all the settings for policies
public class PolicyConfiguration
{
    // list of all types of policies
    public List<PolicyCategory> PolicyCategories { get; set; } = new();
    // things that change the price like age or smoking
    public RiskFactors RiskFactors { get; set; } = new();
    // extra charges based on how you pay
    public PaymentModeMultipliers PaymentModeMultipliers { get; set; } = new();
    // how the form should look like
    public ApplicationStructure ApplicationStructure { get; set; } = new();
    // math formula to calculate final price
    public string PremiumFormula { get; set; } = string.Empty;
}

// this class is for categories like individual or family
public class PolicyCategory
{
    // unique id for category
    public string CategoryId { get; set; } = string.Empty;
    // name of category
    public string CategoryName { get; set; } = string.Empty;
    // max people in one policy
    public int MaxMembersAllowed { get; set; }
    // how we calculate the price
    public string? PremiumBasedOn { get; set; }
    // tiers like gold or silver
    public List<PolicyTier> Tiers { get; set; } = new();
}

// this class is for policy tiers like gold silver etc
public class PolicyTier
{
    // unique id for tier
    public string TierId { get; set; } = string.Empty;
    // names like silver or gold
    public string TierName { get; set; } = string.Empty;
    // starting coverage amount
    public decimal BaseCoverageAmount { get; set; }
    // starting price
    public decimal BasePremiumAmount { get; set; }
    // how long it lasts in years
    public int ValidityInYears { get; set; }
    // list of things covered
    public List<string> Benefits { get; set; } = new();
}

// factors that make insurance cost more or less
public class RiskFactors
{
    // multipliers based on age
    public List<AgeMultiplier> AgeMultipliers { get; set; } = new();
    // multipliers based on job
    public List<ProfessionMultiplier> ProfessionMultipliers { get; set; } = new();
    // extra cost if you drink alcohol
    public AlcoholMultiplier AlcoholMultiplier { get; set; } = new();
    // extra cost if you smoke
    public SmokingMultiplier SmokingMultiplier { get; set; } = new();
    // cost based on how much you travel
    public List<TravelFrequencyMultiplier> TravelFrequencyMultiplier { get; set; } = new();
}

// multiplier for a specific age range
public class AgeMultiplier
{
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public double Multiplier { get; set; }
}

// multiplier for a specific job
public class ProfessionMultiplier
{
    public string Profession { get; set; } = string.Empty;
    public double Multiplier { get; set; }
}

// multipliers for drinking habits
public class AlcoholMultiplier
{
    public double NonDrinker { get; set; }
    public double Occasional { get; set; }
    public double Regular { get; set; }
    public double Others { get; set; }
}

// multipliers for smoking habits
public class SmokingMultiplier
{
    public double NonSmoker { get; set; }
    public double Occasional { get; set; }
    public double Regular { get; set; }
    public double Others { get; set; }
}

// multiplier based on km traveled
public class TravelFrequencyMultiplier
{
    public int MaxKmPerMonth { get; set; }
    public double Multiplier { get; set; }
    public string? Label { get; set; }
}

// extra costs based on payment timing
public class PaymentModeMultipliers
{
    public double Monthly { get; set; }
    public double HalfYearly { get; set; }
    public double Yearly { get; set; }
}

// how the application form is built
public class ApplicationStructure
{
    public IndividualApplicationStructure Individual { get; set; } = new();
    public FamilyApplicationStructure Family { get; set; } = new();
}

// fields for individual form
public class IndividualApplicationStructure
{
    public List<string> RequiredFields { get; set; } = new();
    public bool NomineeRequired { get; set; }
}

// fields for family form
public class FamilyApplicationStructure
{
    public List<string> PrimaryApplicantFields { get; set; } = new();
    public List<string> FamilyMembersFields { get; set; } = new();
    public int MaxMembersAllowed { get; set; }
    public bool NomineeRequired { get; set; }
    public string PremiumCalculatedUsing { get; set; } = string.Empty;
}
