using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Application.Interfaces;

namespace API.Controllers
{
  
    public class VoiceProcessRequest
    {
        // The actual voice recording file (like a .wav or .mp3)
        public IFormFile AudioFile { get; set; }
        
        // The unique ID of the insurance policy
        public string PolicyId { get; set; }
        
        // The unique ID of the customer
        public string CustomerId { get; set; }
        
        // Detailed information about the policy in JSON format (helps the AI understand the rules)
        public string PolicyContext { get; set; }
        
        // Detailed information about the customer in JSON format (helps the AI know who it's talking to)
        public string CustomerContext { get; set; }
    }

 
    /// This controller handles all requests related to the Voice AI Agent.
   
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceAgentController : ControllerBase
    {
        // This service does the heavy lifting: transcribing, AI thinking, and speaking.
        private readonly IVoiceOrchestrator _voiceService;

        public VoiceAgentController(IVoiceOrchestrator voiceService)
        {
            _voiceService = voiceService;
        }

     
        [HttpPost("Process")]
        public async Task<IActionResult> ProcessVoice([FromForm] VoiceProcessRequest request)
        {
            // First, check if the user actually sent an audio file
            if (request.AudioFile == null || request.AudioFile.Length == 0)
                return BadRequest("No audio file provided.");

            try
            {
                using var stream = request.AudioFile.OpenReadStream();

                //  Transcribe audio (Voice to Text)
                // We use Deepgram to "listen" to the audio and give us the written text.
                var transcript = await _voiceService.TranscribeAudioAsync(stream);
                
                // If we couldn't understand anything, ask the user to repeat.
                if (string.IsNullOrWhiteSpace(transcript))
                    return Ok(new { Transcript = "", AiResponse = "I couldn't hear that clearly. Could you please repeat?" });

                // We send the written text (transcript) plus the policy and customer info to the AI.
                // The AI then comes up with a helpful answer.
                var aiTextResponse = await _voiceService.GenerateAiResponseAsync(
                    transcript,
                    request.PolicyContext,
                    request.CustomerContext
                );

                // Synthesize speech (Text to Voice)
                // We take the AI's written answer and use ElevenLabs to turn it into a realistic human voice.
                var audioBytes = await _voiceService.SynthesizeSpeechAsync(aiTextResponse);

                // Convert the audio data into a Base64 string so it can be easily sent over the internet to the browser.
                var audioBase64 = (audioBytes != null && audioBytes.Length > 0) ? Convert.ToBase64String(audioBytes) : "";
                
                // Send everything back to the user: what they said, what the AI said, and the audio recording of the AI talking.
                return Ok(new
                {
                    Transcript = transcript,      // What the user said
                    AiResponse = aiTextResponse,  // What the AI typed
                    AudioBase64 = audioBase64,    // The AI's voice (as data)
                    AudioSize = audioBytes?.Length ?? 0,
                    DebugInfo = audioBytes?.Length == 0 ? "ElevenLabs returned 0 bytes. Check server terminal for exact API error body." : "OK"
                });
            }
            catch (Exception ex)
            {
                // If anything goes wrong (like the internet cutting out), return an error message.
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
