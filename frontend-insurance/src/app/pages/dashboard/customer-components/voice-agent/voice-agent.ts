import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-voice-agent',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './voice-agent.html',
  styleUrls: ['./voice-agent.css'],
})
export class VoiceAgent implements OnInit, OnChanges, OnDestroy {
    @Input() isProcessing = false;
    @Output() audioCaptured = new EventEmitter<Blob>();
    @Output() stopVoiceAgent = new EventEmitter<void>();

    isListening = signal<boolean>(false);
    private mediaRecorder: MediaRecorder | null = null;
    private audioContext: AudioContext | null = null;
    private analyser: AnalyserNode | null = null;
    private microphone: MediaStreamAudioSourceNode | null = null;
    private currentStream: MediaStream | null = null;
    private audioChunks: Blob[] = [];

    // VAD Configuration
    private silenceThreshold = 10; // Volume threshold (0-255)
    private silenceTimeout = 2000; // ms of silence before triggering stop
    private silenceTimer: any = null;
    private animationFrameId: number | null = null;

    // Visualizer state
    volumeLevel = signal<number>(0);

    ngOnInit() {
        console.log('[VoiceAgent] Component mounted. isProcessing =', this.isProcessing);
        if (!this.isProcessing) {
            // No greeting playing — start mic immediately
            this.startListening();
        } else {
            console.log('[VoiceAgent] Waiting for greeting to finish before starting mic...');
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['isProcessing']) {
            const curr = changes['isProcessing'].currentValue;
            const prev = changes['isProcessing'].previousValue;
            
            if (curr === true) {
                console.log('[VoiceAgent] Processing started — ensuring microphone is CLOSED.');
                this.stopAllAudio();
            } else if (prev === true && curr === false) {
                console.log('[VoiceAgent] Processing finished — starting microphone.');
                this.startListening();
            }
        }
    }

    ngOnDestroy() {
        this.stopAllAudio();
    }

    async startListening() {
        if (this.isProcessing) {
            console.log('[VoiceAgent] startListening() skipped — still processing/speaking.');
            return;
        }
        if (this.isListening()) {
            console.log('[VoiceAgent] startListening() skipped — already listening.');
            return;
        }

        console.log('[VoiceAgent] Requesting microphone permission...');

        try {
            this.currentStream = await navigator.mediaDevices.getUserMedia({ audio: true });
            console.log('[VoiceAgent] ✅ Microphone granted! Starting recording...');
            this.isListening.set(true);
            this.audioChunks = [];

            // Setup MediaRecorder
            this.mediaRecorder = new MediaRecorder(this.currentStream);
            this.mediaRecorder.ondataavailable = (e) => {
                if (e.data.size > 0) {
                    this.audioChunks.push(e.data);
                }
            };

            this.mediaRecorder.onstop = () => {
                const totalSize = this.audioChunks.reduce((sum, b) => sum + b.size, 0);
                console.log(`[VoiceAgent] Recording stopped. Chunks=${this.audioChunks.length}, Total size=${totalSize} bytes`);
                if (this.audioChunks.length > 0) {
                    const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
                    console.log(`[VoiceAgent] Emitting audioBlob: size=${audioBlob.size}, type=${audioBlob.type}`);
                    this.audioCaptured.emit(audioBlob);
                } else {
                    console.warn('[VoiceAgent] No audio chunks collected, nothing to emit.');
                }
            };

            this.mediaRecorder.start();
            console.log('[VoiceAgent] 🎙️ MediaRecorder started. Listening for speech...');

            // Setup AudioContext for VAD and Visualizer
            this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.minDecibels = -90;
            this.analyser.maxDecibels = -10;
            this.analyser.smoothingTimeConstant = 0.85;
            this.microphone = this.audioContext.createMediaStreamSource(this.currentStream);
            this.microphone.connect(this.analyser);

            this.detectSilence();

        } catch (err) {
            console.error('[VoiceAgent] ❌ Error accessing microphone:', err);
            alert('Could not access microphone. Please ensure permissions are granted.');
            this.cancelVoiceAgent();
        }
    }

    private detectSilence() {
        if (!this.analyser || !this.isListening()) return;

        this.analyser.fftSize = 256;
        const bufferLength = this.analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        const checkAudio = () => {
            if (!this.analyser || !this.isListening()) return;

            this.analyser.getByteFrequencyData(dataArray);
            let sum = 0;
            for (let i = 0; i < bufferLength; i++) {
                sum += dataArray[i];
            }
            const averageVolume = sum / bufferLength;
            this.volumeLevel.set(averageVolume);

            if (averageVolume < this.silenceThreshold) {
                if (!this.silenceTimer) {
                    this.silenceTimer = setTimeout(() => {
                        console.log('[VoiceAgent] 🔇 Silence detected for 2s — stopping recording and emitting...');
                        this.stopRecordingAndEmit();
                    }, this.silenceTimeout);
                }
            } else {
                if (this.silenceTimer) {
                    clearTimeout(this.silenceTimer);
                    this.silenceTimer = null;
                }
            }

            this.animationFrameId = requestAnimationFrame(checkAudio);
        };

        checkAudio();
    }

    private stopRecordingAndEmit() {
        this.isListening.set(false);
        if (this.silenceTimer) {
            clearTimeout(this.silenceTimer);
            this.silenceTimer = null;
        }

        if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
            this.mediaRecorder.stop();
        }

        this.stopAllAudio();
    }

    private stopAllAudio() {
        this.isListening.set(false);
        if (this.silenceTimer) {
            clearTimeout(this.silenceTimer);
            this.silenceTimer = null;
        }
        if (this.animationFrameId) {
            cancelAnimationFrame(this.animationFrameId);
            this.animationFrameId = null;
        }
        if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
            this.mediaRecorder.stop();
        }
        if (this.currentStream) {
            this.currentStream.getTracks().forEach(track => track.stop());
            this.currentStream = null;
        }
        if (this.audioContext && this.audioContext.state !== 'closed') {
            this.audioContext.close();
            this.audioContext = null;
        }
    }

    cancelVoiceAgent() {
        this.stopAllAudio();
        this.stopVoiceAgent.emit();
    }
}
