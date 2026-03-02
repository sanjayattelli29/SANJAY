import { Component, signal, inject, OnInit, computed } from '@angular/core';
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

Chart.register(...registerables);

@Component({
    selector: 'app-claims-officer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, NotificationPanelComponent],
    templateUrl: './claims-officer-dashboard.page.html'
})
export class ClaimsOfficerDashboardPage implements OnInit {
    private authService = inject(AuthService);
    private claimService = inject(ClaimService);
    private policyService = inject(PolicyService);
    private adminService = inject(AdminService);

    user = this.authService.getUser();
    myRequests = signal<any[]>([]);
    isLoading = signal(false);
    config = signal<any>(null);
    activeSection = signal('dashboard');

    // Payments Dashboard signals
    unifiedPayments = signal<any[]>([]);
    selectedPayment = signal<any | null>(null);
    showInvoiceModal = signal(false);

    // Stats
    stats = computed(() => {
        const requests = this.myRequests();
        return {
            total: requests.length,
            pending: requests.filter(r => r.status === 'PendingAssessment' || r.status === 'Assigned').length,
            approved: requests.filter(r => r.status === 'Approved').length,
            rejected: requests.filter(r => r.status === 'Rejected').length
        };
    });

    // Review Modal State
    selectedClaim = signal<any | null>(null);
    showReviewModal = signal(false);
    reviewForm = {
        status: 'Approved',
        remarks: '',
        approvedAmount: 0
    };

    // History Detail State
    showHistoryDetailModal = signal(false);
    selectedHistoryClaim = signal<any | null>(null);

    // Chart instances
    private charts: Chart[] = [];

    ngOnInit() {
        this.loadRequests();
        this.loadConfig();
    }

    loadConfig() {
        this.policyService.getConfiguration().subscribe({
            next: (data) => this.config.set(data),
            error: (err) => console.error('Failed to load config', err)
        });
    }

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

    private initCharts() {
        if (this.activeSection() !== 'dashboard') return;
        setTimeout(() => {
            this.destroyCharts();
            this.createSettlementChart();
            this.createClaimStatusChart();
        }, 100);
    }

    private destroyCharts() {
        this.charts.forEach(c => c.destroy());
        this.charts = [];
    }

    private createSettlementChart() {
        const canvas = document.getElementById('settlementChart') as HTMLCanvasElement;
        if (!canvas) return;

        const approved = this.myRequests().filter(r => r.status === 'Approved');
        const labels = approved.map(r => new Date(r.processedDate).toLocaleDateString());
        const data = approved.map(r => r.approvedAmount);

        this.charts.push(new Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Settlement Amount',
                    data,
                    borderColor: '#10b981',
                    backgroundColor: '#10b98120',
                    fill: true,
                    tension: 0.3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true },
                    x: { grid: { display: false } }
                }
            }
        }));
    }

    private createClaimStatusChart() {
        const canvas = document.getElementById('claimStatusChart') as HTMLCanvasElement;
        if (!canvas) return;

        const stats = this.stats();
        this.charts.push(new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: ['Approved', 'Rejected', 'Pending'],
                datasets: [{
                    data: [stats.approved, stats.rejected, stats.pending],
                    backgroundColor: ['#10b981', '#ef4444', '#f97316']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '70%',
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }
                }
            }
        }));
    }

    openReviewModal(claim: any) {
        // Parse and normalize JSON data from the policy application
        let details: any = null;
        if (claim.policy?.applicationDataJson) {
            try {
                const raw = JSON.parse(claim.policy.applicationDataJson);
                const normalize = (obj: any) => {
                    if (!obj) return null;
                    const normalized: any = {};
                    Object.keys(obj).forEach(key => {
                        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                        normalized[normalizedKey] = obj[key];
                    });
                    return normalized;
                };

                details = normalize(raw);
                if (details) {
                    details.applicant = normalize(details.applicant || details.primaryApplicant || raw.Applicant || raw.PrimaryApplicant);
                    details.nominee = normalize(details.nominee || raw.Nominee);
                    details.medicalProfile = normalize(details.medicalProfile || raw.MedicalProfile);
                    details.lifestyle = normalize(details.lifestyle || raw.Lifestyle);
                    details.incident = normalize(details.incident || details.incidentVerification || raw.Incident || raw.IncidentVerification);
                    details.familyMembers = details.familyMembers || raw.FamilyMembers;
                    details.paymentMode = details.paymentMode || raw.PaymentMode;
                }

                // Calculate Sum Insured from config
                if (this.config()) {
                    const cat = this.config().policyCategories.find((c: any) => c.categoryId === claim.policy?.policyCategory);
                    const tier = cat?.tiers.find((t: any) => t.tierId === claim.policy?.tierId);
                    claim.totalCoverage = tier?.baseCoverageAmount || (claim.policy?.totalCoverageAmount || 0);
                    claim.remainingCoverage = claim.policy?.remainingCoverageAmount || 0;
                }
            } catch (e) {
                console.error('Failed to parse policy data', e);
            }
        }

        this.selectedClaim.set({ ...claim, fullDetails: details });
        this.showReviewModal.set(true);
        this.reviewForm.status = 'Approved';
        this.reviewForm.remarks = '';
        this.reviewForm.approvedAmount = claim.requestedAmount || 0;
    }

    setSection(section: string) {
        this.activeSection.set(section);
        if (section === 'dashboard') {
            this.initCharts();
        } else if (section === 'payments') {
            this.destroyCharts();
            this.loadUnifiedPayments();
        } else {
            this.destroyCharts();
        }
    }

    loadUnifiedPayments() {
        this.isLoading.set(true);
        this.adminService.getUnifiedPayments().subscribe({
            next: (data) => {
                this.unifiedPayments.set(data);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error('Failed to load unified payments', err);
                this.isLoading.set(false);
            }
        });
    }

    openInvoiceModal(payment: any) {
        this.selectedPayment.set(payment);
        this.showInvoiceModal.set(true);
    }

    generateInvoicePDF(pay: any) {
        const doc = new jsPDF() as any;

        // Header Section
        doc.setFillColor(30, 41, 59); // Brand Navy
        doc.rect(0, 0, 210, 40, 'F');

        doc.setTextColor(255, 255, 255);
        doc.setFontSize(24);
        doc.setFont('helvetica', 'bold');
        doc.text('PREMIUM INVOICE', 20, 25);

        doc.setFontSize(10);
        doc.setTextColor(249, 115, 22); // Brand Orange
        doc.text('INSURANCE PLATFORM - ENTERPRISE AUDIT', 20, 32);

        // Body Content
        doc.setTextColor(30, 41, 59);
        doc.setFontSize(12);
        doc.text('BILL TO:', 20, 55);
        doc.setFontSize(10);
        doc.setFont('helvetica', 'normal');
        doc.text(`Customer: ${pay.customerEmail}`, 20, 62);
        doc.text(`Transaction: ${pay.transactionId || 'Awaiting Sync'}`, 20, 68);

        doc.setFont('helvetica', 'bold');
        doc.text('INVOICE DATE:', 140, 55);
        doc.setFont('helvetica', 'normal');
        doc.text(new Date(pay.nextPaymentDate).toLocaleDateString(), 140, 62);

        // Table Data
        const tableBody = [
            ['Policy Category', pay.policyCategory],
            ['Plan Tier', pay.tierId],
            ['Agent Email', pay.agentEmail || 'Direct'],
            ['Claims Officer', pay.claimsOfficerEmail || 'Unassigned'],
            ['Total Coverage', `INR ${pay.totalCoverage.toLocaleString()}`],
            ['Active Coverage', `INR ${pay.currentCoverage.toLocaleString()}`],
            ['Premium Amount', `INR ${pay.premiumAmount.toLocaleString()}`],
            ['Total Paid To Date', `INR ${pay.paidAmount.toLocaleString()}`]
        ];

        (doc as any).autoTable({
            startY: 85,
            head: [['Description', 'Detail']],
            body: tableBody,
            theme: 'grid',
            headStyles: { fillColor: [30, 41, 59], textColor: [255, 255, 255], fontStyle: 'bold' },
            styles: { fontSize: 9, cellPadding: 5 },
            columnStyles: {
                0: { cellWidth: 80, fontStyle: 'bold' },
                1: { cellWidth: 'auto' }
            }
        });

        const finalY = (doc as any).lastAutoTable.finalY + 20;

        // Total Branding
        doc.setFillColor(249, 115, 22);
        doc.rect(130, finalY - 10, 60, 20, 'F');
        doc.setTextColor(255, 255, 255);
        doc.setFontSize(10);
        doc.text('TOTAL PAID', 135, finalY);
        doc.setFontSize(14);
        doc.setFont('helvetica', 'bold');
        doc.text(`INR ${pay.paidAmount.toLocaleString()}`, 135, finalY + 7);

        // Footer
        doc.setTextColor(150, 150, 150);
        doc.setFontSize(8);
        doc.text('This is a computer-generated audit log and requires no physical signature.', 105, 280, { align: 'center' });
        doc.text('Insurance Platform Enterprise Security Protocol v2.4', 105, 285, { align: 'center' });

        doc.save(`Invoice_${pay.transactionId || 'TEMP'}.pdf`);
    }

    viewHistoryDetail(claim: any) {
        // Reuse parsing logic for details
        let details = null;
        if (claim.policy?.applicationDataJson) {
            try {
                const raw = JSON.parse(claim.policy.applicationDataJson);
                const normalize = (obj: any) => {
                    if (!obj) return null;
                    const normalized: any = {};
                    Object.keys(obj).forEach(key => {
                        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                        normalized[normalizedKey] = obj[key];
                    });
                    return normalized;
                };
                const fullDetails = normalize(raw);
                if (fullDetails) {
                    fullDetails.applicant = normalize(fullDetails.applicant || fullDetails.primaryApplicant || raw.Applicant || raw.PrimaryApplicant);
                    fullDetails.nominee = normalize(fullDetails.nominee || raw.Nominee);
                    fullDetails.medicalProfile = normalize(fullDetails.medicalProfile || raw.MedicalProfile);
                    fullDetails.lifestyle = normalize(fullDetails.lifestyle || raw.Lifestyle);
                    fullDetails.incident = normalize(fullDetails.incident || fullDetails.incidentVerification || raw.Incident || raw.IncidentVerification);
                }
                details = fullDetails;

                // Enrich with financial data for history
                claim.totalCoverage = claim.policy?.totalCoverageAmount || 0;
                claim.remainingCoverage = claim.policy?.remainingCoverageAmount || 0;
            } catch (e) { }
        }

        this.selectedHistoryClaim.set({ ...claim, fullDetails: details });
        this.showHistoryDetailModal.set(true);
    }

    submitReview(status: string) {
        const claimId = this.selectedClaim()?.id;
        if (!claimId) return;

        this.isLoading.set(true);
        this.claimService.reviewClaim(claimId, status, this.reviewForm.remarks, this.reviewForm.approvedAmount).subscribe({
            next: () => {
                this.isLoading.set(false);
                this.showReviewModal.set(false);
                this.loadRequests();
            },
            error: (err) => {
                console.error('Failed to submit review', err);
                this.isLoading.set(false);
            }
        });
    }

    logout() {
        this.authService.logout();
    }
}
