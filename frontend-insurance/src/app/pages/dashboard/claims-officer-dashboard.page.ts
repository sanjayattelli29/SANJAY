import { Component, signal, inject, OnInit, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { AdminService } from '../../services/admin.service';
import { jsPDF } from 'jspdf';
import 'jspdf-autotable';
import { UserOptions } from 'jspdf-autotable';
import { NotificationPanelComponent } from '../../components/notification-panel/notification-panel.component';
import { HttpClient } from '@angular/common/http';
import { LocationMapComponent } from '../../components/location-map/location-map.component';
import { environment } from '../../../environments/environment';
import { n8nWebhooks } from '../../../environments/n8n/n8n';

// n8n webhook kept for reference (Vertex AI is the active pipeline)
const N8N_WEBHOOK_URL = n8nWebhooks.claimAiInsights;

// Rich AI insights response shape from Vertex AI Python backend
interface ClaimAIInsightsResponse {
    authenticityScore: number;
    overallRiskLevel: 'LOW' | 'MEDIUM' | 'HIGH';
    aiOpinion: string;
    claimVerificationSummary: string;
    documentAuthenticityReport: string;
    medicalReportAnalysis: string;
    financialValidationReport: string;
    policyEligibilityCheck: string;
    customerRiskProfile: string;
    nomineeValidation: string;
    fraudSignals: string[];
    settlementRecommendation: string;
    agentRecommendations: string[];
    suggestedAmountMin: number;
    suggestedAmountMax: number;
    riskFactors: {
        factor: string;
        value: string;
        riskPercentage: number;
        riskLevelColor: 'red' | 'yellow' | 'green';
    }[];
    meta?: any;
}

// Vertex AI Python backend — move to environment.ts for production
const VERTEX_AI_CLAIM_URL = `${environment.pythonAiUrl ?? 'http://localhost:8000'}/analyze-claim`;

// Register all Chart.js modules
Chart.register(...registerables);

@Component({
    selector: 'app-claims-officer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, NotificationPanelComponent, LocationMapComponent],
    templateUrl: './claims-officer-dashboard.page.html'
})
export class ClaimsOfficerDashboardPage implements OnInit {

    // ─── Services ──────────────────────────────────────────────────────────────
    private authService  = inject(AuthService);
    private claimService = inject(ClaimService);
    private policyService = inject(PolicyService);
    private adminService  = inject(AdminService);
    private http = inject(HttpClient);

    // ─── Template refs ─────────────────────────────────────────────────────────
    /** The main scrollable content div — used to scroll-to-top on section change */
    @ViewChild('mainScroll') mainScrollRef!: ElementRef<HTMLDivElement>;
    /** The AI section anchor — scrolled into view after AI results load */
    @ViewChild('aiSection') aiSectionRef!: ElementRef<HTMLDivElement>;

    // ─── Auth ───────────────────────────────────────────────────────────────────
    user = this.authService.getUser();

    // ─── Core signals ───────────────────────────────────────────────────────────
    myRequests    = signal<any[]>([]);
    isLoading     = signal(false);
    config        = signal<any>(null);
    activeSection = signal('dashboard');
    sidebarOpen   = signal(false);
    profileDropdownOpen = signal(false);

    // ─── Payment signals ────────────────────────────────────────────────────────
    unifiedPayments  = signal<any[]>([]);
    selectedPayment  = signal<any | null>(null);
    showInvoiceModal = signal(false);

    // ─── Computed stats ─────────────────────────────────────────────────────────
    stats = computed(() => {
        const requests = this.myRequests();
        return {
            total:    requests.length,
            pending:  requests.filter(r => r.status === 'PendingAssessment' || r.status === 'Assigned').length,
            approved: requests.filter(r => r.status === 'Approved').length,
            rejected: requests.filter(r => r.status === 'Rejected').length
        };
    });

    // ─── Audit page state (replaces review modal) ───────────────────────────────
    selectedClaim = signal<any | null>(null);

    reviewForm = {
        status: 'Approved',
        remarks: '',
        approvedAmount: 0
    };

    // ─── History detail modal ───────────────────────────────────────────────────
    showHistoryDetailModal = signal(false);
    selectedHistoryClaim   = signal<any | null>(null);

    // ─── AI insights state ──────────────────────────────────────────────────────
    isAILoading = signal(false);
    aiInsights  = signal<ClaimAIInsightsResponse | null>(null);
    aiError     = signal<string | null>(null);

    // ─── Chart instances ────────────────────────────────────────────────────────
    private charts: Chart[] = [];

    // ─── Lifecycle ──────────────────────────────────────────────────────────────
    ngOnInit() {
        this.loadRequests();
        this.loadConfig();
    }

    // ─── Config ─────────────────────────────────────────────────────────────────
    loadConfig() {
        this.policyService.getConfiguration().subscribe({
            next:  (data) => this.config.set(data),
            error: (err)  => console.error('Failed to load config', err)
        });
    }

    // ─── Claims ─────────────────────────────────────────────────────────────────
    loadRequests() {
        this.isLoading.set(true);
        this.claimService.getOfficerRequests().subscribe({
            next: (data) => {
                this.myRequests.set(data);
                this.isLoading.set(false);
                this.initCharts();
            },
            error: (err) => {
                console.error('Failed to load requests', err);
                this.isLoading.set(false);
            }
        });
    }

    // ─── Navigation ─────────────────────────────────────────────────────────────
    setSection(section: string) {
        this.activeSection.set(section);
        this.scrollToTop();

        if (section === 'dashboard') {
            this.initCharts();
        } else if (section === 'payments') {
            this.destroyCharts();
            if (this.unifiedPayments().length === 0) {
                this.loadUnifiedPayments();
            }
        } else {
            this.destroyCharts();
        }
    }

    /**
     * Opens the full-page audit view for a claim.
     * Replaces the old openReviewModal() popup approach.
     * Parses applicationDataJson, sets activeSection to 'audit',
     * and scrolls back to top so the officer starts reading from the top.
     */
    openAuditPage(claim: any) {
        const details = this.parseApplicationData(claim);

        this.selectedClaim.set({ ...claim, fullDetails: details });
        this.reviewForm.status        = 'Approved';
        this.reviewForm.remarks       = '';
        this.reviewForm.approvedAmount = claim.requestedAmount || 0;

        // Reset AI state for fresh audit
        this.aiInsights.set(null);
        this.aiError.set(null);
        this.isAILoading.set(false);

        this.activeSection.set('audit');
        this.scrollToTop();
    }

    toggleProfileDropdown() {
        this.profileDropdownOpen.update(v => !v);
    }

    logout() {
        this.authService.logout();
    }

    // ─── Charts ─────────────────────────────────────────────────────────────────
    private initCharts() {
        if (this.activeSection() !== 'dashboard') return;
        setTimeout(() => {
            this.destroyCharts();
            this.createSettlementChart();
            this.createClaimStatusChart();
            this.createPriorityChart();
            this.createDistributionChart();
        }, 500);
    }

    private destroyCharts() {
        this.charts.forEach(c => c.destroy());
        this.charts = [];
    }

    private createSettlementChart() {
        const canvas = document.getElementById('settlementChart') as HTMLCanvasElement;
        if (!canvas) return;

        const approved = this.myRequests().filter(r => r.status === 'Approved');
        const labels = approved.length > 0
            ? approved.map(r => new Date(r.processedAt || Date.now()).toLocaleDateString())
            : ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'];
        const data = approved.length > 0
            ? approved.map(r => r.approvedAmount)
            : [30000, 45000, 32000, 50000, 48000, 60000];

        this.charts.push(new Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Settlement Velocity',
                    data,
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    fill: true,
                    tension: 0.4,
                    borderWidth: 3,
                    pointBackgroundColor: '#10b981',
                    pointBorderColor: '#fff',
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#0f172a',
                        titleFont: { family: "'Sora', sans-serif", size: 12 },
                        bodyFont:  { family: "'Sora', sans-serif", size: 12 },
                        padding: 12,
                        cornerRadius: 12
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        ticks: { font: { family: "'Sora', sans-serif", size: 10 } }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: "'Sora', sans-serif", size: 10 } }
                    }
                }
            }
        }));
    }

    private createClaimStatusChart() {
        const canvas = document.getElementById('claimStatusChart') as HTMLCanvasElement;
        if (!canvas) return;

        const stats = this.stats();
        const dataValues = [stats.approved, stats.rejected, stats.pending];
        const finalData  = dataValues.some(v => v > 0) ? dataValues : [12, 5, 8];

        this.charts.push(new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: ['Settled', 'Declined', 'Audit'],
                datasets: [{
                    data: finalData,
                    backgroundColor: ['#10b981', '#ef4444', '#f59e0b'],
                    borderWidth: 0,
                    hoverOffset: 15
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '75%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            boxWidth: 8,
                            usePointStyle: true,
                            font: { family: "'Sora', sans-serif", size: 10, weight: 'bold' },
                            padding: 20
                        }
                    }
                }
            }
        }));
    }

    private createPriorityChart() {
        const canvas = document.getElementById('priorityChart') as HTMLCanvasElement;
        if (!canvas) return;

        this.charts.push(new Chart(canvas, {
            type: 'bar',
            data: {
                labels: ['Critical', 'High', 'Medium', 'Low'],
                datasets: [{
                    label: 'Claim Weight',
                    data: [5, 12, 18, 10],
                    backgroundColor: ['#ef4444', '#f97316', '#3b82f6', '#10b981'],
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { font: { family: "'Sora', sans-serif" } } },
                    x: { ticks: { font: { family: "'Sora', sans-serif" } } }
                }
            }
        }));
    }

    private createDistributionChart() {
        const canvas = document.getElementById('distributionChart') as HTMLCanvasElement;
        if (!canvas) return;

        this.charts.push(new Chart(canvas, {
            type: 'bar',
            data: {
                labels: ['Medical', 'Accident', 'Life', 'Critical'],
                datasets: [{
                    label: 'Volume',
                    data: [45, 25, 15, 30],
                    backgroundColor: '#6366f1',
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { font: { family: "'Sora', sans-serif" } } },
                    x: { ticks: { font: { family: "'Sora', sans-serif" } } }
                }
            }
        }));
    }

    // ─── Application Data Parser ─────────────────────────────────────────────────
    /**
     * Parses policy.applicationDataJson and normalises all keys to camelCase.
     * Extracted to avoid duplication between openAuditPage() and viewHistoryDetail().
     */
    private parseApplicationData(claim: any): any {
        if (!claim.policy?.applicationDataJson) return null;

        try {
            const raw = JSON.parse(claim.policy.applicationDataJson);

            const normalize = (obj: any): any => {
                if (!obj) return null;
                const out: any = {};
                Object.keys(obj).forEach(key => {
                    out[key.charAt(0).toLowerCase() + key.slice(1)] = obj[key];
                });
                return out;
            };

            const details = normalize(raw);
            if (!details) return null;

            details.applicant    = normalize(details.applicant || details.primaryApplicant || raw.Applicant || raw.PrimaryApplicant);
            details.nominee      = normalize(details.nominee || raw.Nominee);
            details.medicalProfile = normalize(details.medicalProfile || raw.MedicalProfile);
            details.lifestyle    = normalize(details.lifestyle || raw.Lifestyle);
            details.incident     = normalize(details.incident || details.incidentVerification || raw.Incident || raw.IncidentVerification);
            details.familyMembers = details.familyMembers || raw.FamilyMembers;
            details.paymentMode  = details.paymentMode || raw.PaymentMode;

            // Resolve coverage from config if available
            if (this.config()) {
                const cat  = this.config().policyCategories?.find((c: any) => c.categoryId === claim.policy?.policyCategory);
                const tier = cat?.tiers?.find((t: any) => t.tierId === claim.policy?.tierId);
                claim.totalCoverage    = tier?.baseCoverageAmount || (claim.policy?.totalCoverageAmount || 0);
                claim.remainingCoverage = claim.policy?.remainingCoverageAmount || 0;
            }

            return details;
        } catch (e) {
            console.error('Failed to parse policy application data', e);
            return null;
        }
    }

    // ─── Submit Review ───────────────────────────────────────────────────────────
    submitReview(status: string) {
        const claimId = this.selectedClaim()?.id;
        if (!claimId) return;

        this.isLoading.set(true);
        this.claimService.reviewClaim(
            claimId,
            status,
            this.reviewForm.remarks,
            this.reviewForm.approvedAmount
        ).subscribe({
            next: () => {
                this.isLoading.set(false);
                // Go back to Audit Queue after submission
                this.setSection('pending');
                this.loadRequests();
            },
            error: (err) => {
                console.error('Failed to submit review', err);
                this.isLoading.set(false);
            }
        });
    }

    // ─── AI Insights ─────────────────────────────────────────────────────────────
    /**
     * Fetches claim documents as Blobs, builds structured JSON payloads,
     * POSTs to Vertex AI Python backend, and sets aiInsights() inline
     * on the audit page (no modal). After results load, auto-scrolls
     * the AI section into view.
     */
    async getAIInsights(claim: any): Promise<void> {
        if (!claim?.documents?.length) return;

        this.isAILoading.set(true);
        this.aiInsights.set(null);
        this.aiError.set(null);

        try {
            const BASE_URL  = environment.apiUrl.replace('/api', '');
            const formData  = new FormData();

            // Step 1: Fetch all document files as Blobs
            for (const doc of claim.documents) {
                let fileUrl: string = doc.fileUrl || doc.url || doc.documentUrl || doc.filePath || '';
                if (fileUrl && !fileUrl.startsWith('http') && !fileUrl.startsWith('data:')) {
                    fileUrl = fileUrl.startsWith('/') ? `${BASE_URL}${fileUrl}` : `${BASE_URL}/${fileUrl}`;
                }
                if (!fileUrl) continue;

                try {
                    const resp = await fetch(fileUrl);
                    if (!resp.ok) continue;
                    const blob     = await resp.blob();
                    const fileName = doc.fileName || doc.documentType || 'document';
                    formData.append('files', blob, fileName);
                } catch (fetchErr) {
                    console.warn(`Could not fetch doc ${doc.fileName}:`, fetchErr);
                }
            }

            // Step 2: Claim payload
            const claimPayload = {
                claimId:                 claim.id ?? '',
                incidentType:            claim.incidentType ?? '',
                incidentDate:            claim.incidentDate ?? '',
                incidentTime:            claim.incidentTime ?? '',
                accidentCause:           claim.accidentCause ?? '',
                incidentLocation:        claim.incidentLocation ?? '',
                policeCaseFiled:         claim.policeCaseFiled ?? false,
                firNumber:               claim.firNumber ?? '',
                description:             claim.description ?? '',
                hospitalName:            claim.hospitalName ?? '',
                hospitalizationRequired: claim.hospitalizationRequired ?? false,
                admissionDate:           claim.admissionDate ?? '',
                dischargeDate:           claim.dischargeDate ?? '',
                injuryType:              claim.injuryType ?? '',
                bodyPartInjured:         claim.bodyPartInjured ?? '',
                requestedAmount:         claim.requestedAmount ?? 0,
                hospitalBill:            claim.hospitalBill ?? 0,
                medicines:               claim.medicines ?? 0,
                otherExpenses:           claim.otherExpenses ?? 0,
                estimatedMedicalCost:    claim.estimatedMedicalCost ?? 0,
                remainingCoverage:       claim.policy?.remainingCoverageAmount ?? 0,
                totalCoverage:           claim.policy?.totalCoverageAmount ?? 0,
            };

            // Step 3: Customer / policy / nominee payloads
            const details  = claim.fullDetails || {};
            const applicant = details.applicant || {};
            const nominee   = details.nominee || {};

            const customerPayload = {
                fullName:              applicant.fullName ?? claim.policy?.customerEmail ?? '',
                email:                 claim.policy?.customerEmail ?? '',
                age:                   applicant.age ?? '',
                profession:            applicant.profession ?? '',
                annualIncome:          applicant.annualIncome ?? '',
                smokingHabit:          details.lifestyle?.smokingHabit ?? '',
                alcoholHabit:          details.lifestyle?.alcoholHabit ?? '',
                vehicleType:           details.lifestyle?.vehicleType ?? '',
                travelDistancePerMonth: details.lifestyle?.travelDistancePerMonth ?? '',
            };

            const policyPayload = {
                policyNumber:           claim.policy?.policyNumber ?? '',
                policyCategory:         claim.policy?.policyCategory ?? '',
                tierId:                 claim.policy?.tierId ?? '',
                totalCoverageAmount:    claim.policy?.totalCoverageAmount ?? 0,
                remainingCoverageAmount: claim.policy?.remainingCoverageAmount ?? 0,
                calculatedPremium:      claim.policy?.calculatedPremium ?? 0,
                startDate:              claim.policy?.startDate ?? '',
                endDate:                claim.policy?.endDate ?? '',
            };

            const nomineePayload = {
                fullName:      nominee.name ?? nominee.fullName ?? '',
                relationship:  nominee.relationship ?? '',
                contactNumber: nominee.phone ?? nominee.contactNumber ?? '',
            };

            // Step 4: Append JSON to FormData
            formData.append('claimData',    JSON.stringify(claimPayload));
            formData.append('customerData', JSON.stringify(customerPayload));
            formData.append('policyData',   JSON.stringify(policyPayload));
            formData.append('nomineeData',  JSON.stringify(nomineePayload));

            // Step 5: POST to Vertex AI backend
            const response = await fetch(VERTEX_AI_CLAIM_URL, {
                method: 'POST',
                body:   formData
            });

            if (!response.ok) {
                throw new Error(`Vertex AI backend error: ${response.status}`);
            }

            const result: ClaimAIInsightsResponse = await response.json();

            if ((result as any).error) {
                throw new Error(`AI analysis failed: ${(result as any).error}`);
            }

            this.aiInsights.set(result);

            // Scroll AI section into view so officer sees results
            setTimeout(() => {
                this.aiSectionRef?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 150);

        } catch (err: any) {
            console.error('Vertex AI Claim Insights error:', err);
            this.aiError.set(err?.message ?? 'Analysis failed. Please try again.');
        } finally {
            this.isAILoading.set(false);
        }
    }

    clearAIInsights(): void {
        this.aiInsights.set(null);
        this.aiError.set(null);
    }

    /** Sets the approved amount to the midpoint of the AI-suggested range */
    applyAISuggestedAmount(): void {
        const insights = this.aiInsights();
        if (!insights) return;
        this.reviewForm.approvedAmount = Math.round(
            (insights.suggestedAmountMin + insights.suggestedAmountMax) / 2
        );
    }

    // ─── Risk Score Helpers ──────────────────────────────────────────────────────
    getRiskScoreColor(score: number): string {
        if (score <= 30) return 'text-emerald-600';
        if (score <= 60) return 'text-amber-500';
        return 'text-rose-600';
    }

    getRiskScoreStroke(score: number): string {
        if (score <= 30) return '#10b981';
        if (score <= 60) return '#f59e0b';
        return '#ef4444';
    }

    getRiskLabel(score: number): string {
        if (score <= 30) return 'Low';
        if (score <= 60) return 'Med';
        return 'High';
    }

    // ─── History ─────────────────────────────────────────────────────────────────
    viewHistoryDetail(claim: any) {
        const details = this.parseApplicationData(claim);

        // Enrich with financial data for the history view
        claim.totalCoverage    = claim.policy?.totalCoverageAmount || 0;
        claim.remainingCoverage = claim.policy?.remainingCoverageAmount || 0;

        this.selectedHistoryClaim.set({ ...claim, fullDetails: details });
        this.showHistoryDetailModal.set(true);
    }

    // ─── Payments ────────────────────────────────────────────────────────────────
    loadUnifiedPayments() {
        this.isLoading.set(true);
        this.adminService.getUnifiedPayments().subscribe({
            next:  (data) => { this.unifiedPayments.set(data); this.isLoading.set(false); },
            error: (err)  => { console.error('Failed to load unified payments', err); this.isLoading.set(false); }
        });
    }

    openInvoiceModal(payment: any) {
        this.selectedPayment.set(payment);
        this.showInvoiceModal.set(true);
    }

    // ─── Invoice PDF ─────────────────────────────────────────────────────────────
    generateInvoicePDF(payment: any) {
        const doc            = new jsPDF();
        const primaryColor   = [242, 101, 34];  // AcciSure brand orange #F26522
        const secondaryColor = [15, 23, 42];    // #0f172a
        const accentColor    = [251, 146, 60];  // #fb923c

        // 1. Custom diamond logo
        const drawDiamond = (x: number, y: number, size: number, color: number[]) => {
            doc.setFillColor(color[0], color[1], color[2]);
            doc.setDrawColor(color[0], color[1], color[2]);
            doc.lines(
                [[size, size], [size, -size], [-size, -size]],
                x, y - size, [1, 1], 'FD', true
            );
        };
        drawDiamond(25, 20, 4, primaryColor);
        drawDiamond(29, 24, 4, accentColor);

        // 2. Header branding
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(24);
        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.text('AcciSure', 38, 23);
        doc.setFontSize(8);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(100, 116, 139);
        doc.text('PROTECT TODAY. BRIGHTER TOMORROW.', 38, 28);

        doc.setFontSize(28);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
        doc.text('INVOICE', 190, 25, { align: 'right' });

        doc.setDrawColor(primaryColor[0], primaryColor[1], primaryColor[2]);
        doc.setLineWidth(1.5);
        doc.line(20, 35, 190, 35);

        // 3. Invoice info
        doc.setFontSize(10);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.text('BILL TO:', 20, 50);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(71, 85, 105);
        doc.text(`${payment.customerEmail || 'Valued Customer'}`, 20, 56);
        doc.text('India', 20, 61);

        doc.setFont('helvetica', 'bold');
        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.text('INVOICE DETAILS:', 130, 50);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(71, 85, 105);
        doc.text('Invoice ID:',       130, 56);
        doc.text('Date:',             130, 61);
        doc.text('Status:',           130, 66);
        doc.text('Transaction ID:',   130, 72);

        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.setFont('helvetica', 'bold');
        doc.text(`INV-${payment.transactionId?.substring(0, 8).toUpperCase() || 'N/A'}`, 160, 56);
        doc.text(`${new Date().toLocaleDateString()}`, 160, 61);
        doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
        doc.text('PAID', 160, 66);

        doc.setTextColor(71, 85, 105);
        doc.setFontSize(8);
        doc.setFont('helvetica', 'normal');
        doc.text(`${payment.transactionId || 'Pending'}`, 130, 77);
        doc.setFontSize(10);

        // 4. Main table
        (doc as any).autoTable({
            startY: 85,
            head: [['DESCRIPTION', 'POLICY DETAILS', 'AMOUNT']],
            body: [
                ['Insurance Premium',  `${payment.policyCategory} - ${payment.tierId}`,  `INR ${payment.premiumAmount.toLocaleString()}`],
                ['Plan Coverage',      'Individual / Family Group',                       `INR ${payment.totalCoverage.toLocaleString()}`],
                ['Current Status',     'Active / Distributed',                            payment.paymentMode?.toUpperCase() || 'DIGITAL'],
                ['Agent Commission',   'Processing Fee Included',                         'INCL.'],
            ],
            theme: 'grid',
            headStyles:  { fillColor: [15, 23, 42], textColor: [255, 255, 255], fontSize: 10, fontStyle: 'bold', halign: 'center', cellPadding: 5 },
            bodyStyles:  { fontSize: 9, cellPadding: 4, textColor: [51, 65, 85] },
            columnStyles: {
                0: { fontStyle: 'bold', cellWidth: 50 },
                2: { halign: 'right', fontStyle: 'bold', textColor: [15, 23, 42] }
            },
            alternateRowStyles: { fillColor: [248, 250, 252] }
        });

        const tableEndY = (doc as any).lastAutoTable.finalY + 15;

        // 5. Summary total
        doc.setDrawColor(226, 232, 240);
        doc.setLineWidth(0.5);
        doc.line(120, tableEndY, 190, tableEndY);

        doc.setFontSize(12);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.text('Total Amount Paid:', 120, tableEndY + 10);
        doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
        doc.text(
            `INR ${payment.paidAmount?.toLocaleString() || payment.premiumAmount.toLocaleString()}`,
            190, tableEndY + 10, { align: 'right' }
        );

        // 6. Terms
        const footerY = 230;
        doc.setDrawColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.setLineWidth(0.5);
        doc.line(20, footerY, 190, footerY);

        doc.setFontSize(9);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(secondaryColor[0], secondaryColor[1], secondaryColor[2]);
        doc.text('TERMS AND CONDITIONS:', 20, footerY + 8);

        doc.setFont('helvetica', 'normal');
        doc.setFontSize(7.5);
        doc.setTextColor(100, 116, 139);
        const terms = [
            '1. This invoice is a valid legal document for premium payment acknowledgement.',
            '2. All insurance coverage is subject to the successful realization of the premium amount.',
            '3. Please refer to your policy document for detailed coverage, exclusions, and claim procedures.',
            '4. AcciSure Insurance is not liable for any delays caused by third-party payment gateways.',
            '5. For any disputes or support, please contact us at support@accisure.com quoting your Transaction ID.'
        ];
        terms.forEach((term, i) => doc.text(term, 20, footerY + 14 + i * 4));

        // 7. Footer
        doc.setFontSize(8);
        doc.setFont('helvetica', 'italic');
        doc.setTextColor(148, 163, 184);
        doc.text('This is a computer-generated invoice and requires no physical signature.', 105, 275, { align: 'center' });

        doc.setFontSize(10);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(primaryColor[0], primaryColor[1], primaryColor[2]);
        doc.text('AcciSure Insurance - Protecting Your Brighter Tomorrow', 105, 285, { align: 'center' });

        doc.save(`AcciSure_Invoice_${payment.transactionId?.substring(0, 8) || 'Doc'}.pdf`);
    }

    // ─── Scroll helpers ──────────────────────────────────────────────────────────
    private scrollToTop() {
        setTimeout(() => {
            this.mainScrollRef?.nativeElement?.scrollTo({ top: 0, behavior: 'smooth' });
        }, 50);
    }
}