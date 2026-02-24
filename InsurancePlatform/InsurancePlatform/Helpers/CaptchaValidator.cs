using System.Net.Http;
using System.Text.Json;

namespace InsurancePlatform.Helpers
{
    public class CaptchaValidator
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CaptchaValidator(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> ValidateAsync(string token)
        {
            var secretKey = _configuration["Captcha:SecretKey"];

            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);

            var jsonString = await response.Content.ReadAsStringAsync();

            var captchaResponse = JsonSerializer.Deserialize<CaptchaResponse>(jsonString);

            return captchaResponse != null && captchaResponse.success;
        }

        private class CaptchaResponse
        {
            public bool success { get; set; }
        }
    }
}