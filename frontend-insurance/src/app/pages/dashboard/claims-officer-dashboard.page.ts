import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
    selector: 'app-claims-officer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './claims-officer-dashboard.page.html'
})
export class ClaimsOfficerDashboardPage implements OnInit {
    private authService = inject(AuthService);
    private claimService = inject(ClaimService);
    private policyService = inject(PolicyService);

    user = this.authService.getUser();
    myRequests = signal<any[]>([]);
    isLoading = signal(false);
    config = signal<any>(null);
    activeSection = signal('dashboard');

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
        } else {
            this.destroyCharts();
        }
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
