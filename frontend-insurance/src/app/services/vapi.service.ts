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

  // Start the voice call
  async startCall() {
    if (this.isCallActive()) return;
    
    this.callStatus.set('connecting');
    try {
      await this.vapi.start(environment.vapiAssistantId);
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
