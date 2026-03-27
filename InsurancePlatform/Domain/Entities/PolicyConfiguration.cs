using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

// this class holds all the settings for policies
public class PolicyConfiguration
{
    // list of all types of policies
    public List<PolicyCategory> PolicyCategories { get; set; } = new();
    
    // professions that must provide income proof
    public List<string> IncomeProofRequiredFor { get; set; } = new();

    // things that change the price like age, smoking, and income
    public RiskFactors RiskFactors { get; set; } = new();
    
    
    // max coverage rules
    public CoverageRules CoverageRules { get; set; } = new();

    // math formula to calculate final price
    public string PremiumFormula { get; set; } = string.Empty;
}

// this class is for categories like individual or family
public class PolicyCategory
{
    // unique id for category
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string CategoryId { get; set; } = string.Empty;
    // name of category
    public string CategoryName { get; set; } = string.Empty;
    // max people in one policy
    public int MaxMembersAllowed { get; set; }
    // how we calculate the price
    public string? PremiumBasedOn { get; set; }
    
    // form requirements for this specific category
    [NotMapped]
    public ApplicationRequirements ApplicationRequirements { get; set; } = new();

    // tiers like gold or silver
    public List<PolicyTier> Tiers { get; set; } = new();
}

public class ApplicationRequirements
{
    public List<RequiredDocumentInfo> DocumentsRequired { get; set; } = new();
    public List<string> ApplicantQuestions { get; set; } = new();
    public List<string> NomineeInformation { get; set; } = new();
    public List<string> FamilyMemberQuestions { get; set; } = new();
}

public class RequiredDocumentInfo
{
    public string DocType { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? RequiredCondition { get; set; }
    public string? Description { get; set; }
    public List<string>? Accepted { get; set; }
}

// this class is for policy tiers like gold silver etc
public class PolicyTier
{
    // unique id for tier
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string TierId { get; set; } = string.Empty;

    public string TierName { get; set; } = string.Empty;

    [ForeignKey("Category")]
    public string CategoryId { get; set; } = string.Empty;
    public PolicyCategory? Category { get; set; }
    public decimal BaseCoverageAmount { get; set; }
    public decimal BasePremiumAmount { get; set; }
    public int ValidityInYears { get; set; }
    public List<string> Benefits { get; set; } = new();
}

// factors that make insurance cost more or less
public class RiskFactors
{
    public List<AgeMultiplier> AgeMultipliers { get; set; } = new();
    public List<ProfessionMultiplier> ProfessionMultipliers { get; set; } = new();
    public List<IncomeMultiplier> IncomeMultiplier { get; set; } = new();
    public AlcoholMultiplier AlcoholMultiplier { get; set; } = new();
    public SmokingMultiplier SmokingMultiplier { get; set; } = new();
    public List<TravelFrequencyMultiplier> TravelFrequencyMultiplier { get; set; } = new();
    public List<VehicleTypeMultiplier> VehicleTypeMultiplier { get; set; } = new();
}

public class IncomeMultiplier
{
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public double Multiplier { get; set; }
}

public class CoverageRules
{
    public MaxCoverageBasedOnIncome MaxCoverageBasedOnIncome { get; set; } = new();
}

public class MaxCoverageBasedOnIncome
{
    public int Multiplier { get; set; }
    public string Description { get; set; } = string.Empty;
}

// (multiplier helper classes stay same)
public class AgeMultiplier { public int MinAge { get; set; } public int MaxAge { get; set; } public double Multiplier { get; set; } }
public class ProfessionMultiplier { public string Profession { get; set; } = string.Empty; public double Multiplier { get; set; } }
public class AlcoholMultiplier { public double NonDrinker { get; set; } public double Occasional { get; set; } public double Regular { get; set; } }
public class SmokingMultiplier { public double NonSmoker { get; set; } public double Occasional { get; set; } public double Regular { get; set; } }
public class TravelFrequencyMultiplier { public int MaxKmPerMonth { get; set; } public double Multiplier { get; set; } public string? Label { get; set; } }
public class VehicleTypeMultiplier { public string VehicleType { get; set; } = string.Empty; public double Multiplier { get; set; } }
