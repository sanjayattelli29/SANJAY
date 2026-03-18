import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { marked } from 'marked';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-analysis-ai-section',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 md:p-10 space-y-6 animate-in fade-in duration-500">
      <!-- Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 class="text-2xl md:text-3xl font-black text-brand-navy tracking-tighter" style="font-family:'Sora',sans-serif">
            AI Data Analysis
          </h2>
          <p class="text-slate-500 text-sm font-medium mt-1">
            Ask natural language questions about your data. The AI will query the live database.
          </p>
        </div>
        <div class="flex items-center gap-2 px-4 py-2 bg-emerald-50 rounded-2xl border border-emerald-100">
          <div class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>
          <span class="text-[10px] font-black text-emerald-700 uppercase tracking-widest">Semantic Kernel Active</span>
        </div>
      </div>

      <!-- Suggested Prompts -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        @for (suggestion of suggestions; track suggestion) {
          <button
            (click)="prompt = suggestion"
            class="text-left p-4 rounded-2xl border border-slate-100 bg-white text-slate-600 hover:border-brand-navy hover:bg-slate-50 transition-all duration-200 group shadow-sm hover:shadow-md">
            <p class="text-xs font-bold group-hover:text-brand-navy">{{ suggestion }}</p>
          </button>
        }
      </div>

      <!-- Input Box -->
      <div class="bg-white p-6 rounded-3xl border border-slate-100 shadow-xl shadow-slate-200/50 space-y-4">
        <div class="relative">
          <textarea
            [(ngModel)]="prompt"
            (keydown.enter)="$event.preventDefault(); askAI()"
            rows="3"
            placeholder="e.g. List all customers assigned to agent X..."
            class="w-full resize-none rounded-2xl border border-slate-100 px-5 py-4 text-sm
                   text-slate-700 focus:outline-none focus:ring-4 focus:ring-brand-navy/5 focus:border-brand-navy/20
                   bg-slate-50/50 transition-all font-medium">
          </textarea>
          <div class="absolute bottom-4 right-4 flex items-center gap-2">
             <button
              (click)="askAI()"
              [disabled]="isLoading() || !prompt.trim()"
              class="px-8 py-3 rounded-xl bg-brand-navy text-white font-black text-xs uppercase tracking-widest
                     hover:bg-opacity-90 transition-all active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg shadow-brand-navy/20">
              @if (isLoading()) {
                <span class="flex items-center gap-2">
                  <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                  </svg>
                  Thinking...
                </span>
              } @else {
                Ask AI Assistant
              }
            </button>
          </div>
        </div>
      </div>

      <!-- Error -->
      @if (error()) {
        <div class="bg-rose-50 border border-rose-100 text-rose-700 rounded-2xl px-6 py-4 flex items-center gap-4 animate-in slide-in-from-top-2">
          <div class="w-10 h-10 rounded-full bg-rose-100 flex items-center justify-center shrink-0">
            <span class="font-black text-lg">!</span>
          </div>
          <div>
            <p class="text-xs font-black uppercase tracking-widest">Analysis Failed</p>
            <p class="text-xs font-bold opacity-80 mt-0.5">{{ error() }}</p>
          </div>
        </div>
      }

      <!-- Loading Placeholder -->
      @if (isLoading() && !result()) {
        <div class="bg-white rounded-3xl border border-slate-100 p-10 flex flex-col items-center justify-center space-y-4 animate-pulse">
           <div class="w-16 h-16 rounded-full bg-slate-50 flex items-center justify-center">
              <svg class="w-8 h-8 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
           </div>
           <p class="text-sm font-black text-slate-300 uppercase tracking-widest">Processing Data...</p>
        </div>
      }

      <!-- Result -->
      @if (result()) {
        <div class="bg-white rounded-3xl border border-slate-100 shadow-xl shadow-slate-200/50 overflow-hidden animate-in zoom-in-95 duration-300">
          <div class="px-8 py-5 bg-slate-50 border-b border-slate-100 flex items-center justify-between">
            <div class="flex items-center gap-3">
               <div class="w-8 h-8 rounded-lg bg-brand-navy flex items-center justify-center">
                  <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
               </div>
               <div>
                  <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest">AI Result Generated</p>
                  <p class="text-xs font-bold text-brand-navy mt-0.5">Live Database Insight</p>
               </div>
            </div>
            <button (click)="clearResult()" class="text-xs font-black text-rose-500 hover:text-rose-700 p-2 uppercase tracking-tighter transition-colors">Clear Result</button>
          </div>
          
          <div class="p-8">
            <!-- Rendered markdown table or text -->
            <div class="prose prose-sm max-w-none ai-result overflow-x-auto"
                 [innerHTML]="renderedHtml()">
            </div>
          </div>
          
          <div class="px-8 py-4 bg-slate-50/50 border-t border-slate-50 flex justify-end">
             <p class="text-[9px] text-slate-400 font-bold italic">Powered by Groq Llama-3 70B & Semantic Kernel</p>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host ::ng-deep .ai-result table {
      width: 100%;
      border-collapse: separate;
      border-spacing: 0;
      font-size: 0.75rem;
      margin: 1.5rem 0;
      border: 1px solid #f1f5f9;
      border-radius: 1rem;
      overflow: hidden;
    }
    :host ::ng-deep .ai-result th {
      background: #0f172a;
      color: white;
      padding: 12px 16px;
      text-align: left;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    :host ::ng-deep .ai-result td {
      padding: 12px 16px;
      border-bottom: 1px solid #f1f5f9;
      color: #475569;
      font-weight: 500;
    }
    :host ::ng-deep .ai-result tr:last-child td {
      border-bottom: none;
    }
    :host ::ng-deep .ai-result tr:nth-child(even) td {
      background: #f8fafc;
    }
    :host ::ng-deep .ai-result tr:hover td {
      background: #f1f5f9;
    }
    :host ::ng-deep .ai-result p {
      margin-bottom: 1rem;
      line-height: 1.6;
      color: #334155;
      font-weight: 500;
    }
  `]
})
export class AnalysisAISectionComponent {
  private adminService = inject(AdminService);
  private sanitizer = inject(DomSanitizer);

  prompt = '';
  isLoading = signal(false);
  result = signal<string | null>(null);
  error = signal<string | null>(null);
  renderedHtml = signal<SafeHtml>('');

  suggestions = [
    'List all customers',
    'Show all pending claims',
    'List all active policies',
    'How many agents are registered?',
    'Show approved claims with amounts',
    'List all rejected policy applications'
  ];

  async askAI() {
    if (!this.prompt.trim()) return;
    this.isLoading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.adminService.askAI(this.prompt).subscribe({
      next: async (res) => {
        this.result.set(res.answer);
        const html = await marked(res.answer);
        this.renderedHtml.set(this.sanitizer.bypassSecurityTrustHtml(html as string));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('AI error:', err);
        const serverError = err.error?.message || err.message || 'Unknown error occurred';
        this.error.set(`AI analysis failed: ${serverError}`);
        this.isLoading.set(false);
      }
    });
  }

  clearResult() {
    this.result.set(null);
    this.renderedHtml.set('');
    this.prompt = '';
  }
}
