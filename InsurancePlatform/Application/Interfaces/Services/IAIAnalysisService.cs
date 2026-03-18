namespace Application.Interfaces.Services
{
    public interface IAIAnalysisService
    {
        Task<string> AskAsync(string prompt);
    }
}
