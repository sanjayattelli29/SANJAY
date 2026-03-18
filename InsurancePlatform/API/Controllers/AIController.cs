using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces.Services;
using Domain.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/Admin/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AIController : ControllerBase
    {
        private readonly IAIAnalysisService _aiService;

        public AIController(IAIAnalysisService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AIAskRequest request)
        {
            try
            {
                var result = await _aiService.AskAsync(request.Prompt);
                return Ok(new { Answer = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine("AI Controller Exception: " + ex.ToString());
                return StatusCode(500, new { error = "AI analysis failed.", details = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }

    public record AIAskRequest(string Prompt);
}
