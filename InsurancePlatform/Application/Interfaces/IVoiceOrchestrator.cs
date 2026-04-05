using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface is the "Manager" for the Voice AI system.
    /// It handles the entire process of listening, thinking, and talking back to a user.
    /// </summary>
    public interface IVoiceOrchestrator
    {
        // STEP 1: Listen - Turn the user's recorded voice into written text (Transcription)
        Task<string> TranscribeAudioAsync(Stream audioStream);
        
        // STEP 2: Think - Send the written text to the AI to get a helpful response
        // It also uses 'Context' to know which policy and customer it is talking about.
        Task<string> GenerateAiResponseAsync(string transcript, string policyContext, string customerContext);
        
        // STEP 3: Speak - Turn the AI's written answer into a realistic human voice (Speech)
        Task<byte[]> SynthesizeSpeechAsync(string text);
    }
}
