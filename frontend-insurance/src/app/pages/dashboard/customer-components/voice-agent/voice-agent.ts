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

    // ─── VAD Configuration ────────────────────────────────────────────────────
    // FIX 1: Raised threshold from 20 → 35 to ignore fan/AC background hum.
    //         Fan hum is typically a constant low rumble; speech peaks are sharp
    //         and louder. Tune this up/down if needed in your environment.
    private silenceThreshold = 35;

    // Silence timeout: how long (ms) of quiet AFTER speech before we submit.
    // 1200ms feels natural — long enough to handle brief breath pauses,
    // short enough to feel responsive after you finish a sentence.
    private silenceTimeout = 1200;

    // Minimum duration (ms) of audio above threshold to count as "real speech".
    // 800ms prevents fan-noise bursts from being treated as speech,
    // while still catching short replies like "Yes" or "No".
    private minSpeechDuration = 800;
    private speechStartTime: number | null = null;
    private hasSpeechContent = false;

    private silenceTimer: ReturnType<typeof setTimeout> | null = null;
    private animationFrameId: number | null = null;

    // Visualizer state
    volumeLevel = signal<number>(0);

    ngOnInit() {
        console.log('[VoiceAgent] Component mounted. isProcessing =', this.isProcessing);
        if (!this.isProcessing) {
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
            this.hasSpeechContent = false;
            this.speechStartTime = null;

            // ── MediaRecorder setup ──────────────────────────────────────────
            this.mediaRecorder = new MediaRecorder(this.currentStream);

            this.mediaRecorder.ondataavailable = (e) => {
                if (e.data.size > 0) {
                    this.audioChunks.push(e.data);
                }
            };

            // FIX 3: Guard against double-stop by using a local flag.
            //         Previously stopRecordingAndEmit() called stopAllAudio()
            //         which called mediaRecorder.stop() a second time, risking
            //         onstop firing twice and emitting two blobs.
            let emitHandled = false;
            this.mediaRecorder.onstop = () => {
                if (emitHandled) return;
                emitHandled = true;

                const totalSize = this.audioChunks.reduce((sum, b) => sum + b.size, 0);
                console.log(`[VoiceAgent] Recording stopped. Chunks=${this.audioChunks.length}, Total size=${totalSize} bytes`);

                // FIX 4: Only emit if we actually captured real speech content,
                //         not just silence/fan noise.
                if (this.audioChunks.length > 0 && this.hasSpeechContent) {
                    // Where final audio is created
                    // This runs after recording stops
                    const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
                    console.log(`[VoiceAgent] ✅ Emitting audioBlob: size=${audioBlob.size}, type=${audioBlob.type}`);
                    this.audioCaptured.emit(audioBlob);
                } else if (!this.hasSpeechContent) {
                    console.warn('[VoiceAgent] ⚠️ No real speech detected (only silence/noise) — not emitting. Restarting mic.');
                    // Resume listening if we didn't actually get speech
                    if (!this.isProcessing) {
                        setTimeout(() => this.startListening(), 300);
                    }
                } else {
                    console.warn('[VoiceAgent] No audio chunks collected, nothing to emit.');
                }
            };

            this.mediaRecorder.start();
            console.log('[VoiceAgent] 🎙️ MediaRecorder started. Listening for speech...');

            // ── AudioContext / Analyser setup ────────────────────────────────
            this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 512; // Higher FFT = more frequency resolution
            this.analyser.minDecibels = -85;
            this.analyser.maxDecibels = -10;
            this.analyser.smoothingTimeConstant = 0.75; // Slightly less smoothing = faster reaction

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

        const bufferLength = this.analyser.frequencyBinCount; // = fftSize / 2 = 256
        const dataArray = new Uint8Array(bufferLength);

        // FIX 5: With fftSize=512, each bin = sampleRate(48000) / fftSize(512) ≈ 94Hz.
        //         Skip bins 0–3 (0–375Hz) to avoid fan/AC hum (typically 50–120Hz).
        //         Focus on bins 4–32 = ~375Hz to ~3kHz (core human speech range).
        //         Fan noise is concentrated in bins 0–3; skipping them avoids false triggers.
        const SPEECH_BIN_START = 4;
        const SPEECH_BIN_END = 32;

        const checkAudio = () => {
            if (!this.analyser || !this.isListening()) return;

            this.analyser.getByteFrequencyData(dataArray);

            // FIX 6: Use AVERAGE energy in the speech band, not just peak max.
            //         A single noisy bin won't spike the average as easily.
            let sum = 0;
            for (let i = SPEECH_BIN_START; i <= SPEECH_BIN_END; i++) {
                sum += dataArray[i];
            }
            const avgVolume = sum / (SPEECH_BIN_END - SPEECH_BIN_START + 1);

            this.volumeLevel.set(Math.round(avgVolume));

            const isSpeaking = avgVolume >= this.silenceThreshold;

            if (isSpeaking) {
                // Track first time we detect speech above threshold
                if (!this.speechStartTime) {
                    this.speechStartTime = Date.now();
                    console.log(`[VoiceAgent] 🗣️ Speech detected (avg: ${avgVolume.toFixed(1)})`);
                }

                // Mark hasSpeechContent once user has spoken for minSpeechDuration
                if (!this.hasSpeechContent && (Date.now() - this.speechStartTime) >= this.minSpeechDuration) {
                    this.hasSpeechContent = true;
                    console.log('[VoiceAgent] ✅ Minimum speech duration met — will submit on silence.');
                }

                // Clear silence timer whenever voice is detected
                if (this.silenceTimer) {
                    clearTimeout(this.silenceTimer);
                    this.silenceTimer = null;
                }

            } else {
                // FIX 7: Only start the silence countdown if we already have real speech.
                //         This prevents the timer from firing immediately on component mount
                //         before the user has said anything.
                if (this.hasSpeechContent && !this.silenceTimer) {
                    // FIX 8: Capture the threshold in a const so the log inside setTimeout
                    //         always shows the correct value (not a stale closure).
                    const thresholdSnapshot = this.silenceThreshold;
                    const avgSnapshot = avgVolume.toFixed(1);

                    this.silenceTimer = setTimeout(() => {
                        console.log(`[VoiceAgent] 🔇 Silence for ${this.silenceTimeout}ms (avg: ${avgSnapshot} < threshold: ${thresholdSnapshot}) — stopping.`);
                        this.stopRecordingAndEmit();
                    }, this.silenceTimeout);
                }
            }

            this.animationFrameId = requestAnimationFrame(checkAudio);
        };

        checkAudio();
    }

    private stopRecordingAndEmit() {
        // FIX 9: Set isListening false and cancel animation frame FIRST so
        //         detectSilence's checkAudio loop exits immediately and doesn't
        //         queue more frames or timers.
        this.isListening.set(false);

        if (this.silenceTimer) {
            clearTimeout(this.silenceTimer);
            this.silenceTimer = null;
        }
        if (this.animationFrameId) {
            cancelAnimationFrame(this.animationFrameId);
            this.animationFrameId = null;
        }

        // Stop the MediaRecorder — this triggers onstop → emit
        if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
            this.mediaRecorder.stop();
        }

        // FIX 10: Release stream/context AFTER stopping the recorder so that
        //          ondataavailable fires one final time before we tear everything down.
        //          Use a small delay to ensure the final chunk is captured.
        setTimeout(() => this.releaseAudioResources(), 200);
    }

    // Separated from stopAllAudio so we can call it safely after recorder stops
    private releaseAudioResources() {
        if (this.currentStream) {
            this.currentStream.getTracks().forEach(track => track.stop());
            this.currentStream = null;
        }
        if (this.audioContext && this.audioContext.state !== 'closed') {
            this.audioContext.close();
            this.audioContext = null;
        }
        this.analyser = null;
        this.microphone = null;
    }

    private stopAllAudio() {
        this.isListening.set(false);
        this.hasSpeechContent = false;
        this.speechStartTime = null;

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
        this.analyser = null;
        this.microphone = null;
    }

    cancelVoiceAgent() {
        this.stopAllAudio();
        this.stopVoiceAgent.emit();
    }
}