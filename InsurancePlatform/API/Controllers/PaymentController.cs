using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // this handles paying for the insurance policies
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPolicyManager _policyManager;

        public PaymentController(IPolicyManager policyManager)
        {
            _policyManager = policyManager;
        }

        // customer sends money through this
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentProcessRequest request)
        {
            // amount cannot be zero or less
            if (request.Amount <= 0) return BadRequest(new { Message = "Amount must be greater than zero" });

            // make a fake transaction id like a real bank would
            var transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // update the system that payment is done
            var success = await _policyManager.ProcessPaymentAsync(request.ApplicationId, request.Amount, transactionId);
            
            if (!success) return BadRequest(new { Message = "Payment processing failed. Ensure policy is in AwaitingPayment status." });

            return Ok(new { 
                Status = "Success", 
                TransactionId = transactionId, 
                Message = "Payment successful and policy activated." 
            });
        }
    }

    public class PaymentProcessRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
