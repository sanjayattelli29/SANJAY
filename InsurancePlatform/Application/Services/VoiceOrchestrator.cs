using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    ///  service is the for our AI Voice system.
    /// It connects three different AI tools together:
    /// 1. Deepgram (to listen and type what it hears)
    /// 2. n8n (the brain that decides what to say)
    /// 3. ElevenLabs (the voice that speaks the answer back)
    public class VoiceOrchestrator : IVoiceOrchestrator
    {
        // These are the tools we need to work with settings, web requests, and logging.
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<VoiceOrchestrator> _logger;

        // The constructor sets up the tools we need when the service starts.
        public VoiceOrchestrator(IConfiguration config, HttpClient httpClient, ILogger<VoiceOrchestrator> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// STEP 1: Listen to the user's voice and turn it into written text.
        /// We use a high-quality tool called Deepgram Nova-2 for this.
        public async Task<string> TranscribeAudioAsync(Stream audioStream)
        {
            // Get our secret API key for Deepgram from the configuration settings.
            var dgKey = _config["ExternalApis:Deepgram:ApiKey"];
            _logger.LogInformation("[VoiceAgent] STEP 1: Sending audio to Deepgram...");

            try
            {
                // We send the audio to Deepgram and ask it to use the "Nova-2" model.
                // We also ask for Indian English and smart formatting (like adding dots and capitals).
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepgram.com/v1/listen?model=nova-2&smart_format=true&language=en-IN");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", dgKey);

                // Prepare the audio content to be sent. We tell Deepgram it's an "audio/webm" file.
                var content = new StreamContent(audioStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
                request.Content = content;

                // Send the request and wait for Deepgram to finish listening.
                var response = await _httpClient.SendAsync(request);

                // If Deepgram has an issue (like a wrong API key), we log the error and stop.
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] Deepgram Error {Status}: {Error}", (int)response.StatusCode, err);
                    return string.Empty;
                }

                // Read the JSON results from Deepgram and extract the text transcript.
                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);

                // Deepgram returns results in a nested format: results -> channels -> alternatives -> transcript.
                var transcript = doc.RootElement
                    .GetProperty("results")
                    .GetProperty("channels")[0]
                    .GetProperty("alternatives")[0]
                    .GetProperty("transcript")
                    .GetString();

                // Log exactly what the user said so we can debug easily.
                _logger.LogInformation("[VoiceAgent] STEP 1 DONE: Transcript = \"{Transcript}\"", transcript);
                return transcript ?? "";
            }
            catch (Exception ex)
            {
                // If the internet crashes or any other big error happens, we log it here.
                _logger.LogError(ex, "[VoiceAgent] Deepgram exception: {Message}", ex.Message);
                return string.Empty;
            }
        }

        /// Send the written text to our AI "Brain" (n8n) and get a helpful reply.
        /// We include context about the customer and their policy so the AI can give accurate advice.
        public async Task<string> GenerateAiResponseAsync(string transcript, string policyContext, string customerContext)
        {
            // Log the input transcript and the context data for debugging.
            _logger.LogInformation("[VoiceAgent] STEP 2: Calling n8n webhook with transcript: \"{Transcript}\"", transcript);
            _logger.LogInformation("[VoiceAgent] PolicyContext: {Policy}", policyContext ?? "null");
            _logger.LogInformation("[VoiceAgent] CustomerContext: {Customer}", customerContext ?? "null");

            try
            {
                // Parse policy and customer JSON sent from the frontend into usable objects.
                JsonElement policy = default;
                JsonElement customer = default;

                // Safely handle cases where the policy or customer info might be missing.
                using var policyDoc = !string.IsNullOrWhiteSpace(policyContext)
                    ? JsonDocument.Parse(policyContext)
                    : JsonDocument.Parse("{}");
                policy = policyDoc.RootElement.Clone();

                using var customerDoc = !string.IsNullOrWhiteSpace(customerContext)
                    ? JsonDocument.Parse(customerContext)
                    : JsonDocument.Parse("{}");
                customer = customerDoc.RootElement.Clone();

                // Build the message package (payload) according to what our n8n workflow expects.
                var payload = new
                {
                    customer = customer,
                    policy = policy,
                    message = transcript,
                    question = transcript // fallback for n8n AI Agent nodes
                };

                // Convert the message package into a JSON string and log a bit of it.
                var json = JsonSerializer.Serialize(payload);
                _logger.LogInformation("[VoiceAgent] Sending to n8n: {Payload}", json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                // Get our n8n webhook URL from configuration and send the request.
                var n8nUrl = _config["ExternalApis:N8n:ChatbotAgentUrl"];
                var request = new HttpRequestMessage(HttpMethod.Post, n8nUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                // If n8n returns an error, log it and return a polite fallback message.
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] n8n Error {Status}: {Error}", (int)response.StatusCode, err);
                    return "I'm sorry, I couldn't reach the assistant right now.";
                }

                // Parse the response from n8n to find the AI's reply.
                var resultStr = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[VoiceAgent] n8n raw response: {Response}", resultStr.Length > 300 ? resultStr.Substring(0, 300) : resultStr);

                // n8n might return the answer in a field called 'reply', 'answer', or 'output'.
                using var resultDoc = JsonDocument.Parse(resultStr);
                var root = resultDoc.RootElement;

                string aiReply = null;
                if (root.TryGetProperty("reply", out var replyEl)) aiReply = replyEl.GetString();
                else if (root.TryGetProperty("answer", out var answerEl)) aiReply = answerEl.GetString();
                else if (root.TryGetProperty("output", out var outputEl)) aiReply = outputEl.GetString();

                // Clean up any markdown symbols (like **) so the AI doesn't try to "speak" them.
                if (!string.IsNullOrEmpty(aiReply))
                {
                    aiReply = aiReply
                        .Replace("*", "").Replace("_", "").Replace("`", "")
                        .Replace("#", "").Replace(">", "").Replace("- ", "")
                        .Trim();
                }

                // If for some reason we got no reply, use a friendly default message.
                aiReply ??= "I'm sorry, I couldn't understand that. Could you please rephrase?";
                _logger.LogInformation("[VoiceAgent] STEP 2 DONE: n8n response = \"{Response}\"", aiReply);
                return aiReply;
            }
            catch (Exception ex)
            {
                // Log any unexpected problems during the AI response generation.
                _logger.LogError(ex, "[VoiceAgent] n8n exception: {Message}", ex.Message);
                return "I'm sorry, I'm having trouble right now. Please try again.";
            }
        }

        /// Take the final AI text and turn it into a high-quality human voice.
        /// We use ElevenLabs for a very realistic-sounding AI voice.
        public async Task<byte[]> SynthesizeSpeechAsync(string text)
        {
            // First, optimize the text for natural speaking (e.g., converting symbols and units).
            var spokenText = PreprocessForTTS(text);
            _logger.LogInformation("[VoiceAgent] STEP 3: Synthesizing speech. Input text: \"{Text}\"", text);

            try
            {
                // Get our ElevenLabs API key and pick a specific voice and model.
                var apiKey = _config["ExternalApis:ElevenLabs:ApiKey"];
                var voiceId = "56bWURjYFHyYyVf490Dp"; 
                var modelId = "eleven_multilingual_v2";

                // Prepare the URL. We ask for high-quality MP3 (44kHz).
                var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=mp3_44100_128";
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("xi-api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

                // Submit the text and voice settings to ElevenLabs.
                var payload = new
                {
                    text = spokenText,
                    model_id = modelId,
                    voice_settings = new
                    {
                        stability = 0.5,
                        similarity_boost = 0.75
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[VoiceAgent] Sending request to ElevenLabs... Model: {Model}, Voice: {Voice}", modelId, voiceId);
                
                var response = await _httpClient.SendAsync(request);

                // If ElevenLabs has an issue, log the detailed error and return nothing.
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] ElevenLabs API ERROR! Status: {Status}, Body: {Body}", (int)response.StatusCode, errorBody);
                    return Array.Empty<byte>();
                }

                // Download the final MP3 voice file as a byte array.
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                
                if (audioBytes == null || audioBytes.Length == 0)
                {
                    _logger.LogWarning("[VoiceAgent] ElevenLabs returned SUCCESS but EMPTY audio bytes.");
                    return Array.Empty<byte>();
                }

                // Log the final success and return the audio to the frontend.
                _logger.LogInformation("[VoiceAgent] ElevenLabs SUCCESS! Received {Size} bytes of audio.", audioBytes.Length);
                return audioBytes;
            }
            catch (Exception ex)
            {
                // Log any critical errors that happen during voice synthesis.
                _logger.LogError(ex, "[VoiceAgent] CRITICAL: ElevenLabs exception: {Message}", ex.Message);
                return Array.Empty<byte>();
            }
        }

        /// This helper function prepares the text for the voice AI.
        /// It converts currency symbols (₹) and amounts into spoken words like "Lakhs" and "Crores".
        private string PreprocessForTTS(string text)
        {
            // Find Rupee symbols and numbers (e.g., ₹10,00,000) and convert them to Indian numbering words.
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"₹\s*([\d,]+)",
                m =>
                {
                    var numStr = m.Groups[1].Value.Replace(",", "");
                    if (long.TryParse(numStr, out long amount))
                        return AmountToWords(amount);
                    return m.Value;
                }
            );

            // Also handle variants like "INR" to make sure all currency is spoken correctly.
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\bINR\s*([\d,]+)",
                m =>
                {
                    var numStr = m.Groups[1].Value.Replace(",", "");
                    if (long.TryParse(numStr, out long amount))
                        return AmountToWords(amount);
                    return m.Value;
                }
            );

            return text;
        }

        /// Helper function to convert large numbers into traditional Indian words like Lakhs and Crores.
        private string AmountToWords(long amount)
        {
            if (amount >= 10_00_00_000) // 10 Crores and above.
                return $"{amount / 10_00_00_000} crore rupees";
            if (amount >= 1_00_00_000) // Between 1 and 9.9 Crores.
                return $"{amount / 1_00_00_000} crore rupees";
            if (amount >= 10_00_000) // 10 Lakhs and above.
                return $"{amount / 1_00_000} lakhs rupees";
            if (amount >= 1_00_000) // Between 1 and 9.9 Lakhs.
                 return $"{amount / 1_00_000} lakh rupees";
            if (amount >= 1_000) // Thousands.
                return $"{amount / 1_000} thousand rupees";
            return $"{amount} rupees";
        }
    }
}
