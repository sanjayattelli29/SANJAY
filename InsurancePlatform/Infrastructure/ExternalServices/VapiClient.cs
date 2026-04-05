using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Application.Interfaces;

namespace Infrastructure.ExternalServices
{
    /// <summary>
    /// This service talks to "Vapi", an AI Voice platform.
    /// Its main job is to trigger an automatic "Welcome Call" to a customer
    /// right after they register on our website.
    /// </summary>
    public class VapiClient : IVapiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // The constructor gets access to our secret settings (like the Vapi API key).
        public VapiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// This method starts a phone call from our AI Assistant to the customer.
        /// </summary>
        public async Task<bool> TriggerWelcomeCallAsync(string phoneNumber, string customerName)
        {
            // We load the security key and ID for our AI Assistant from the settings.
            var apiKey = _configuration["Vapi:ApiKey"];
            var assistantId = _configuration["Vapi:AssistantId"];
            var phoneNumberId = _configuration["Vapi:PhoneNumberId"]; // The phone number the AI will "dial from".
            var baseUrl = _configuration["Vapi:BaseUrl"] ?? "https://api.vapi.ai";

            // If we don't have the keys, we can't make the call.
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(assistantId))
            {
                return false;
            }

            // We create a "Call Package" (request body) with the customer's phone and name.
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

            // Convert the package into a JSON string and prepare it for the web.
            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // Add our secret API key to the message as a "Bearer" token so Vapi knows it's us.
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                // Send the "Start Call" command to the Vapi URL.
                var response = await _httpClient.PostAsync($"{baseUrl}/call", requestContent);
                
                // Return "True" if the call was successfully queued, "False" if it failed.
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // If there's a problem with the internet, return false.
                return false;
            }
        }
    }
}
