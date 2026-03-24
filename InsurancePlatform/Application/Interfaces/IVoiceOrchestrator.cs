using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVoiceOrchestrator
    {
        Task<string> TranscribeAudioAsync(Stream audioStream);
        // Calls the same n8n webhook as the text chat — sends transcript + full policy + customer context
        Task<string> GenerateAiResponseAsync(string transcript, string policyContext, string customerContext);
        Task<byte[]> SynthesizeSpeechAsync(string text);
    }
}
