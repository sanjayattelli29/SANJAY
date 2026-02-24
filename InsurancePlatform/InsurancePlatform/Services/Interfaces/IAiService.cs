using System;
using System.Threading.Tasks;

namespace InsurancePlatform.Services.Interfaces
{
    public interface IAiService
    {
        Task<decimal> AnalyzeBuyingIntentAsync(string message);
    }
}
