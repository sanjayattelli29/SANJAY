namespace Application.Interfaces.Infrastructure
{
    public interface IVapiService
    {
        Task<bool> TriggerWelcomeCallAsync(string phoneNumber, string customerName);
    }
}
