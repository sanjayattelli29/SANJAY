import { Injectable, signal } from '@angular/core';
import Vapi from '@vapi-ai/web';
import { environment } from '../../environments/environment';

// Service to handle Vapi.ai voice calls
@Injectable({
  providedIn: 'root'
})
export class VapiService {
  private vapi = new Vapi(environment.vapiPublicKey);

  // Reactive signals for UI state
  isCallActive = signal<boolean>(false);
  isMuted = signal<boolean>(false);
  callStatus = signal<'idle' | 'connecting' | 'listening' | 'speaking'>('idle');

  constructor() {
    this.setupEventListeners();
  }
 //Listening for Events
  private setupEventListeners() {
    this.vapi.on('call-start', () => {
      console.log('[Vapi] Call started');
      this.isCallActive.set(true);
      this.callStatus.set('listening');
    });

    this.vapi.on('call-end', () => {
      console.log('[Vapi] Call ended');
      this.isCallActive.set(false);
      this.callStatus.set('idle');
    });

    this.vapi.on('speech-start', () => {
      console.log('[Vapi] AI speaking');
      this.callStatus.set('speaking');
    });

    this.vapi.on('speech-end', () => {
      console.log('[Vapi] AI finished speaking');
      this.callStatus.set('listening');
    });

    this.vapi.on('error', (error) => {
      console.error('[Vapi] Error:', error);
      this.isCallActive.set(false);
      this.callStatus.set('idle');
    });
  }

  // Start the voice call with optional userId for personalization
  async startCall(userId: string = 'guest') {
    if (this.isCallActive()) return;
    
    this.callStatus.set('connecting');
    try {
      // Start the call with metadata AND assistant overrides
      // This tells the AI who the user is so it doesn't have to ask
      await this.vapi.start(environment.vapiAssistantId, {
        metadata: {
          userId: userId
        },
        variableValues: {
          userId: userId
        },
        artifactPlan: {
          recordingEnabled: true
        },
        model: {
          provider: 'openai',
          model: 'gpt-4o',
          tools: [
            {
              type: 'function',
              function: {
                name: 'getClaims',
                description: 'Fetch the insurance claims filed by the current user.',
                parameters: {
                  type: 'object',
                  properties: {
                    userId: { type: 'string', description: 'The unique ID of the user.' }
                  },
                  required: ['userId']
                }
              },
              server: {
                url: `${environment.apiUrl}/VapiWebhook/Process`
              }
            },
            {
              type: 'function',
              function: {
                name: 'getPolicies',
                description: 'Fetch the insurance policies for the current user.',
                parameters: {
                  type: 'object',
                  properties: {
                    userId: { type: 'string', description: 'The unique ID of the user.' }
                  },
                  required: ['userId']
                }
              },
              server: {
                url: `${environment.apiUrl}/VapiWebhook/Process`
              }
            },
            {
              type: 'function',
              function: {
                name: 'getNotifications',
                description: 'Fetch the notifications for the current user.',
                parameters: {
                  type: 'object',
                  properties: {
                    userId: { type: 'string', description: 'The unique ID of the user.' }
                  },
                  required: ['userId']
                }
              },
              server: {
                url: `${environment.apiUrl}/VapiWebhook/Process`
              }
            }
          ],
          messages: [
            {
              role: 'system',
              content: `You are an AI assistant for insurance claims and policies. The current user's ID is ${userId}. 
                NEVER ask the user for their ID. Always use the provided tools like 'getClaims', 'getPolicies', and 'getNotifications' to fetch real-time information for the user whenever they ask about their account.
                Identify yourself as a personalized assistant.`
            }
          ]
        }
      });
    } catch (error) {
      console.error('[Vapi] Failed to start call:', error);
      this.isCallActive.set(false);
      this.callStatus.set('idle');
    }
  }

  // Stop the voice call
  stopCall() {
    if (!this.isCallActive()) return;
    this.vapi.stop();
  }

  // Toggle mute state
  toggleMute() {
    const newState = !this.isMuted();
    this.vapi.setMuted(newState);
    this.isMuted.set(newState);
  }
}
