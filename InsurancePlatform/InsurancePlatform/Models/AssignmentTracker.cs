using System;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class AssignmentTracker : BaseEntity
    {
        public Guid? LastAssignedAgentId { get; set; }

        public Guid? LastAssignedClaimsOfficerId { get; set; }
    }
}