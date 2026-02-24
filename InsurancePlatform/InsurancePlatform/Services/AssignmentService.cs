using InsurancePlatform.Data;
using InsurancePlatform.Models;
using InsurancePlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InsurancePlatform.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public AssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> GetLeastLoadedAgentIdAsync()
        {
            var agent = await _context.AgentProfiles
                .Where(a => a.IsApprovedByAdmin)
                .OrderBy(a => a.ActiveCustomerCount)
                .FirstOrDefaultAsync();

            if (agent == null)
                throw new Exception("No active agents available for assignment");

            return agent.UserId;
        }

        public async Task<Guid> GetLeastLoadedAgentByLeadCountAsync()
        {
            var agent = await _context.AgentProfiles
                .Where(a => a.IsApprovedByAdmin)
                .OrderBy(a => a.ActiveLeadCount)
                .FirstOrDefaultAsync();

            if (agent == null)
                throw new Exception("No active agents available for lead assignment");

            return agent.UserId;
        }

        public async Task<Guid> GetLeastLoadedClaimsOfficerIdAsync()
        {
            var officer = await _context.ClaimsOfficerProfiles
                .Where(o => o.IsApprovedByAdmin)
                .OrderBy(o => o.ActiveClaimCount)
                .FirstOrDefaultAsync();

            if (officer == null)
                throw new Exception("No active claims officers available for assignment");

            return officer.UserId;
        }
    }
}
