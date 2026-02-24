using InsurancePlatform.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace InsurancePlatform.Services
{
    public class AiService : IAiService
    {
        public Task<decimal> AnalyzeBuyingIntentAsync(string message)
        {
            decimal score = 0;
            string normalized = message.ToLower();

            // Keywords indicating intent
            if (normalized.Contains("buy") || normalized.Contains("purchase")) score += 10;
            if (normalized.Contains("premium") || normalized.Contains("cost") || normalized.Contains("price")) score += 5;
            if (normalized.Contains("interested") || normalized.Contains("want")) score += 5;
            if (normalized.Contains("policy") || normalized.Contains("insurance")) score += 2;
            if (normalized.Contains("cover") || normalized.Contains("sum insured")) score += 5;

            return Task.FromResult(score);
        }
    }
}
