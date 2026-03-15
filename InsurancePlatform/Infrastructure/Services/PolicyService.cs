using Application.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Domain.Enums;
using System.Text.Json;

namespace Infrastructure.Services
{
    // this class manages everything about policies like plans and premiums
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorage; // Added
        private static PolicyConfiguration? _cachedConfig;

        public PolicyService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            INotificationService notificationService,
            IFileStorageService fileStorage) // Added fileStorage to constructor
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _fileStorage = fileStorage; // Initialized _fileStorage
        }
        // code to read the configuration from json file
        public async Task<PolicyConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfig != null) return _cachedConfig;

            // find the path to policy-config.json
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "policy-config.json");
            
            if (!File.Exists(path))
            {
                // check in application directory (for production)
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "policy-config.json");
            }

            if (!File.Exists(path))
            {
                // last resort: check root of application directory
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "policy-config.json");
            }

            var json = await File.ReadAllTextAsync(path);
            _cachedConfig = JsonSerializer.Deserialize<PolicyConfiguration>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return _cachedConfig!;
        }

        // math to find how much money policy costs
        public async Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request)
        {
            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == request.PolicyCategory);
            if (category == null) throw new Exception("Invalid category");

            var tier = category.Tiers.FirstOrDefault(t => t.TierId == request.TierId);
            if (tier == null) throw new Exception("Invalid tier");

            var applicant = request.PolicyCategory == "INDIVIDUAL" ? request.Applicant : request.PrimaryApplicant;
            if (applicant == null) throw new Exception("Applicant details missing");

            double multiplier = 1.0;

            // Age Multiplier
            var ageM = config.RiskFactors.AgeMultipliers.FirstOrDefault(m => applicant.Age >= m.MinAge && applicant.Age <= m.MaxAge);
            multiplier *= ageM?.Multiplier ?? 1.2;

            // Profession Multiplier
            var profM = config.RiskFactors.ProfessionMultipliers.FirstOrDefault(m => string.Equals(m.Profession, applicant.Profession, StringComparison.OrdinalIgnoreCase));
            multiplier *= profM?.Multiplier ?? 1.0;

            // Income Multiplier
            var incomeM = config.RiskFactors.IncomeMultiplier.FirstOrDefault(m => request.AnnualIncome >= m.MinIncome && request.AnnualIncome <= m.MaxIncome);
            multiplier *= incomeM?.Multiplier ?? 1.0;

            // Alcohol Multiplier (normalize habits to lowercase)
            var alcoholKey = applicant.AlcoholHabit.ToLower().Replace(" ", "");
            multiplier *= alcoholKey switch
            {
                "nondrinker" => config.RiskFactors.AlcoholMultiplier.NonDrinker,
                "occasional" => config.RiskFactors.AlcoholMultiplier.Occasional,
                "regular" => config.RiskFactors.AlcoholMultiplier.Regular,
                _ => 1.0
            };

            // Smoking Multiplier
            var smokingKey = applicant.SmokingHabit.ToLower().Replace(" ", "");
            multiplier *= smokingKey switch
            {
                "nonsmoker" => config.RiskFactors.SmokingMultiplier.NonSmoker,
                "occasional" => config.RiskFactors.SmokingMultiplier.Occasional,
                "regular" => config.RiskFactors.SmokingMultiplier.Regular,
                _ => 1.0
            };

            // Travel Multiplier
            var travelM = config.RiskFactors.TravelFrequencyMultiplier
                .OrderBy(m => m.MaxKmPerMonth)
                .FirstOrDefault(m => applicant.TravelKmPerMonth <= m.MaxKmPerMonth) 
                ?? config.RiskFactors.TravelFrequencyMultiplier.OrderByDescending(m => m.MaxKmPerMonth).First();
            multiplier *= travelM.Multiplier;
            
            // Vehicle Type Multiplier
            var vehicleM = config.RiskFactors.VehicleTypeMultiplier.FirstOrDefault(m => string.Equals(m.VehicleType, applicant.VehicleType, StringComparison.OrdinalIgnoreCase));
            multiplier *= vehicleM?.Multiplier ?? 1.0;

            // Payment Mode Multiplier
            multiplier *= request.PaymentMode.ToLower() switch
            {
                "monthly" => config.PaymentModeMultipliers.Monthly,
                "halfyearly" => config.PaymentModeMultipliers.HalfYearly,
                "yearly" => config.PaymentModeMultipliers.Yearly,
                _ => 1.0
            };

            return tier.BasePremiumAmount * (decimal)multiplier;
        }

        // customer uses this when they want to buy a policy
        public async Task<AuthResponseDto> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request)
        {
            // make sure customer doesn't already have active policy in same category
            var alreadyHasActivePolicy = await _context.PolicyApplications
                .AnyAsync(pa => pa.UserId == userId && 
                                pa.PolicyCategory == request.PolicyCategory && 
                                pa.Status == "Active");

            if (alreadyHasActivePolicy)
            {
                throw new Exception("You already have an active policy in this category.");
            }

            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == request.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == request.TierId);

            if (tier == null) throw new Exception("Invalid policy tier.");

            var premium = await CalculatePremiumAsync(request);
            var applicant = request.PolicyCategory == "INDIVIDUAL" ? request.Applicant! : request.PrimaryApplicant!;

            // create a new application record with structured fields
            var application = new PolicyApplication
            {
                UserId = userId,
                PolicyCategory = request.PolicyCategory,
                TierId = request.TierId,
                CalculatedPremium = premium,
                SubmissionDate = DateTime.UtcNow,
                Status = "Pending", // Admin still needs to assign agent
                
                // Structured Underwriting Data
                Age = applicant.Age,
                Profession = applicant.Profession,
                AnnualIncome = request.AnnualIncome,
                AlcoholHabit = applicant.AlcoholHabit,
                SmokingHabit = applicant.SmokingHabit,
                TravelKmPerMonth = applicant.TravelKmPerMonth,
                VehicleType = applicant.VehicleType,

                // Map Location components
                Address = request.Location?.Address,
                State = request.Location?.State,
                District = request.Location?.District,
                Pincode = request.Location?.Pincode,
                Latitude = request.Location?.Latitude,
                Longitude = request.Location?.Longitude,

                ApplicationDataJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };

            _context.PolicyApplications.Add(application);
            await _context.SaveChangesAsync(); // generate application.Id

            // Save Nominee
            if (request.Nominee != null)
            {
                var nominee = new NomineeDetails
                {
                    PolicyApplicationId = application.Id,
                    NomineeName = request.Nominee.Name,
                    Relationship = request.Nominee.Relationship,
                    NomineePhone = request.Nominee.Phone,
                    NomineeEmail = request.Nominee.Email,
                    BankAccountNumber = request.Nominee.BankAccount,
                    IFSC = request.Nominee.IFSC,
                    AadharNumber = request.Nominee.AadharNumber ?? string.Empty,
                    AadharCardUrl = request.Nominee.AadharCardUrl ?? string.Empty
                };
                _context.NomineeDetails.Add(nominee);
            }

            // Save Family Members
            if (request.PolicyCategory == "FAMILY" && request.FamilyMembers != null)
            {
                foreach (var fm in request.FamilyMembers)
                {
                    var member = new FamilyMember
                    {
                        PolicyApplicationId = application.Id,
                        FullName = fm.FullName,
                        Relation = fm.Relation,
                        DateOfBirth = fm.DateOfBirth,
                        ExistingHealthConditions = fm.HealthConditions
                    };
                    _context.FamilyMembers.Add(member);
                }
            }

            await _context.SaveChangesAsync();

            // tell the customer their application is submitted
            await _notificationService.SendNotificationAsync(userId, "Application Submitted", 
                $"Your {request.TierId} policy application has been submitted successfully.", $"Policy:{application.Id}");

            // Get user email for admin notification
            var user = await _userManager.FindByIdAsync(userId);
            string userEmail = user?.Email ?? userId;

            // tell admin to assign agent
            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationAsync(admin.Id, "New Policy Application 📝", 
                    $"New {request.TierId} application from {userEmail} needs agent assignment.", $"ADM:Policy:{application.Id}");
            }

            return new AuthResponseDto { Status = "Success", Message = application.Id };
        }

        // shows all policies that a customer has bought or applied for
        public async Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.UserId == userId)
                .OrderByDescending(pa => pa.SubmissionDate)  // newest first
                .ToListAsync();
        }

        // admin uses this to see every policy application from all customers
        public async Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync()
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)  // also load customer details
                .Include(pa => pa.AssignedAgent)  // also load agent details if assigned
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        // shows which agent has how many applications so admin can balance work
        public async Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync()
        {
            // first get all users who are agents
            var agents = await _userManager.GetUsersInRoleAsync(UserRoles.Agent);
            
            var workloads = new List<AgentWorkloadDto>();
            // for each agent count how many policies they're handling
            foreach (var agent in agents)
            {
                var count = await _context.PolicyApplications.CountAsync(pa => pa.AssignedAgentId == agent.Id);
                workloads.Add(new AgentWorkloadDto
                {
                    AgentId = agent.Id,
                    AgentEmail = agent.Email ?? string.Empty,
                    AssignedPolicyCount = count
                });
            }
            return workloads;
        }

        // admin uses this to give an application to a specific agent
        public async Task<bool> AssignAgentAsync(string applicationId, string agentId)
        {
            // find the application in database
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null) return false;

            // check if agent exists
            var agent = await _userManager.FindByIdAsync(agentId);
            if (agent == null) return false;

            // link the agent to this application
            app.AssignedAgentId = agentId;
            app.Status = "Assigned";  // update status
            
            await _context.SaveChangesAsync();

            // tell customer that someone is now working on their application
            await _notificationService.SendNotificationAsync(app.UserId, "Agent Assigned", 
                $"Agent {agent.Email} has been assigned to your {app.TierId} policy application.", $"Policy:{applicationId}");
            
            // Get user for display name
            var applicantUser = await _userManager.FindByIdAsync(app.UserId);
            string applicantEmail = applicantUser?.Email ?? app.UserId;

            // tell agent they got            // Notify agent
            await _notificationService.SendNotificationAsync(agentId, "New Assignment 📁", 
                $"You have been assigned a new policy application from {applicantEmail}.", $"AGENT:Policy:{applicationId}");

            return true;
        }

        // agent sees all applications assigned to them
        public async Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)  // load customer info too
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.AssignedAgentId == agentId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        // agent uses this to approve or reject customer's policy application
        public async Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId)
        {
            // find the application and make sure it's assigned to this agent
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null || app.AssignedAgentId != agentId) return false;

            // if agent approves it
            if (status == "Approved")
            {
                app.ApprovedByAgentId = agentId;
                app.ApprovedAt = DateTime.UtcNow;
                app.Status = "AwaitingPayment";  // customer needs to pay now
            }
            else
            {
                app.Status = "Rejected";  // application denied
            }

            await _context.SaveChangesAsync();

            // send notification to customer about the decision
            string title = status == "Approved" ? "Payment Required 💳" : "Application Rejected ❌";
            string displayId = applicationId.Substring(0, 3).ToUpper();
            string message = status == "Approved" ? 
                $"Please complete your payment of {app.CalculatedPremium:C} for {app.TierId}. ID: {displayId}..." : 
                $"Your application for {app.TierId} (ID: {displayId}...) has been rejected.";
            
            await _notificationService.SendNotificationAsync(app.UserId, title, message, $"Policy:{applicationId}");

            return true;
        }

        // this runs when customer pays their premium and activates the policy
        public async Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId)
        {
            // make sure application exists and is waiting for payment
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null || app.Status != "AwaitingPayment") return false;

            // mark policy as active and save payment details
            app.Status = "Active";
            app.PaidAmount = amount;
            app.PaymentDate = DateTime.UtcNow;
            app.TransactionId = transactionId;
            app.StartDate = DateTime.UtcNow;  // policy starts today

            // figure out when policy expires based on plan validity
            var config = await GetConfigurationAsync();
            var category = config.PolicyCategories.FirstOrDefault(c => c.CategoryId == app.PolicyCategory);
            var tier = category?.Tiers.FirstOrDefault(t => t.TierId == app.TierId);
            
            int validityYears = tier?.ValidityInYears ?? 1; 
            app.ExpiryDate = app.StartDate.Value.AddYears(validityYears);
            
            // find how customer wants to pay monthly yearly etc
            try 
            {
                var appData = JsonDocument.Parse(app.ApplicationDataJson);
                if (appData.RootElement.TryGetProperty("paymentMode", out var modeProp))
                {
                    app.PaymentMode = modeProp.GetString();
                }
            }
            catch {}

            if (string.IsNullOrEmpty(app.PaymentMode)) app.PaymentMode = "yearly";

            // calculate when next payment is due
            app.NextPaymentDate = app.PaymentMode.ToLower() switch
            {
                "monthly" => app.StartDate.Value.AddMonths(1),
                "halfyearly" => app.StartDate.Value.AddMonths(6),
                "yearly" => app.ExpiryDate,
                _ => app.ExpiryDate
            };

            // setup coverage amounts for claims tracking
            app.TotalCoverageAmount = tier?.BaseCoverageAmount ?? 0;
            app.RemainingCoverageAmount = app.TotalCoverageAmount;  // full coverage available
            app.TotalApprovedClaimsAmount = 0;  // nothing claimed yet

            await _context.SaveChangesAsync();

            // tell customer their policy is now active
            await _notificationService.SendNotificationAsync(app.UserId, "Policy Activated", 
                $"Your {app.TierId} policy is now ACTIVE. Coverage starts from today.", $"CUST:Policy:{applicationId}");

            // Get user for commission alert 
            var customerUser = await _userManager.FindByIdAsync(app.UserId);
            string customerEmail = customerUser?.Email ?? app.UserId;

            // Notify the assigned agent about the payment and commission
            if (!string.IsNullOrEmpty(app.AssignedAgentId))
            {
                decimal commission = app.CalculatedPremium * 0.10m;
                await _notificationService.SendNotificationAsync(app.AssignedAgentId, "Commission Earned 💰", 
                    $"Customer {customerEmail} has paid for their {app.TierId} policy. You earned {commission:C} commission!", $"AGENT:Commission:{applicationId}");
            }
            else
            {
                // If no agent, notify admin about a direct payment
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendNotificationAsync(admin.Id, "Direct Payment Received", 
                        $"Customer {customerEmail} has paid directly for {app.TierId}.", $"ADM:Policy:{applicationId}");
                }
            }

            return true;
        }

        // agent can see how much money they made from commissions
        public async Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId)
        {
            // get all active policies this agent helped sell
            var activePolicies = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.AssignedAgentId == agentId && pa.Status == "Active")
                .OrderByDescending(pa => pa.PaymentDate)
                .ToListAsync();

            // calculate total commission - agent gets 10% of each premium
            decimal totalCommission = activePolicies.Sum(pa => pa.CalculatedPremium * 0.10m);

            return new AgentCommissionDto
            {
                TotalCommission = totalCommission,
                ActivePolicies = activePolicies
            };
        }

        // agent sees list of all customers they have worked with
        public async Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)  // bring customer details
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.AssignedAgentId == agentId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        public async Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId)
        {
            var allAssigned = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.AssignedAgentId == agentId)
                .ToListAsync();

            var activePolicies = allAssigned.Where(pa => pa.Status == "Active").ToList();
            
            // Total Claims for these policies
            var policyIds = allAssigned.Select(pa => pa.Id).ToList();
            var claims = await _context.InsuranceClaims
                .Where(c => policyIds.Contains(c.PolicyApplicationId))
                .ToListAsync();

            var analytics = new AgentAnalyticsDto
            {
                TotalCoverageProvided = activePolicies.Sum(pa => pa.TotalCoverageAmount),
                ActivePolicyCount = activePolicies.Count,
                UniqueCustomerCount = allAssigned.Select(pa => pa.UserId).Distinct().Count(),
                TotalPremiumCollected = activePolicies.Sum(pa => pa.PaidAmount ?? 0),
                TotalCommissionEarned = activePolicies.Sum(pa => pa.CalculatedPremium * 0.10m),

                BestPerformingCategory = allAssigned.GroupBy(pa => pa.PolicyCategory)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "N/A",

                BestPerformingTier = allAssigned.GroupBy(pa => pa.TierId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "N/A",

                // Portfolio Mix (Category Distribution)
                PortfolioMix = allAssigned.GroupBy(pa => pa.PolicyCategory)
                    .Select(g => new CategoryDistribution { Category = g.Key, Count = g.Count() })
                    .ToList(),

                // Tier Breakdown
                TierBreakdown = allAssigned.GroupBy(pa => pa.TierId)
                    .Select(g => new TierDistribution { Tier = g.Key, Count = g.Count() })
                    .ToList(),

                // Status Metrics
                PolicyStatusMetrics = allAssigned.GroupBy(pa => pa.Status)
                    .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                    .ToList(),

                // Commission & Premium Trends (Last 6 Months)
                CommissionPerformance = activePolicies
                    .Where(pa => pa.PaymentDate.HasValue)
                    .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                    .OrderBy(g => g.Min(pa => pa.PaymentDate))
                    .Select(g => new MonthlyDataPoint { Month = g.Key, Value = g.Sum(pa => pa.CalculatedPremium * 0.10m) })
                    .TakeLast(6)
                    .ToList(),

                PremiumTrends = activePolicies
                    .Where(pa => pa.PaymentDate.HasValue)
                    .GroupBy(pa => pa.PaymentDate!.Value.ToString("MMM yyyy"))
                    .OrderBy(g => g.Min(pa => pa.PaymentDate))
                    .Select(g => new MonthlyDataPoint { Month = g.Key, Value = g.Sum(pa => pa.PaidAmount ?? 0) })
                    .TakeLast(6)
                    .ToList(),

                // Claim Impact
                ClaimImpact = new List<ClaimImpactData>
                {
                    new ClaimImpactData { Metric = "Premium Collected", Value = activePolicies.Sum(pa => pa.PaidAmount ?? 0) },
                    new ClaimImpactData { Metric = "Approved Claims", Value = claims.Where(c => c.Status == "Approved").Sum(c => c.ApprovedAmount) },
                    new ClaimImpactData { Metric = "Pending Claims", Value = claims.Where(c => c.Status == "Pending" || c.Status == "Assigned").Sum(c => c.RequestedAmount) }
                }
            };

            return analytics;
        }

        public async Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync()
        {
            var reports = await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.AssignedAgent)
                .Select(pa => new UnifiedPaymentDto
                {
                    ApplicationId = pa.Id,
                    CustomerEmail = pa.User != null ? pa.User.Email : "Unknown",
                    AgentEmail = pa.AssignedAgent != null ? pa.AssignedAgent.Email : "Unassigned",
                    // We take the first assigned officer from any associated claim for this policy
                    ClaimsOfficerEmail = _context.InsuranceClaims
                        .Include(c => c.AssignedOfficer)
                        .Where(c => c.PolicyApplicationId == pa.Id && c.AssignedClaimOfficerId != null)
                        .Select(c => c.AssignedOfficer.Email)
                        .FirstOrDefault() ?? "N/A",
                    PolicyCategory = pa.PolicyCategory,
                    TierId = pa.TierId,
                    TotalCoverage = pa.TotalCoverageAmount,
                    CurrentCoverage = pa.RemainingCoverageAmount,
                    PremiumAmount = pa.CalculatedPremium,
                    PaidAmount = pa.PaidAmount,
                    NextPaymentDate = pa.NextPaymentDate,
                    LastPaymentDate = pa.PaymentDate,
                    TransactionId = pa.TransactionId,
                    PaymentMode = pa.PaymentMode,
                    Status = pa.Status
                })
                .ToListAsync();

            return reports;
        }

        public async Task<AuthResponseDto> SubmitPolicyDocumentsAsync(SubmitPolicyDocumentsRequest request)
        {
            var application = await _context.PolicyApplications.FindAsync(request.PolicyApplicationId);
            if (application == null) throw new Exception("Application not found.");

            foreach (var doc in request.Documents)
            {
                using var stream = doc.File.OpenReadStream();
                var uploadResult = await _fileStorage.UploadFileAsync(stream, doc.File.FileName, "policy-docs");

                var policyDoc = new ApplicationDocument
                {
                    PolicyApplicationId = application.Id,
                    DocumentType = doc.DocumentType,
                    FileName = uploadResult.FileName,
                    FileUrl = uploadResult.FileUrl,
                    FileSize = uploadResult.FileSize,
                    UploadedAt = DateTime.UtcNow
                };
                _context.ApplicationDocuments.Add(policyDoc);
            }

            application.Status = "PendingReview";
            await _context.SaveChangesAsync();

            return new AuthResponseDto { Status = "Success", Message = "Documents uploaded and application is now under review." };
        }
    
        public async Task<bool> UpdateInvoiceUrlAsync(string applicationId, string invoiceUrl)
        {
            var app = await _context.PolicyApplications.FindAsync(applicationId);
            if (app == null) return false;

            app.InvoiceUrl = invoiceUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> UploadGeneralFileAsync(Stream fileStream, string fileName, string folder)

        {
            var result = await _fileStorage.UploadFileAsync(fileStream, fileName, folder);
            return result.FileUrl;
        }
    }
}