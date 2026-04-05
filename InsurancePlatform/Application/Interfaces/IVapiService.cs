namespace Application.Interfaces
{
    /// <summary>
    /// This interface is used to make automated phone calls to customers.
    /// We use it to welcome new customers with a friendly automated voice.
    /// </summary>
    public interface IVapiService
    {
        // Triggers the system to call a customer's phone number and say a welcome message
        Task<bool> TriggerWelcomeCallAsync(string phoneNumber, string customerName);
    }
}
