import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { FormsModule } from '@angular/forms';

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

    // Review Modal State
    selectedClaim = signal<any | null>(null);
    showReviewModal = signal(false);
    reviewForm = {
        status: 'Approved',
        remarks: '',
        approvedAmount: 0
    };

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
            },
            error: (err) => {
                console.error('Failed to load requests', err);
                this.isLoading.set(false);
            }
        });
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
                    details.familyMembers = details.familyMembers || raw.FamilyMembers;
                    details.paymentMode = details.paymentMode || raw.PaymentMode;
                }

                // Calculate Sum Insured from config
                if (this.config()) {
                    const cat = this.config().policyCategories.find((c: any) => c.categoryId === claim.policy?.policyCategory);
                    const tier = cat?.tiers.find((t: any) => t.tierId === claim.policy?.tierId);
                    claim.coverageAmount = tier?.baseCoverageAmount || 0;
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

    submitReview() {
        const claimId = this.selectedClaim()?.id;
        if (!claimId) return;

        this.isLoading.set(true);
        this.claimService.reviewClaim(claimId, this.reviewForm.status, this.reviewForm.remarks, this.reviewForm.approvedAmount).subscribe({
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
