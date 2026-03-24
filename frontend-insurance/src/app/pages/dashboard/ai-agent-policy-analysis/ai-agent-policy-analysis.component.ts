import { Component, Input, OnInit, AfterViewInit, ViewChild, ElementRef, signal, effect, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';
import { AgentService } from '../../../services/agent.service';
import { environment } from '../../../../environments/environment';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as htmlToImage from 'html-to-image';

Chart.register(...registerables);

@Component({
  selector: 'app-ai-agent-policy-analysis',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-agent-policy-analysis.component.html'
})
export class AiAgentPolicyAnalysisComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() policyData: any = null;
  @ViewChild('riskBarChart') riskBarChartRef!: ElementRef;

  isAnalyzing = signal(false);
  analysisResult = signal<any>(null);
  analysisError = signal<string | null>(null);

  private barChartInstance: Chart | null = null;
  private viewInitialized = false;
  private pendingRenderData: any = null;

  constructor(private agentService: AgentService, private cdr: ChangeDetectorRef) {
    // Watch for analysis result — if view not ready, defer rendering
    effect(() => {
      const result = this.analysisResult();
      if (result && result.riskFactors?.length) {
        if (this.viewInitialized) {
          setTimeout(() => this.renderCharts(result), 100);
        } else {
          this.pendingRenderData = result;
        }
      }
    });
  }

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    if (this.pendingRenderData) {
      setTimeout(() => this.renderCharts(this.pendingRenderData), 100);
      this.pendingRenderData = null;
    }
  }

  ngOnDestroy(): void {
    if (this.barChartInstance) this.barChartInstance.destroy();
  }

  async runCompleteAnalysis() {
    if (!this.policyData) {
      console.warn('[AI Analysis] No policyData provided — cannot run analysis.');
      return;
    }

    this.isAnalyzing.set(true);
    this.analysisResult.set(null);
    this.analysisError.set(null);

    // ── Build comprehensive payload ──────────────────────────────────────────
    const policy   = this.policyData.policy   || this.policyData || {};
    const customer = this.policyData.customer  || this.policyData.user || {};
    const claim    = this.policyData.claim     || null;
    const agent    = this.policyData.agent     || null;
    const claimOfficer = this.policyData.claimOfficer || null;

    // Full applicant details from fullDetails or top-level
    const fullDetails = policy.fullDetails || {};
    const applicant   = fullDetails.applicant || {};
    const nominee     = fullDetails.nominee   || {};
    const location    = fullDetails.location  || {};
    const familyMembers = fullDetails.familyMembers || policy.familyMembers || [];

    // Robust parsing of applicationDataJson like in dashboard
    let raw: any = {};
    try {
      raw = typeof policy.applicationDataJson === 'string'
        ? JSON.parse(policy.applicationDataJson)
        : (policy.applicationDataJson || {});
    } catch (e) {}

    const annualIncomeFallback = policy.annualIncome || policy.AnnualIncome || raw.AnnualIncome || applicant.annualIncome || 0;
    const professionFallback = policy.profession || policy.Profession || raw.Profession || applicant.profession || 'Standard';

    // Documents — collect from all possible locations
    const docs1 = Array.isArray(policy.documents)                 ? policy.documents                 : [];
    const docs2 = Array.isArray(fullDetails.documents)            ? fullDetails.documents             : [];
    const docsAll = [...docs2, ...docs1].filter(Boolean);
    const seen = new Set<string>();
    const uniqueDocs = docsAll.filter((d: any) => {
      const key = `${d.fileUrl || d.url || ''}|${d.fileName || ''}`;
      if (seen.has(key)) return false;
      seen.add(key); return true;
    });

    // Claims documents (if it's a claim review)
    const claimDocs = claim?.documents || [];

    // Structured customer payload
    const customerPayload = {
      email:         customer.email         || '',
      fullName:      customer.fullName       || customer.name || applicant.fullName || '',
      phone:         customer.phoneNumber    || customer.phone || '',
      age:           customer.age            || applicant.age || '',
      annualIncome:  annualIncomeFallback,
      profession:    professionFallback,
    };

    // Structured policy payload with EVERYTHING relevant
    const policyPayload = {
      policyId:            policy.id              || '',
      policyNumber:        policy.policyNumber     || policy.id?.substring(0, 8).toUpperCase() || '',
      policyName:          policy.policyName       || '',
      policyCategory:      policy.policyCategory   || '',
      tierId:              policy.tierId           || '',
      status:              policy.status           || '',
      paymentMode:         policy.paymentMode      || '',
      calculatedPremium:   policy.calculatedPremium || 0,
      monthlyPremium:      policy.monthlyPremium   || 0,
      basePremiumAmount:   policy.basePremiumAmount || 0,
      coverageAmount:      policy.coverageAmount    || policy.totalCoverageAmount || 0,
      applicant: {
        fullName:        applicant.fullName       || '',
        age:             applicant.age            || '',
        profession:      professionFallback,
        annualIncome:    annualIncomeFallback,
        smokingHabit:    applicant.smokingHabit   || applicant.SmokingHabit || raw.SmokingHabit || '',
        alcoholHabit:    applicant.alcoholHabit   || applicant.AlcoholHabit || raw.AlcoholHabit || '',
        vehicleType:     applicant.vehicleType    || applicant.VehicleType || raw.VehicleType || '',
        travelKmPerMonth:applicant.travelKmPerMonth || applicant.TravelKmPerMonth || raw.TravelKmPerMonth || 0,
      },
      nominee: {
        name:            nominee.name             || '',
        relationship:    nominee.relationship     || '',
        email:           nominee.email            || '',
        phone:           nominee.phone            || '',
        bankAccount:     nominee.bankAccount      || '',
        ifsc:            nominee.ifsc             || '',
        aadharNumber:    nominee.aadharNumber     || '',
        hasAadharCard:   !!nominee.aadharCardUrl,
      },
      location: {
        address:  location.address  || '',
        state:    location.state    || '',
        district: location.district || '',
        pincode:  location.pincode  || '',
      },
      familyMembers: familyMembers.map((m: any) => ({
        fullName:         m.fullName         || '',
        relation:         m.relation         || '',
        dateOfBirth:      m.dateOfBirth      || '',
        healthConditions: m.healthConditions || '',
      })),
      documents: uniqueDocs.map((d: any) => ({
        documentType: d.documentType || '',
        fileName:     d.fileName     || '',
        fileSize:     d.fileSize     || 0,
        hasUrl:       !!(d.fileUrl   || d.url),
      })),
      agentEmail:        agent?.email       || '',
      claimOfficerEmail: claimOfficer?.email|| '',
    };

    // Claims payload (for claim reviews)
    const claimsPayload = claim ? {
      claimId:           claim.id              || '',
      incidentType:      claim.incidentType    || '',
      incidentDate:      claim.incidentDate    || '',
      description:       claim.description     || '',
      requestedAmount:   claim.requestedAmount || 0,
      approvedAmount:    claim.approvedAmount  || 0,
      status:            claim.status          || '',
      documents: claimDocs.map((d: any) => ({
        documentType: d.documentType || '',
        fileName:     d.fileName     || '',
      })),
    } : null;

    // ── Log exactly what is going to the AI ────────────────────────────────
    console.group('🤖 [AI Analysis] Sending to Python Backend');
    console.log('📋 Customer:', customerPayload);
    console.log('📄 Policy:', policyPayload);
    console.log('⚕️ Claim:', claimsPayload);
    console.log('📁 Documents count:', uniqueDocs.length, uniqueDocs.map((d: any) => d.documentType));
    console.groupEnd();

    const formData = new FormData();
    formData.append('customer',       JSON.stringify(customerPayload));
    formData.append('policy',         JSON.stringify(policyPayload));
    formData.append('claimsSummary',  claimsPayload ? JSON.stringify(claimsPayload) : '');
    formData.append('paymentsSummary', '');

    // ── Fetch actual document files and attach ──
    const BASE_URL = environment.apiUrl.replace('/api', '');
    for (const doc of uniqueDocs) {
      let fileUrl = doc.fileUrl || doc.url || '';
      if (fileUrl && !fileUrl.startsWith('http') && !fileUrl.startsWith('data:')) {
        fileUrl = fileUrl.startsWith('/') ? `${BASE_URL}${fileUrl}` : `${BASE_URL}/${fileUrl}`;
      }
      if (!fileUrl) continue;
      
      try {
        const resp = await fetch(fileUrl);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const blob = await resp.blob();
        const fileName = doc.fileName || doc.documentType || 'document';
        formData.append('files', blob, fileName);
        console.log(`[AI Analysis] Attached file blob: ${fileName}`);
      } catch (err: any) {
        console.warn(`[AI Analysis] Could not fetch doc ${doc.fileName}:`, err.message);
      }
    }

    this.agentService.sendFilesToAIAnalyser(formData).subscribe({
      next: (res) => {
        console.log('✅ [AI Analysis] Raw response from Python:', res);
        const aiResponse = res.geminiResponse || res;
        console.log('📊 [AI Analysis] Parsed AI response:', aiResponse);
        console.log('📈 [AI Analysis] Risk factors:', aiResponse.riskFactors);
        this.analysisResult.set(aiResponse);
        this.isAnalyzing.set(false);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('❌ [AI Analysis] Failed:', err);
        this.analysisError.set('Failed to generate AI analysis. Please try again.');
        this.isAnalyzing.set(false);
      }
    });

  }

  private renderCharts(result: any) {
    // Destroy old instances
    if (this.barChartInstance) { this.barChartInstance.destroy(); this.barChartInstance = null; }

    const factors = result.riskFactors || [];
    if (!factors.length) {
      console.warn('[AI Charts] No riskFactors in result — charts will not render');
      return;
    }

    const labels   = factors.map((f: any) => f.factor);
    const data     = factors.map((f: any) => Number(f.riskPercentage) || 0);
    const bgColors = factors.map((f: any) => {
      const c = (f.riskLevelColor || '').toLowerCase();
      if (c === 'red')    return 'rgba(239, 68, 68, 0.8)';
      if (c === 'yellow') return 'rgba(234, 179, 8, 0.8)';
      return 'rgba(34, 197, 94, 0.8)';
    });

    console.log('[AI Charts] Rendering bar chart with labels:', labels, 'data:', data);

    // ── Bar Chart ────────────────────────────────────────────────────────────
    const barCanvas = this.riskBarChartRef?.nativeElement;
    if (barCanvas) {
      this.barChartInstance = new Chart(barCanvas, {
        type: 'bar',
        data: {
          labels,
          datasets: [{
            label: 'Risk %',
            data,
            backgroundColor: bgColors,
            borderRadius: 6,
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: { y: { beginAtZero: true, max: 100 } }
        }
      });
      console.log('[AI Charts] Bar chart rendered ✅');
    } else {
      console.warn('[AI Charts] riskBarChartRef not available');
    }
  }

  downloadPdf() {
    const result = this.analysisResult();
    if (!result) return;

    const element = document.getElementById('analysis-report-content');
    if (!element) {
      console.error('Analysis content element not found');
      return;
    }

    // Temporarily remove height restrictions to capture full content
    const originalStyles = element.style.cssText;
    element.style.height = 'auto';
    element.style.overflow = 'visible';
    element.style.maxHeight = 'none';

    htmlToImage.toCanvas(element, { 
      backgroundColor: '#ffffff',
      filter: (node: any) => {
        if (node.tagName === 'LINK' && node.href && (node.href.includes('fonts.googleapis') || node.href.includes('fonts.gstatic'))) {
          return false;
        }
        if (node.tagName === 'STYLE' && node.textContent && node.textContent.includes('@import')) {
          // Optional: skip styles importing fonts if needed, but link filters usually suffice
        }
        return true;
      }
    }).then((canvas: HTMLCanvasElement) => {
      // Restore styles
      element.style.cssText = originalStyles;

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');
      const imgWidth = 210; 
      const pageHeight = 295; 
      const imgHeight = (canvas.height * imgWidth) / canvas.width;
      let heightLeft = imgHeight;
      let position = 0;

      pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
      heightLeft -= pageHeight;

      while (heightLeft >= 0) {
        pdf.addPage();
        position = heightLeft - imgHeight;
        pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
        heightLeft -= pageHeight;
      }
      pdf.save(`AI_Analysis_Report_${result.overallRiskLevel || 'Report'}.pdf`);
    });
  }

  generatePdfBase64(): Promise<string> {
    return new Promise((resolve, reject) => {
      const result = this.analysisResult();
      if (!result) { reject('No analysis result'); return; }

      const element = document.getElementById('analysis-report-content');
      if (!element) { reject('Analysis content element not found'); return; }

      const originalStyles = element.style.cssText;
      element.style.height = 'auto';
      element.style.overflow = 'visible';
      element.style.maxHeight = 'none';

      htmlToImage.toCanvas(element, { 
        backgroundColor: '#ffffff',
        filter: (node: any) => {
          if (node.tagName === 'LINK' && node.href && (node.href.includes('fonts.googleapis') || node.href.includes('fonts.gstatic'))) {
            return false;
          }
          return true;
        }
      }).then((canvas: HTMLCanvasElement) => {
        element.style.cssText = originalStyles;

        const imgData = canvas.toDataURL('image/png');
        const pdf = new jsPDF('p', 'mm', 'a4');
        const imgWidth = 210; 
        const pageHeight = 295; 
        const imgHeight = (canvas.height * imgWidth) / canvas.width;
        let heightLeft = imgHeight;
        let position = 0;

        pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
        heightLeft -= pageHeight;

        while (heightLeft >= 0) {
          pdf.addPage();
          position = heightLeft - imgHeight;
          pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
          heightLeft -= pageHeight;
        }
        
        const base64 = pdf.output('datauristring');
        resolve(base64);
      }).catch(reject);
    });
  }

  getBadgeClass(color: string): string {
    const c = (color || '').toLowerCase();
    if (c === 'red')    return 'bg-rose-100 text-rose-700 border-rose-200';
    if (c === 'yellow') return 'bg-yellow-100 text-yellow-700 border-yellow-200';
    return 'bg-emerald-100 text-emerald-700 border-emerald-200';
  }
}
