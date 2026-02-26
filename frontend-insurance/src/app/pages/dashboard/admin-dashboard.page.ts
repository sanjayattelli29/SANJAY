import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { AdminService } from '../../services/admin.service';
import { ClaimService } from '../../services/claim.service';

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
    private fb = inject(FormBuilder);

    user = this.authService.getUser();
    activeSection = signal('dashboard'); // dashboard, agents, officers
    isLoading = signal(false);
    message = signal({ type: '', text: '' });

    // Data
    stats: any[] = [];
    recentActivities: any[] = [];
    agents: any[] = [];
    officers: any[] = [];
    policyRequests = signal<any[]>([]);
    agentsWithLoad = signal<any[]>([]);
    pendingClaims = signal<any[]>([]);
    claimOfficersWithWorkload = signal<any[]>([]);

    // UI State
    showAssignModal = signal(false);
    showAssignOfficerModal = signal(false);
    selectedApplicationId = signal<string | null>(null);
    selectedClaimId = signal<string | null>(null);
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
        this.loadAgents();
        this.loadOfficers();
        this.loadPolicyRequests();
        this.loadPendingClaims();
    }

    setSection(section: string) {
        this.activeSection.set(section);
        this.message.set({ type: '', text: '' });
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

    loadPolicyRequests() {
        this.adminService.getPolicyRequests().subscribe({
            next: (data) => this.policyRequests.set(data),
            error: (err) => console.error('Failed to load policy requests', err)
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
