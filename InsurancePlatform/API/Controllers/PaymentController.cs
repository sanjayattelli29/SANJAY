using Application.Interfaces;
using Microsoft.AspNetCore.Authorization; // login check
using Microsoft.AspNetCore.Mvc; // web api

namespace API.Controllers
{
    // this handles paying for the insurance policies
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // must login
    public class PaymentController : ControllerBase // payment logic api
    {
        private readonly IPolicyService _policyService;

        public PaymentController(IPolicyService policyService)
        {
            _policyService = policyService; // set policy service
        }

        // customer sends money through this
        [HttpPost("process")] // post call for pay
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentProcessRequest request) // receives payment data
        {
            // amount cannot be zero or less
            if (request.Amount <= 0) return BadRequest(new { Message = "Amount must be greater than zero" }); // amount check

            // make a fake transaction id like a real bank would
            var transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // update the system that payment is done
            var success = await _policyService.ProcessPaymentAsync(request.ApplicationId, request.Amount, transactionId); // update db
            
            if (!success) return BadRequest(new { Message = "Payment processing failed. Ensure policy is in AwaitingPayment status." });

            return Ok(new { 
                Status = "Success", 
                TransactionId = transactionId, 
                Message = "Payment successful and policy activated." 
            });
        }
    }
// payment handler end
    public class PaymentProcessRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
