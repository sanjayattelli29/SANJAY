import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { AgentService } from '../../services/agent.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';

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
    stats: any = { pendingReview: 0, totalCommission: 0 };
    policyRequests = signal<any[]>([]);
    commissionData = signal<any>({ totalCommission: 0, activePolicies: [] });
    customerClaims = signal<any[]>([]);
    myCustomers = signal<any[]>([]);

    // UI State for Modal
    showDetailModal = signal(false);
    showClaimModal = signal(false);
    selectedApplication = signal<any | null>(null);
    selectedClaim = signal<any | null>(null);
    isProcessing = signal(false);

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.loadPolicyRequests();
        this.loadCommissionStats();
        this.loadCustomerClaims();
        this.loadMyCustomers();
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
            } catch (e) {
                console.error('Failed to parse claim policy data', e);
            }
        }
        this.selectedClaim.set(claim);
        this.showClaimModal.set(true);
    }

    logout() {
        this.authService.logout();
    }
}
