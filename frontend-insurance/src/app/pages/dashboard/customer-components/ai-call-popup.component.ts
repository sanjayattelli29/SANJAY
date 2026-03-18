import { Component, inject, signal, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VapiService } from '../../../services/vapi.service';

@Component({
  selector: 'app-ai-call-popup',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen) {
    <!-- Mini-Bar Container: Positioned top-right below the header -->
    <div class="fixed top-24 right-6 z-[120] animate-in slide-in-from-top-4 duration-300">
      <div class="bg-[#1e293b]/95 backdrop-blur-md text-white rounded-[2rem] shadow-2xl px-4 py-2.5 flex items-center gap-4 border border-white/5 ring-1 ring-black/20">
        
        <!-- Icon 1: Mic / Status Indicator -->
        <div class="relative flex items-center justify-center">
          @if (vapiService.isCallActive()) {
            <div class="absolute inset-0 bg-brand-orange/30 rounded-full animate-ping duration-1000"></div>
          }
          <div [class]="'w-12 h-12 rounded-full flex items-center justify-center transition-all duration-500 ' + 
               (vapiService.callStatus() === 'speaking' ? 'bg-emerald-500/20 text-emerald-400 shadow-[0_0_15px_rgba(52,211,153,0.3)]' : 
                vapiService.callStatus() === 'listening' ? 'bg-brand-orange/20 text-brand-orange shadow-[0_0_15px_rgba(255,108,0,0.3)]' : 
                'bg-slate-700/50 text-slate-300')">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z" />
            </svg>
          </div>
        </div>

        <!-- Vertical Divider -->
        <div class="h-8 w-px bg-white/10 mx-1"></div>

        <!-- Icon 2: Mute / Speaker Toggle -->
        <button (click)="vapiService.toggleMute()" 
          class="p-3 rounded-full transition-all duration-300 hover:bg-white/5 active:scale-95 group"
          [class.text-rose-500]="vapiService.isMuted()"
          [class.text-slate-300]="!vapiService.isMuted()">
          <svg class="w-6 h-6 group-hover:scale-110 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            @if (vapiService.isMuted()) {
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2" />
            } @else {
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z" />
            }
          </svg>
        </button>

        <!-- Icon 3: End Call (Red circular button) -->
        <button (click)="vapiService.stopCall(); close.emit()" 
          class="w-12 h-12 bg-[#f43f5e] hover:bg-[#e11d48] text-white rounded-full flex items-center justify-center transition-all duration-300 shadow-lg shadow-rose-900/40 active:scale-90 group">
          <svg class="w-6 h-6 group-hover:rotate-90 transition-transform duration-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

      </div>
    </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class AICallPopupComponent {
  vapiService = inject(VapiService);

  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
}
