import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { AgentService } from '../../services/agent.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
    selector: 'app-agent-dashboard',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './agent-dashboard.page.html'
})
export class AgentDashboardPage implements OnInit {
    private authService = inject(AuthService);
    private agentService = inject(AgentService);

    user = this.authService.getUser();
    activeSection = signal('dashboard');
    private claimService = inject(ClaimService);
    private policyService = inject(PolicyService);
    isLoading = signal(false);
    message = signal({ type: '', text: '' });

    // Data
    stats: any = {
        pendingReview: 0,
        totalCommission: 0,
        totalCoverage: 0,
        activePolicies: 0,
        uniqueCustomers: 0,
        bestCategory: '',
        bestTier: '',
        totalPremium: 0
    };
    analytics = signal<any>(null);
    policyRequests = signal<any[]>([]);
    commissionData = signal<any>({ totalCommission: 0, activePolicies: [] });
    customerClaims = signal<any[]>([]);
    myCustomers = signal<any[]>([]);

    // UI State for Modal
    showDetailModal = signal(false);
    showClaimModal = signal(false);
    showUnifiedDetail = signal(false);
    selectedUnifiedDetail = signal<any>(null);
    selectedApplication = signal<any | null>(null);
    selectedClaim = signal<any | null>(null);
    isProcessing = signal(false);

    // Chart instances
    private charts: Chart[] = [];

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.loadPolicyRequests();
        this.loadCommissionStats();
        this.loadCustomerClaims();
        this.loadMyCustomers();
        this.loadAnalytics();
    }

    loadAnalytics() {
        this.agentService.getAnalytics().subscribe({
            next: (data) => {
                this.analytics.set(data);
                this.stats.totalCoverage = data.totalCoverageProvided;
                this.stats.activePolicies = data.activePolicyCount;
                this.stats.uniqueCustomers = data.uniqueCustomerCount;
                this.stats.bestCategory = data.bestPerformingCategory;
                this.stats.bestTier = data.bestPerformingTier;
                this.stats.totalPremium = data.totalPremiumCollected;
                this.stats.totalCommission = data.totalCommissionEarned;
                this.initCharts();
            },
            error: (err) => console.error('Failed to load analytics', err)
        });
    }

    loadCustomerClaims() {
        this.claimService.getAgentClaims().subscribe({
            next: (data: any[]) => this.customerClaims.set(data),
            error: (err: any) => console.error('Failed to load customer claims', err)
        });
    }

    loadMyCustomers() {
        this.policyService.getAgentCustomers().subscribe({
            next: (data) => this.myCustomers.set(data),
            error: (err) => console.error('Failed to load my customers', err)
        });
    }

    setSection(section: string) {
        this.activeSection.set(section);
        this.message.set({ type: '', text: '' });
        if (section === 'dashboard') {
            this.initCharts();
        } else {
            this.destroyCharts();
        }
    }

    private initCharts() {
        if (this.activeSection() !== 'dashboard') return;
        setTimeout(() => {
            this.destroyCharts();
            this.createCommissionChart();
            this.createPortfolioChart();
            this.createTierChart();
            this.createPremiumTrendChart();
            this.createStatusChart();
            this.createClaimImpactChart();
        }, 100);
    }

    private destroyCharts() {
        this.charts.forEach(c => c.destroy());
        this.charts = [];
    }

    private createCommissionChart() {
        const canvas = document.getElementById('commissionChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().commissionPerformance || [];
        const labels = chartData.map((d: any) => d.month);
        const data = chartData.map((d: any) => d.value);

        this.charts.push(new Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Commission (₹)',
                    data,
                    borderColor: '#f97316',
                    backgroundColor: 'rgba(249, 115, 22, 0.1)',
                    fill: true,
                    tension: 0.4,
                    pointRadius: 4,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#f97316',
                    pointBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: { size: 10 } } },
                    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
                }
            }
        }));
    }

    private createPortfolioChart() {
        const canvas = document.getElementById('portfolioChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().portfolioMix || [];

        this.charts.push(new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: chartData.map((d: any) => d.category),
                datasets: [{
                    data: chartData.map((d: any) => d.count),
                    backgroundColor: ['#4f46e5', '#f97316', '#10b981', '#6366f1']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10, weight: 'bold' } } }
                }
            }
        }));
    }

    private createTierChart() {
        const canvas = document.getElementById('tierChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().tierBreakdown || [];

        this.charts.push(new Chart(canvas, {
            type: 'bar',
            data: {
                labels: chartData.map((d: any) => d.tier),
                datasets: [{
                    label: 'Policies',
                    data: chartData.map((d: any) => d.count),
                    backgroundColor: '#6366f1',
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: { size: 10 } } },
                    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
                }
            }
        }));
    }

    private createPremiumTrendChart() {
        const canvas = document.getElementById('premiumTrendChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().premiumTrends || [];

        this.charts.push(new Chart(canvas, {
            type: 'bar',
            data: {
                labels: chartData.map((d: any) => d.month),
                datasets: [{
                    label: 'Premium Collected (₹)',
                    data: chartData.map((d: any) => d.value),
                    backgroundColor: '#10b981',
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: { size: 10 } } },
                    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
                }
            }
        }));
    }

    private createStatusChart() {
        const canvas = document.getElementById('statusChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().policyStatusMetrics || [];

        this.charts.push(new Chart(canvas, {
            type: 'pie',
            data: {
                labels: chartData.map((d: any) => d.status),
                datasets: [{
                    data: chartData.map((d: any) => d.count),
                    backgroundColor: ['#10b981', '#f59e0b', '#ef4444', '#6366f1']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10, weight: 'bold' } } }
                }
            }
        }));
    }

    private createClaimImpactChart() {
        const canvas = document.getElementById('claimChart') as HTMLCanvasElement;
        if (!canvas || !this.analytics()) return;

        const chartData = this.analytics().claimImpact || [];

        this.charts.push(new Chart(canvas, {
            type: 'radar',
            data: {
                labels: chartData.map((d: any) => d.metric),
                datasets: [{
                    label: 'Portfolio Impact',
                    data: chartData.map((d: any) => d.value),
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.2)',
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    r: {
                        beginAtZero: true,
                        ticks: { display: false },
                        suggestedMax: Math.max(...chartData.map((d: any) => d.value)) * 1.2
                    }
                }
            }
        }));
    }

    loadPolicyRequests() {
        this.agentService.getMyRequests().subscribe({
            next: (data) => {
                this.policyRequests.set(data);
                // Fix: Pending review count should only be for 'Assigned' status
                const pending = data.filter(r => r.status === 'Assigned').length;
                this.stats.pendingReview = pending;
            },
            error: (err) => console.error('Failed to load my requests', err)
        });
    }

    loadCommissionStats() {
        this.agentService.getCommissionStats().subscribe({
            next: (data) => {
                this.commissionData.set(data);
                this.stats.totalCommission = data.totalCommission;
                this.initCharts();
            },
            error: (err) => console.error('Failed to load commission stats', err)
        });
    }

    viewDetails(application: any) {
        // Parse the JSON data stored in ApplicationDataJson
        const raw = JSON.parse(application.applicationDataJson);

        // Normalize keys (handle both PascalCase from DB and camelCase from frontend)
        const normalize = (obj: any) => {
            if (!obj) return null;
            const normalized: any = {};
            Object.keys(obj).forEach(key => {
                const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                normalized[normalizedKey] = obj[key];
            });
            return normalized;
        };

        const details = normalize(raw);
        if (details) {
            details.applicant = normalize(details.applicant || details.primaryApplicant || raw.Applicant || raw.PrimaryApplicant);
            details.nominee = normalize(details.nominee || raw.Nominee);
            details.familyMembers = details.familyMembers || raw.FamilyMembers;
            details.paymentMode = details.paymentMode || raw.PaymentMode;
        }

        this.selectedApplication.set({ ...application, fullDetails: details });
        this.showDetailModal.set(true);
    }

    reviewApplication(status: 'Approved' | 'Rejected') {
        const app = this.selectedApplication();
        if (!app) return;

        this.isProcessing.set(true);
        this.agentService.reviewRequest(app.id, status).subscribe({
            next: (res) => {
                this.isProcessing.set(false);
                this.showDetailModal.set(false);
                this.message.set({ type: 'success', text: `Policy ${status} successfully!` });
                this.loadPolicyRequests();
                setTimeout(() => this.message.set({ type: '', text: '' }), 3000);
            },
            error: (err) => {
                this.isProcessing.set(false);
                this.message.set({ type: 'error', text: 'Operation failed!' });
            }
        });
    }

    viewClaimDetails(claim: any) {
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

                const details = normalize(raw);
                if (details) {
                    details.applicant = normalize(details.applicant || details.primaryApplicant || raw.Applicant || raw.PrimaryApplicant);
                    details.nominee = normalize(details.nominee || raw.Nominee);
                    details.familyMembers = details.familyMembers || raw.FamilyMembers;
                    details.paymentMode = details.paymentMode || raw.PaymentMode;
                }
                claim.fullPolicyDetails = details;

                // Enrich with coverage
                claim.totalCoverage = claim.policy?.totalCoverageAmount || 0;
                claim.remainingCoverage = claim.policy?.remainingCoverageAmount || 0;
            } catch (e) {
                console.error('Failed to parse claim policy data', e);
            }
        }
        this.selectedClaim.set({
            ...claim,
            assignedClaimOfficer: claim.assignedOfficer // Map backend property to frontend expected name
        });
        this.showClaimModal.set(true);
    }

    viewUnifiedDetails(item: any, type: 'policy' | 'claim' = 'policy') {
        let policy: any;
        let claim: any;

        if (type === 'policy') {
            policy = item;
            claim = this.customerClaims().find(c => c.policyId === policy.id || c.policyApplicationId === policy.id);
        } else {
            claim = item;
            policy = claim.policy || this.policyRequests().find(p => p.id === (claim.policyId || claim.policyApplicationId)) || this.myCustomers().find(p => p.id === (claim.policyId || claim.policyApplicationId));
        }

        // Aggregate complete details
        const details: any = {
            customer: policy?.user || claim?.user,
            policy: {
                ...policy,
                totalCoverage: policy?.totalCoverageAmount || 0,
                remainingCoverage: policy?.remainingCoverageAmount || 0
            },
            agent: policy?.assignedAgent,
            claim: claim ? {
                ...claim,
                totalCoverage: policy?.totalCoverageAmount || 0,
                remainingCoverage: policy?.remainingCoverageAmount || 0
            } : null,
            claimOfficer: claim?.assignedOfficer || claim?.assignedClaimOfficer,
            payments: policy?.payments || []
        };

        // Proactively fetch claim if missing and policy ID exists
        if (!claim && policy?.id) {
            this.claimService.getClaimByPolicyId(policy.id).subscribe({
                next: (fetchedClaim) => {
                    if (fetchedClaim) {
                        // Enrich and update side panel
                        const enrichedClaim = {
                            ...fetchedClaim,
                            totalCoverage: policy?.totalCoverageAmount || 0,
                            remainingCoverage: policy?.remainingCoverageAmount || 0
                        };
                        details.claim = enrichedClaim;
                        details.claimOfficer = fetchedClaim.assignedOfficer || fetchedClaim.assignedClaimOfficer;
                        this.selectedUnifiedDetail.set({ ...details });
                    }
                }
            });
        }

        // Parse ApplicationDataJson if present (Mirroring Claims Officer depth)
        if (policy?.applicationDataJson) {
            try {
                const raw = JSON.parse(policy.applicationDataJson);
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
                details.policy.fullDetails = fullDetails;

                // Sync sum insured from config if available
                this.policyService.getConfiguration().subscribe(config => {
                    if (config) {
                        const cat = config.policyCategories?.find((c: any) => c.categoryId === policy.policyCategory);
                        const tier = cat?.tiers?.find((t: any) => t.tierId === policy.tierId);
                        details.policy.coverageAmount = tier?.baseCoverageAmount || (policy.sumInsured || 0);
                    }
                });
            } catch (e) {
                console.error('Failed to parse policy data', e);
            }
        }

        this.selectedUnifiedDetail.set(details);
        this.showUnifiedDetail.set(true);
    }

    logout() {
        this.authService.logout();
    }
}
