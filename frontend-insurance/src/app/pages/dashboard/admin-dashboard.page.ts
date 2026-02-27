import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { AdminService } from '../../services/admin.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
    selector: 'app-admin-dashboard',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule],
    templateUrl: './admin-dashboard.page.html'
})
export class AdminDashboardPage implements OnInit {
    private authService = inject(AuthService);
    private adminService = inject(AdminService);
    private claimService = inject(ClaimService);
    private policyService = inject(PolicyService);
    private fb = inject(FormBuilder);

    user = this.authService.getUser();
    activeSection = signal('dashboard'); // dashboard, agents, officers, analysis-users, analysis-policies, analysis-commands
    isLoading = signal(false);
    message = signal({ type: '', text: '' });

    // Data
    adminStats = signal<any>(null);
    recentActivities: any[] = [];
    agents: any[] = [];
    officers: any[] = [];
    policyRequests = signal<any[]>([]);
    agentsWithLoad = signal<any[]>([]);
    pendingClaims = signal<any[]>([]);
    claimOfficersWithWorkload = signal<any[]>([]);

    // Complete Analysis Data
    allUsers = signal<any[]>([]);
    allClaims = signal<any[]>([]);
    allPolicyApps = signal<any[]>([]);
    config = signal<any>(null);

    // Chart instances
    private charts: Chart[] = [];

    // UI State
    showAssignModal = signal(false);
    showAssignOfficerModal = signal(false);
    selectedApplicationId = signal<string | null>(null);
    selectedClaimId = signal<string | null>(null);
    showUnifiedDetail = signal(false);
    selectedUnifiedDetail = signal<any>(null);
    isAssigning = signal(false);

    // Forms
    agentForm = this.fb.group({
        name: ['', [Validators.required, Validators.maxLength(100)]],
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        bankAccountNumber: ['', [Validators.required, Validators.pattern(/^\d{16}$/)]]
    });

    officerForm = this.fb.group({
        name: ['', [Validators.required, Validators.maxLength(100)]],
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        bankAccountNumber: ['', [Validators.required, Validators.pattern(/^\d{16}$/)]]
    });

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.loadAdminStats();
        this.loadAgents();
        this.loadOfficers();
        this.loadPolicyRequests();
        this.loadPendingClaims();
        this.loadAllUsers();
        this.loadAllClaims();
        this.loadConfig();
    }

    loadConfig() {
        this.policyService.getConfiguration().subscribe({
            next: (data) => this.config.set(data),
            error: (err) => console.error('Failed to load config', err)
        });
    }

    loadAdminStats() {
        this.adminService.getAdminStats().subscribe({
            next: (data: any) => this.adminStats.set(data),
            error: (err: any) => console.error('Failed to load admin stats', err)
        });
    }

    loadAllUsers() {
        this.adminService.getAllUsers().subscribe({
            next: (data: any) => this.allUsers.set(data),
            error: (err: any) => console.error('Failed to load all users', err)
        });
    }

    loadAllClaims() {
        this.adminService.getAllClaims().subscribe({
            next: (data: any) => {
                this.allClaims.set(data);
                this.initCharts();
            },
            error: (err: any) => console.error('Failed to load all claims', err)
        });
    }

    loadPolicyRequests() {
        this.adminService.getPolicyRequests().subscribe({
            next: (data: any) => {
                this.policyRequests.set(data);
                this.initCharts();
            },
            error: (err: any) => console.error('Failed to load policy requests', err)
        });
    }

    private initCharts() {
        // Only init if we have enough data and are on dashboard
        if (this.activeSection() !== 'dashboard') return;

        // Debounce or delay slightly to ensure DOM is ready
        setTimeout(() => {
            this.destroyCharts();
            this.createBestPolicyChart();
            this.createBestAgentChart();
            this.createRatioChart();
            this.createClaimsChart();
        }, 100);
    }

    private destroyCharts() {
        this.charts.forEach(c => c.destroy());
        this.charts = [];
    }

    private createBestPolicyChart() {
        const canvas = document.getElementById('bestPolicyChart') as HTMLCanvasElement;
        if (!canvas) return;

        const apps = this.policyRequests();
        const categories: any = {};
        apps.forEach(a => {
            categories[a.policyCategory] = (categories[a.policyCategory] || 0) + 1;
        });

        this.charts.push(new Chart(canvas, {
            type: 'pie',
            data: {
                labels: Object.keys(categories),
                datasets: [{
                    data: Object.values(categories),
                    backgroundColor: ['#4f46e5', '#f97316', '#10b981', '#6366f1', '#ec4899']
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { position: 'bottom', labels: { font: { weight: 'bold', size: 10 } } }
                }
            }
        }));
    }

    private createBestAgentChart() {
        const canvas = document.getElementById('bestAgentChart') as HTMLCanvasElement;
        if (!canvas) return;

        const apps = this.policyRequests();
        const agentPerf: any = {};
        apps.forEach(a => {
            if (a.assignedAgent) {
                const name = a.assignedAgent.email.split('@')[0];
                agentPerf[name] = (agentPerf[name] || 0) + 1;
            }
        });

        this.charts.push(new Chart(canvas, {
            type: 'bar',
            data: {
                labels: Object.keys(agentPerf),
                datasets: [{
                    label: 'Policies Assigned',
                    data: Object.values(agentPerf),
                    backgroundColor: '#6366f1',
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { display: false } },
                    x: { grid: { display: false } }
                }
            }
        }));
    }

    private createRatioChart() {
        const canvas = document.getElementById('ratioChart') as HTMLCanvasElement;
        if (!canvas) return;

        const totalUsers = this.allUsers().length;
        const totalAgents = this.agents.length;
        const totalOfficers = this.officers.length;

        this.charts.push(new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: ['Customers', 'Agents', 'Officers'],
                datasets: [{
                    data: [totalUsers - totalAgents - totalOfficers, totalAgents, totalOfficers],
                    backgroundColor: ['#4f46e5', '#f97316', '#64748b']
                }]
            },
            options: {
                responsive: true,
                cutout: '70%',
                plugins: {
                    legend: { position: 'bottom', labels: { font: { weight: 'bold', size: 10 } } }
                }
            }
        }));
    }

    private createClaimsChart() {
        const canvas = document.getElementById('claimsChart') as HTMLCanvasElement;
        if (!canvas) return;

        const claims = this.allClaims();
        const statusCounts: any = {};
        claims.forEach(c => {
            statusCounts[c.status] = (statusCounts[c.status] || 0) + 1;
        });

        this.charts.push(new Chart(canvas, {
            type: 'polarArea',
            data: {
                labels: Object.keys(statusCounts),
                datasets: [{
                    data: Object.values(statusCounts),
                    backgroundColor: ['#10b981', '#f97316', '#ef4444', '#6366f1']
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { position: 'bottom', labels: { font: { weight: 'bold', size: 10 } } }
                }
            }
        }));
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

    loadAgents() {
        this.adminService.getAgents().subscribe({
            next: (data) => this.agents = data,
            error: (err) => console.error('Failed to load agents', err)
        });
    }

    loadOfficers() {
        this.adminService.getClaimOfficers().subscribe({
            next: (data) => this.officers = data,
            error: (err) => console.error('Failed to load officers', err)
        });
    }

    openAssignModal(applicationId: string) {
        this.selectedApplicationId.set(applicationId);
        this.showAssignModal.set(true);
        this.isLoading.set(true);
        this.adminService.getAgentsWithLoad().subscribe({
            next: (data) => {
                this.agentsWithLoad.set(data);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error('Failed to load agents with load', err);
                this.isLoading.set(false);
            }
        });
    }

    assignAgent(agentId: string) {
        const appId = this.selectedApplicationId();
        if (!appId) return;

        this.isAssigning.set(true);
        this.adminService.assignAgent(appId, agentId).subscribe({
            next: (res) => {
                this.isAssigning.set(false);
                this.showAssignModal.set(false);
                this.message.set({ type: 'success', text: 'Agent assigned successfully!' });
                this.loadPolicyRequests();
                // Clear message after 3s
                setTimeout(() => this.message.set({ type: '', text: '' }), 3000);
            },
            error: (err) => {
                this.isAssigning.set(false);
                this.message.set({ type: 'error', text: 'Assignment failed!' });
            }
        });
    }

    loadPendingClaims() {
        this.claimService.getPendingClaims().subscribe({
            next: (data) => this.pendingClaims.set(data),
            error: (err) => console.error('Failed to load pending claims', err)
        });
    }

    openAssignOfficerModal(claimId: string) {
        this.selectedClaimId.set(claimId);
        this.showAssignOfficerModal.set(true);
        this.isLoading.set(true);
        this.claimService.getClaimOfficers().subscribe({
            next: (data) => {
                this.claimOfficersWithWorkload.set(data);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error('Failed to load claim officers', err);
                this.isLoading.set(false);
            }
        });
    }

    assignOfficer(officerId: string) {
        const claimId = this.selectedClaimId();
        if (!claimId) return;

        this.isAssigning.set(true);
        this.claimService.assignOfficer(claimId, officerId).subscribe({
            next: (res) => {
                this.isAssigning.set(false);
                this.showAssignOfficerModal.set(false);
                this.message.set({ type: 'success', text: 'Claim Officer assigned successfully!' });
                this.loadPendingClaims();
                setTimeout(() => this.message.set({ type: '', text: '' }), 3000);
            },
            error: (err) => {
                this.isAssigning.set(false);
                this.message.set({ type: 'error', text: 'Officer assignment failed!' });
            }
        });
    }

    registerAgent() {
        if (this.agentForm.valid) {
            this.isLoading.set(true);
            this.adminService.createAgent(this.agentForm.value).subscribe({
                next: (res: any) => {
                    this.isLoading.set(false);
                    if (res.status === 'Success') {
                        this.message.set({ type: 'success', text: res.message });
                        this.agentForm.reset();
                        this.loadAgents();
                    } else {
                        this.message.set({ type: 'error', text: res.message });
                    }
                },
                error: (err) => {
                    this.isLoading.set(false);
                    this.message.set({ type: 'error', text: 'Registration failed!' });
                }
            });
        }
    }

    registerOfficer() {
        if (this.officerForm.valid) {
            this.isLoading.set(true);
            this.adminService.createClaimOfficer(this.officerForm.value).subscribe({
                next: (res: any) => {
                    this.isLoading.set(false);
                    if (res.status === 'Success') {
                        this.message.set({ type: 'success', text: res.message });
                        this.officerForm.reset();
                        this.loadOfficers();
                    } else {
                        this.message.set({ type: 'error', text: res.message });
                    }
                },
                error: (err) => {
                    this.isLoading.set(false);
                    this.message.set({ type: 'error', text: 'Registration failed!' });
                }
            });
        }
    }

    viewUnifiedDetails(item: any, type: 'policy' | 'claim' = 'policy') {
        let policy: any;
        let claim: any;

        if (type === 'policy') {
            policy = item;
            claim = this.allClaims().find(c => c.policyId === policy.id);
        } else {
            claim = item;
            policy = claim.policy || this.policyRequests().find(p => p.id === claim.policyId);
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
            claimOfficer: claim?.assignedOfficer,
            payments: policy?.payments || []
        };

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
                if (this.config()) {
                    const cat = this.config().policyCategories?.find((c: any) => c.categoryId === policy.policyCategory);
                    const tier = cat?.tiers?.find((t: any) => t.tierId === policy.tierId);
                    details.policy.coverageAmount = tier?.baseCoverageAmount || (policy.sumInsured || 0);
                }
            } catch (e) {
                console.error('Failed to parse policy data', e);
            }
        }

        this.selectedUnifiedDetail.set(details);
        this.showUnifiedDetail.set(true);
    }

    deleteUser(userId: string) {
        if (confirm('Are you sure you want to delete this user?')) {
            this.adminService.deleteUser(userId).subscribe({
                next: (res: any) => {
                    if (res.status === 'Success') {
                        this.loadData();
                    } else {
                        alert(res.message);
                    }
                },
                error: (err) => alert('Delete failed!')
            });
        }
    }

    logout() {
        this.authService.logout();
    }
}
