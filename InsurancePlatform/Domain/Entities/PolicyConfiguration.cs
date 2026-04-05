using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

// this class holds all the settings for policies

/// <summary>
/// This class holds the "Master Rules" for all policies.
/// It defines what plans exist, how much they cost, and what info is required.
/// </summary>
public class PolicyConfiguration
{
    // A list of every type of insurance we sell (e.g., "Health", "Life").
    public List<PolicyCategory> PolicyCategories { get; set; } = new();
    
    // A list of jobs (like "Police" or "Miner") that MUST show a salary certificate.
    public List<string> IncomeProofRequiredFor { get; set; } = new();

    // The data that changes the price, like "Smokers pay 50% more".
    public RiskFactors RiskFactors { get; set; } = new();
    
    // Rules about the maximum money a person can be insured for.
    public CoverageRules CoverageRules { get; set; } = new();

    // The mathematical code (formula) used to figure out the final premium price.
    public string PremiumFormula { get; set; } = string.Empty;
}

/// <summary>
/// This represents a group of policies, like "Health Insurance" or "Accident Insurance".
/// </summary>
public class PolicyCategory
{
    // A code identifying the category (e.g., "health-ins").
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string CategoryId { get; set; } = string.Empty;

    // The friendly name, like "Health Protection Plan".
    public string CategoryName { get; set; } = string.Empty;

    // How many family members can be covered under one plan (e.g., 4).
    public int MaxMembersAllowed { get; set; }

    // Explains if the price is based on "Age", "Income", or something else.
    public string? PremiumBasedOn { get; set; }
    
    // A list of what documents (Aadhaar, Photo) the user needs to upload.
    [NotMapped]
    public ApplicationRequirements ApplicationRequirements { get; set; } = new();

    // The specific plans under this category, like "Platinum", "Gold", or "Silver".
    public List<PolicyTier> Tiers { get; set; } = new();
}

/// <summary>
/// Details about what the user needs to provide during the application.
/// </summary>
public class ApplicationRequirements
{
    // Which files (proof) must be uploaded.
    public List<RequiredDocumentInfo> DocumentsRequired { get; set; } = new();
    // Specific questions the user must answer about their health or life.
    public List<string> ApplicantQuestions { get; set; } = new();
    // What info we need about the nominee (e.g., Name, Phone).
    public List<string> NomineeInformation { get; set; } = new();
    // Special questions for any family members included.
    public List<string> FamilyMemberQuestions { get; set; } = new();
}

/// <summary>
/// Settings for a specific required document.
/// </summary>
public class RequiredDocumentInfo
{
    // The type of document (e.g., "AgeProof").
    public string DocType { get; set; } = string.Empty;
    // If "True", the user cannot finish the application without this file.
    public bool Required { get; set; }
    // A rule saying when this is required (e.g., "If age > 60").
    public string? RequiredCondition { get; set; }
    // A helper tip for the user (e.g., "Upload your PAN card").
    public string? Description { get; set; }
    // A list of allowed file types like "pdf" or "jpg".
    public List<string>? Accepted { get; set; }
}

/// <summary>
/// This represents a specific plan level like "Gold" or "Silver".
/// It defines the coverage amount and the basic starting price.
/// </summary>
public class PolicyTier
{
    // A unique code for this plan (e.g., "gold-tier").
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string TierId { get; set; } = string.Empty;

    // The name shown to the user (e.g., "Gold Plan").
    public string TierName { get; set; } = string.Empty;

    // Which category this plan belongs to.
    [ForeignKey("Category")]
    public string CategoryId { get; set; } = string.Empty;
    public PolicyCategory? Category { get; set; }

    // The maximum money given for claims (e.g., 10 Lakhs).
    public decimal BaseCoverageAmount { get; set; }

    // The starting price before adding risk factors.
    public decimal BasePremiumAmount { get; set; }

    // How many years the policy lasts before it needs renewal.
    public int ValidityInYears { get; set; }

    // A list of extra perks, like "Free Health Checkup" or "Accident Cover".
    public List<string> Benefits { get; set; } = new();
}

/// <summary>
/// This class lists all the things that change the price of insurance.
/// </summary>
public class RiskFactors
{
    // Price increases for older people.
    public List<AgeMultiplier> AgeMultipliers { get; set; } = new();
    // Price changes based on the user's job risk.
    public List<ProfessionMultiplier> ProfessionMultipliers { get; set; } = new();
    // Price changes based on how much the user earns.
    public List<IncomeMultiplier> IncomeMultiplier { get; set; } = new();
    // Extra cost if the user drinks alcohol.
    public AlcoholMultiplier AlcoholMultiplier { get; set; } = new();
    // Extra cost if the user smokes.
    public SmokingMultiplier SmokingMultiplier { get; set; } = new();
    // Price changes based on how much the user travels.
    public List<TravelFrequencyMultiplier> TravelFrequencyMultiplier { get; set; } = new();
    // Price changes based on car or bike type.
    public List<VehicleTypeMultiplier> VehicleTypeMultiplier { get; set; } = new();
}

/// <summary>
/// Multiplier settings for different income levels.
/// </summary>
public class IncomeMultiplier
{
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public double Multiplier { get; set; }
}

/// <summary>
/// Rules for deciding the maximum amount of insurance a user can buy.
/// </summary>
public class CoverageRules
{
    public MaxCoverageBasedOnIncome MaxCoverageBasedOnIncome { get; set; } = new();
}

/// <summary>
/// Logic to limit insurance coverage based on what the user earns.
/// </summary>
public class MaxCoverageBasedOnIncome
{
    // Usually a number like 10 (e.g., Max coverage = Income * 10).
    public int Multiplier { get; set; }
    public string Description { get; set; } = string.Empty;
}

// These small classes help define specific "Multipliers" (e.g., Smokers pay 1.5x more).
public class AgeMultiplier { public int MinAge { get; set; } public int MaxAge { get; set; } public double Multiplier { get; set; } }
public class ProfessionMultiplier { public string Profession { get; set; } = string.Empty; public double Multiplier { get; set; } }
public class AlcoholMultiplier { public double NonDrinker { get; set; } public double Occasional { get; set; } public double Regular { get; set; } }
public class SmokingMultiplier { public double NonSmoker { get; set; } public double Occasional { get; set; } public double Regular { get; set; } }
public class TravelFrequencyMultiplier { public int MaxKmPerMonth { get; set; } public double Multiplier { get; set; } public string? Label { get; set; } }
public class VehicleTypeMultiplier { public string VehicleType { get; set; } = string.Empty; public double Multiplier { get; set; } }
