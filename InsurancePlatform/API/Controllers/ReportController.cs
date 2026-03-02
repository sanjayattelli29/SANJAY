using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // this handles all the money and payment reports
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Agent},{UserRoles.ClaimOfficer}")]
    public class ReportController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public ReportController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        // get the big list of everyone's payments
        [HttpGet("unified-payments")]
        public async Task<IActionResult> GetUnifiedPayments()
        {
            var reports = await _policyService.GetUnifiedPaymentsAsync();
            return Ok(reports);
        }
    }
}
