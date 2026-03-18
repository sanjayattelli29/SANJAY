using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly Kernel _kernel;

        public AIAnalysisService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<string> AskAsync(string prompt)
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.1
            };

            var systemPrompt = @"You are an AI data analyst for AcciSure, an insurance platform.
You have access to tools that fetch live data from the database.
When listing data, you may only see the top 50 records; if so, mention this to the user.
When asked to list or show data, ALWAYS call the appropriate tool first, then present the result.
Format your response as a GitHub-style Markdown table when returning lists of records.
For count or summary questions, respond in a single sentence.
Never make up data — always use the tool results.
If you need an agentId or specific ID and the user has not provided it, 
ask them to provide the ID (you can suggest they check the Agents section).
Today's date context: the platform is AcciSure insurance, India.";

            var fullPrompt = $"{systemPrompt}\n\nUser question: {prompt}";

            var result = await _kernel.InvokePromptAsync(fullPrompt, new KernelArguments(settings));
            return result.ToString();
        }
    }
}
