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
    public class VoiceOrchestrator : IVoiceOrchestrator
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<VoiceOrchestrator> _logger;

        // n8n webhook the text chat uses — this ensures consistent, accurate responses
        public VoiceOrchestrator(IConfiguration config, HttpClient httpClient, ILogger<VoiceOrchestrator> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        // STEP 1: Transcribe audio using Deepgram
        public async Task<string> TranscribeAudioAsync(Stream audioStream)
        {
            var dgKey = _config["ExternalApis:Deepgram:ApiKey"];
            _logger.LogInformation("[VoiceAgent] STEP 1: Sending audio to Deepgram...");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepgram.com/v1/listen?model=nova-2&smart_format=true&language=en-IN");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", dgKey);

                var content = new StreamContent(audioStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] Deepgram Error {Status}: {Error}", (int)response.StatusCode, err);
                    return string.Empty;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);

                var transcript = doc.RootElement
                    .GetProperty("results")
                    .GetProperty("channels")[0]
                    .GetProperty("alternatives")[0]
                    .GetProperty("transcript")
                    .GetString();

                _logger.LogInformation("[VoiceAgent] STEP 1 DONE: Transcript = \"{Transcript}\"", transcript);
                return transcript ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VoiceAgent] Deepgram exception: {Message}", ex.Message);
                return string.Empty;
            }
        }

        // STEP 2: Get AI response from n8n — SAME webhook as the text chat
        // Sends the exact same payload structure: { customer, policy, message, question }
        public async Task<string> GenerateAiResponseAsync(string transcript, string policyContext, string customerContext)
        {
            _logger.LogInformation("[VoiceAgent] STEP 2: Calling n8n webhook with transcript: \"{Transcript}\"", transcript);
            _logger.LogInformation("[VoiceAgent] PolicyContext: {Policy}", policyContext ?? "null");
            _logger.LogInformation("[VoiceAgent] CustomerContext: {Customer}", customerContext ?? "null");

            try
            {
                // Parse policy and customer JSON sent from the frontend
                JsonElement policy = default;
                JsonElement customer = default;

                using var policyDoc = !string.IsNullOrWhiteSpace(policyContext)
                    ? JsonDocument.Parse(policyContext)
                    : JsonDocument.Parse("{}");
                policy = policyDoc.RootElement.Clone();

                using var customerDoc = !string.IsNullOrWhiteSpace(customerContext)
                    ? JsonDocument.Parse(customerContext)
                    : JsonDocument.Parse("{}");
                customer = customerDoc.RootElement.Clone();

                // Build the SAME payload structure that sendChatMessage() uses
                var payload = new
                {
                    customer = customer,
                    policy = policy,
                    message = transcript,
                    question = transcript // fallback for n8n AI Agent nodes
                };

                var json = JsonSerializer.Serialize(payload);
                _logger.LogInformation("[VoiceAgent] Sending to n8n: {Payload}", json.Length > 500 ? json.Substring(0, 500) + "..." : json);

                var n8nUrl = _config["ExternalApis:N8n:ChatbotAgentUrl"];
                var request = new HttpRequestMessage(HttpMethod.Post, n8nUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] n8n Error {Status}: {Error}", (int)response.StatusCode, err);
                    return "I'm sorry, I couldn't reach the assistant right now.";
                }

                var resultStr = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[VoiceAgent] n8n raw response: {Response}", resultStr.Length > 300 ? resultStr.Substring(0, 300) : resultStr);

                // n8n returns { reply: "..." } or { answer: "..." } — same as text chat
                using var resultDoc = JsonDocument.Parse(resultStr);
                var root = resultDoc.RootElement;

                string aiReply = null;
                if (root.TryGetProperty("reply", out var replyEl)) aiReply = replyEl.GetString();
                else if (root.TryGetProperty("answer", out var answerEl)) aiReply = answerEl.GetString();
                else if (root.TryGetProperty("output", out var outputEl)) aiReply = outputEl.GetString();

                // Clean markdown symbols (same as the text chat does)
                if (!string.IsNullOrEmpty(aiReply))
                {
                    aiReply = aiReply
                        .Replace("*", "").Replace("_", "").Replace("`", "")
                        .Replace("#", "").Replace(">", "").Replace("- ", "")
                        .Trim();
                }

                aiReply ??= "I'm sorry, I couldn't understand that. Could you please rephrase?";
                _logger.LogInformation("[VoiceAgent] STEP 2 DONE: n8n response = \"{Response}\"", aiReply);
                return aiReply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VoiceAgent] n8n exception: {Message}", ex.Message);
                return "I'm sorry, I'm having trouble right now. Please try again.";
            }
        }

        // STEP 3: Synthesize speech using ElevenLabs
        public async Task<byte[]> SynthesizeSpeechAsync(string text)
        {
            var spokenText = PreprocessForTTS(text);
            _logger.LogInformation("[VoiceAgent] STEP 3: Synthesizing speech. Input text: \"{Text}\"", text);

            try
            {
                var apiKey = _config["ExternalApis:ElevenLabs:ApiKey"];
                // Using the Voice ID and Model from official ElevenLabs snippet for reliability
                var voiceId = "56bWURjYFHyYyVf490Dp"; 
                var modelId = "eleven_multilingual_v2";

                // Added output_format query param for high quality
                var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=mp3_44100_128";
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("xi-api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

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

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[VoiceAgent] ElevenLabs API ERROR! Status: {Status}, Body: {Body}", (int)response.StatusCode, errorBody);
                    return Array.Empty<byte>();
                }

                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                
                if (audioBytes == null || audioBytes.Length == 0)
                {
                    _logger.LogWarning("[VoiceAgent] ElevenLabs returned SUCCESS but EMPTY audio bytes.");
                    return Array.Empty<byte>();
                }

                _logger.LogInformation("[VoiceAgent] ElevenLabs SUCCESS! Received {Size} bytes of audio.", audioBytes.Length);
                return audioBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VoiceAgent] CRITICAL: ElevenLabs exception: {Message}", ex.Message);
                return Array.Empty<byte>();
            }
        }

        // Converts ₹2000000 / ₹20,00,000 → "20 lakhs rupees" for natural TTS pronunciation
        private string PreprocessForTTS(string text)
        {
            // Replace ₹ amounts with spoken words
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

            // Also handle "Rs." or "INR" formats
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

        private string AmountToWords(long amount)
        {
            if (amount >= 10_00_00_000) // 1 crore +
                return $"{amount / 10_00_00_000} crore rupees";
            if (amount >= 1_00_00_000) // >= 1 crore but less
                return $"{amount / 1_00_00_000} crore rupees";
            if (amount >= 10_00_000) // 10 lakhs+
                return $"{amount / 1_00_000} lakhs rupees";
            if (amount >= 1_00_00_000) // 1-9 lakhs (fixed a typo in original logic where it was 1_00_000)
                 return $"{amount / 1_00_000} lakh rupees";
            if (amount >= 1_000) // thousands
                return $"{amount / 1_000} thousand rupees";
            return $"{amount} rupees";
        }
    }
}
