using Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace Infrastructure.ExternalServices
{
    public class VapiClient : IVapiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public VapiClient(HttpClient httpClient, IConfiguration configuration)
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
                    number = phoneNumber.StartsWith("+") ? phoneNumber : $"+91{phoneNumber}",
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
                var responseString = await response.Content.ReadAsStringAsync();
                
                System.IO.File.WriteAllText(@"c:\Sanjay\vapi_debug.txt", 
                    $"Status: {response.StatusCode}\n" +
                    $"Response: {responseString}\n" +
                    $"URL: {baseUrl}/call\n" +
                    $"Body: {JsonSerializer.Serialize(requestBody)}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(@"c:\Sanjay\vapi_debug.txt", 
                    $"Exception: {ex.Message}\n" +
                    $"Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}
