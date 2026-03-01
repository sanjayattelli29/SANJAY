using Application.Interfaces; // service types
using Domain.Enums; // staff roles
using Microsoft.AspNetCore.Authorization; // login security
using Microsoft.AspNetCore.Mvc; // web api tools

namespace API.Controllers
{
    // this handles all the money and payment reports
    [ApiController] // web api
    [Route("api/[controller]")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Agent},{UserRoles.ClaimOfficer}")] // staff roles
    public class ReportController : ControllerBase // reports web api
    {
        private readonly IPolicyService _policyService; // policy logic

        public ReportController(IPolicyService policyService)
        {
            _policyService = policyService; // set policy service
        }

        // get the big list of everyone's payments
        [HttpGet("unified-payments")] // get request for report
        public async Task<IActionResult> GetUnifiedPayments() // fetch all payments
        {
            var reports = await _policyService.GetUnifiedPaymentsAsync();
            return Ok(reports);
        }
    }
}
// report controller end
