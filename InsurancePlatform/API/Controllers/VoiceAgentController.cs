using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Application.Interfaces;

namespace API.Controllers
{
    public class VoiceProcessRequest
    {
        public IFormFile AudioFile { get; set; }
        public string PolicyId { get; set; }
        public string CustomerId { get; set; }
        public string PolicyContext { get; set; }    // Full policy JSON (same as text chat sends)
        public string CustomerContext { get; set; }  // Full customer JSON (same as text chat sends)
    }

    [ApiController]
    [Route("api/[controller]")]
    public class VoiceAgentController : ControllerBase
    {
        private readonly IVoiceOrchestratorService _voiceService;

        public VoiceAgentController(IVoiceOrchestratorService voiceService)
        {
            _voiceService = voiceService;
        }

        [HttpPost("Process")]
        public async Task<IActionResult> ProcessVoice([FromForm] VoiceProcessRequest request)
        {
            if (request.AudioFile == null || request.AudioFile.Length == 0)
                return BadRequest("No audio file provided.");

            try
            {
                using var stream = request.AudioFile.OpenReadStream();

                // STEP 1: Transcribe audio using Deepgram
                var transcript = await _voiceService.TranscribeAudioAsync(stream);
                if (string.IsNullOrWhiteSpace(transcript))
                    return Ok(new { Transcript = "", AiResponse = "I couldn't hear that clearly. Could you please repeat?" });

                // STEP 2: Get AI response from n8n — using full policy + customer context
                // Same as the text chat: sends {customer, policy, message, question} to n8n
                var aiTextResponse = await _voiceService.GenerateAiResponseAsync(
                    transcript,
                    request.PolicyContext,
                    request.CustomerContext
                );

                // STEP 3: Synthesize speech using ElevenLabs
                var audioBytes = await _voiceService.SynthesizeSpeechAsync(aiTextResponse);

                return Ok(new
                {
                    Transcript = transcript,
                    AiResponse = aiTextResponse,
                    AudioBase64 = Convert.ToBase64String(audioBytes)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
