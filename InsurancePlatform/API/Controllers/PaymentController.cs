using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PaymentController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentProcessRequest request)
        {
            if (request.Amount <= 0) return BadRequest(new { Message = "Amount must be greater than zero" });

            // Simulated transaction ID
            var transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            var success = await _policyService.ProcessPaymentAsync(request.ApplicationId, request.Amount, transactionId);
            
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
