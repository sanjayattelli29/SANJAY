using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class VapiService : IVapiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public VapiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> TriggerWelcomeCallAsync(string phoneNumber, string customerName)
        {
            var apiKey = _configuration["Vapi:ApiKey"];
            var assistantId = _configuration["Vapi:AssistantId"];
            var phoneNumberId = _configuration["Vapi:PhoneNumberId"];
            var baseUrl = _configuration["Vapi:BaseUrl"] ?? "https://api.vapi.ai";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(assistantId))
            {
                return false;
            }

            var requestBody = new
            {
                assistantId = assistantId,
                phoneNumberId = phoneNumberId,
                customer = new
                {
                    number = phoneNumber,
                    name = customerName
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _httpClient.PostAsync($"{baseUrl}/call", requestContent);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
