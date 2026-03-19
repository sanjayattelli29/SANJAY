// production environment configuration
// uses azure backend api
export const environment = {
    production: true,
    apiUrl: 'https://accisurebackend-ejg3c7e3efg6hre8.centralindia-01.azurewebsites.net/api',
    chatHubUrl: 'https://accisurebackend-ejg3c7e3efg6hre8.centralindia-01.azurewebsites.net/api/chathub',
    notificationHubUrl: 'https://accisurebackend-ejg3c7e3efg6hre8.centralindia-01.azurewebsites.net/api/notificationhub',
    voiceAgentApiUrl: 'https://accisurebackend-ejg3c7e3efg6hre8.centralindia-01.azurewebsites.net/api/VoiceAgent/Process',
    googleVisionApiKey: 'AIzaSyAuDTrlRBfVE3rGyBhFp2LyzhPQsivB9N8',
    deepgram_api: '6cb56fe501b72b03393de0e297a55deb41ff8bce',
    elevenlabs_api: 'sk_010607a2b8e038f74f52b761d6dde9b9fcdeb7eafc1880a6',
    VITE_GROQ_API_KEY: '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',
    new_elevenlabs_api: 'sk_03096496bb9d22d051b23d02debc7e51c89280b9588fbd7f',
    vapiPublicKey: '5094349c-e7e7-4805-bbaa-63733c7b34a0',
    vapiAssistantId: 'c6fc9d80-9d40-4b3e-850f-5311c410cc82',
    pythonAiUrl: 'https://accisure-vertex-ai-gvdtfme4fzdsgpcr.centralindia-01.azurewebsites.net' // Update this with Azure link later
};