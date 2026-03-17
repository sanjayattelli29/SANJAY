namespace Application.Interfaces
{
    public interface IVapiService
    {
        Task<bool> TriggerWelcomeCallAsync(string phoneNumber, string customerName);
    }
}
