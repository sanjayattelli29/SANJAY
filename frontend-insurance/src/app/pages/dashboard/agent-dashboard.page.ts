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

    // Email Feature State
    emailFormSubject = signal('');
    emailFormMessage = signal('');
    includePaymentReminder = signal(true);
    selectedCustomerForEmail = signal<any | null>(null);

    logout() {
        this.authService.logout();
    }

    // Email Methods
    openEmailForm(customer: any) {
        this.selectedCustomerForEmail.set(customer);
        this.emailFormSubject.set(`Policy Notification - ${customer.policyNumber || 'General'}`);
        this.emailFormMessage.set(`Dear ${customer.user?.email?.split('@')[0] || 'Customer'},\n\nI hope this email finds you well. I am reaching out to provide you with an update regarding your policy.`);
        this.message.set({ type: '', text: '' });
    }

    sendEmail() {
        const customer = this.selectedCustomerForEmail();
        if (!customer) return;

        this.isProcessing.set(true);
        const htmlBody = this.compileEmailHtml(customer);
        const payload = {
            toEmail: customer.user?.email,
            subject: this.emailFormSubject(),
            htmlBody: htmlBody
        };

        this.agentService.sendAgentEmail(payload).subscribe({
            next: () => {
                this.isProcessing.set(false);
                this.message.set({ type: 'success', text: 'Email sent successfully via n8n!' });
                this.selectedCustomerForEmail.set(null);
                setTimeout(() => this.message.set({ type: '', text: '' }), 3000);
            },
            error: (err) => {
                this.isProcessing.set(false);
                this.message.set({ type: 'error', text: 'Failed to send email. Check console for details.' });
                console.error(err);
            }
        });
    }

    private compileEmailHtml(cust: any): string {
        const agentName = this.user.email?.split('@')[0] || 'Agent';
        const agentEmail = this.user.email || '';
        const customerName = cust.user?.email?.split('@')[0] || 'Customer';
        const agentMessage = this.emailFormMessage().replace(/\n/g, '<br>');

        let paymentSection = '';
        if (this.includePaymentReminder()) {
            paymentSection = `
                <!-- Payment Breakdown -->
                <div style="margin-top: 25px; padding: 20px; background-color: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0;">
                    <h4 style="margin: 0 0 15px 0; color: #1e293b; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;">Payment & Policy Breakdown</h4>
                    <div style="margin-bottom: 10px;">
                        <span style="color: #64748b; font-size: 12px;">Next Payment:</span>
                        <strong style="display: block; color: #1e293b; font-size: 16px;">₹${cust.nextPaymentAmount || 0}</strong>
                    </div>
                    <div style="margin-bottom: 10px;">
                        <span style="color: #64748b; font-size: 12px;">Due Date:</span>
                        <strong style="display: block; color: #1e293b; font-size: 14px;">${cust.nextPaymentDate ? new Date(cust.nextPaymentDate).toLocaleDateString() : 'N/A'}</strong>
                    </div>
                    <div style="padding-top: 10px; border-top: 1px dashed #cbd5e1;">
                         <p style="margin: 0; color: #475569; font-size: 11px; line-height: 1.4;">
                            * Even if the button below is hidden by your email client, you can use the details above for your records.
                         </p>
                    </div>
                </div>
            `;
        }

        return `
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #334155; margin: 0; padding: 0; background-color: #f1f5f9; }
        .container { max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1); }
        .header { background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 30px; text-align: center; color: #ffffff; }
        .content { padding: 40px; }
        .card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 25px; margin: 20px 0; }
        .btn { display: inline-block; padding: 12px 30px; background-color: #4f46e5; color: #ffffff !important; text-decoration: none; border-radius: 100px; font-weight: bold; font-size: 14px; margin: 20px 0; box-shadow: 0 4px 6px -1px rgba(79, 70, 229, 0.4); }
        .footer { background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9; }
        .label { color: #64748b; font-size: 10px; text-transform: uppercase; font-weight: bold; letter-spacing: 0.05em; display: block; margin-bottom: 4px; }
        .value { color: #1e293b; font-weight: 600; font-size: 14px; margin-bottom: 12px; }
        .grid { display: block; }
        .col { margin-bottom: 15px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2 style="margin: 0; font-weight: 900; letter-spacing: -1px; font-size: 24px;">Insurance<span style="color: #f97316;">Platform</span></h2>
            <p style="margin: 5px 0 0 0; opacity: 0.8; font-size: 12px; text-transform: uppercase; font-weight: bold; letter-spacing: 2px;">Policy Notification</p>
        </div>
        <div class="content">
            <h3 style="color: #1e293b; font-size: 18px; margin-top: 0;">Hello ${customerName},</h3>
            <p style="color: #475569; font-size: 15px; margin-bottom: 30px;">${agentMessage}</p>

            <div class="card">
                <h4 style="margin: 0 0 20px 0; color: #1e293b; font-size: 14px; text-transform: uppercase; letter-spacing: 1px; border-bottom: 2px solid #f1f5f9; padding-bottom: 10px;">Policy Highlights</h4>
                <div class="grid">
                    <div class="col"><span class="label">Policy Type</span><div class="value">${cust.policyCategory}</div></div>
                    <div class="col"><span class="label">Tier / Plan</span><div class="value">${cust.tierId}</div></div>
                    <div class="col"><span class="label">Policy Number</span><div class="value">${cust.policyNumber || 'N/A'}</div></div>
                    <div class="col"><span class="label">Whole Coverage</span><div class="value">₹${cust.totalCoverageAmount?.toLocaleString() || 0}</div></div>
                    <div class="col"><span class="label">Current Coverage</span><div class="value">₹${cust.remainingCoverageAmount?.toLocaleString() || (cust.totalCoverageAmount?.toLocaleString() || 0)}</div></div>
                    <div class="col"><span class="label">Status</span><div class="value" style="color: #10b981;">● ${cust.status}</div></div>
                </div>
            </div>

            <div style="margin: 30px 0;">
                <h4 style="margin: 0 0 15px 0; color: #1e293b; font-size: 13px;">Customer & Agent Context</h4>
                <table width="100%" cellpadding="0" cellspacing="0" border="0">
                    <tr>
                        <td width="50%" style="padding-right: 15px;">
                            <span class="label">Customer Details</span>
                            <div class="value" style="font-size: 12px;">${cust.user?.email}</div>
                        </td>
                        <td width="50%">
                            <span class="label">Agent In-Charge</span>
                            <div class="value" style="font-size: 12px;">${agentName}<br>${agentEmail}</div>
                        </td>
                    </tr>
                </table>
            </div>

            <center>
                <a href="#" class="btn">View Payment Details</a>
            </center>

            ${paymentSection}

        </div>
        <div class="footer">
            &copy; 2024 InsurancePlatform. All rights reserved.<br>
            Secure Policy Management Service.
        </div>
    </div>
</body>
</html>
        `;
    }
}
