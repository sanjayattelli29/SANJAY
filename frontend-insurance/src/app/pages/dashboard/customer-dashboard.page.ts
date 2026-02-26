import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PolicyService } from '../../services/policy.service';
import { ClaimService } from '../../services/claim.service';

@Component({
    selector: 'app-customer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './customer-dashboard.page.html',
    styleUrls: ['./customer-dashboard.page.css']
})
export class CustomerDashboardPage implements OnInit {
    private authService = inject(AuthService);
    private policyService = inject(PolicyService);
    private claimService = inject(ClaimService);

    user = this.authService.getUser();
    activeView = signal<'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims'>('dashboard');

    // Configuration and Data
    config: any = null;
    myPolicies: any[] = [];
    myClaims: any[] = [];

    // Selection for Buy Policy
    selectedCategory: any = null;
    selectedTier: any = null;

    // Form Data
    applicationForm: any = {
        applicant: {
            fullName: '',
            age: 18,
            profession: 'SoftwareEmployee',
            alcoholHabit: 'nonDrinker',
            smokingHabit: 'nonSmoker',
            travelKmPerMonth: 0
        },
        paymentMode: 'yearly',
        nominee: {
            nomineeName: '',
            nomineeEmail: '',
            nomineePhone: '',
            nomineeBankAccountNumber: ''
        },
        familyMembers: []
    };

    calculatedPremium = signal<number>(0);
    isSubmitting = signal<boolean>(false);

    // Policy Detail & Payment
    showPolicyDetailModal = signal(false);
    selectedPolicy = signal<any | null>(null);
    isPaying = signal(false);

    ngOnInit() {
        this.loadConfig();
        this.loadMyPolicies();
        this.loadMyClaims();
    }

    loadConfig() {
        this.policyService.getConfiguration().subscribe({
            next: (config) => {
                this.config = config;
                console.log('Policy Configuration loaded:', config);
            }
        });
    }

    loadMyPolicies() {
        this.policyService.getMyPolicies().subscribe({
            next: (policies) => {
                this.myPolicies = policies;
                console.log('User policies loaded:', policies);
            }
        });
    }

    loadMyClaims() {
        this.claimService.getMyClaims().subscribe({
            next: (claims) => {
                this.myClaims = claims;
                console.log('User claims loaded:', claims);
            }
        });
    }

    switchView(view: 'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims') {
        this.activeView.set(view);
        if (view === 'buy-policy') {
            this.selectedCategory = null;
            this.selectedTier = null;
        }
    }

    selectCategory(category: any) {
        this.selectedCategory = category;
        this.selectedTier = null;
        this.applicationForm.familyMembers = [];
    }

    selectTier(tier: any) {
        this.selectedTier = tier;
        this.updatePremium();
    }

    addFamilyMember() {
        if (this.applicationForm.familyMembers.length < 3) {
            this.applicationForm.familyMembers.push({ fullName: '', relation: 'Spouse' });
        }
    }

    removeFamilyMember(index: number) {
        this.applicationForm.familyMembers.splice(index, 1);
    }

    updatePremium() {
        if (!this.selectedCategory || !this.selectedTier) return;

        const request = {
            policyCategory: this.selectedCategory.categoryId,
            tierId: this.selectedTier.tierId,
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? this.applicationForm.applicant : null,
            primaryApplicant: this.selectedCategory.categoryId === 'FAMILY' ? this.applicationForm.applicant : null,
            familyMembers: this.applicationForm.familyMembers,
            paymentMode: this.applicationForm.paymentMode
        };

        this.policyService.calculatePremium(request).subscribe({
            next: (res) => this.calculatedPremium.set(res.premium)
        });
    }

    submitApplication() {
        this.isSubmitting.set(true);
        const request = {
            policyCategory: this.selectedCategory.categoryId,
            tierId: this.selectedTier.tierId,
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? this.applicationForm.applicant : null,
            primaryApplicant: this.selectedCategory.categoryId === 'FAMILY' ? this.applicationForm.applicant : null,
            familyMembers: this.applicationForm.familyMembers,
            paymentMode: this.applicationForm.paymentMode,
            nominee: this.applicationForm.nominee
        };

        this.policyService.applyForPolicy(request).subscribe({
            next: () => {
                alert('Policy Application Submitted Successfully! It will be reviewed by an agent soon.');
                this.isSubmitting.set(false);
                this.loadMyPolicies();
                this.switchView('my-policies');
            },
            error: (err) => {
                console.error('Application failed:', err);
                this.isSubmitting.set(false);
                alert('Failed to submit application: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    viewPolicyDetails(pol: any) {
        // Parse and normalize JSON data (similar to Agent Dashboard logic)
        const raw = JSON.parse(pol.applicationDataJson);
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

        // Fetch Tier benefits if possible (from the loaded config)
        let tierBenefits: string[] = [];
        if (this.config) {
            const cat = this.config.policyCategories.find((c: any) => c.categoryId === pol.policyCategory);
            const tier = cat?.tiers.find((t: any) => t.tierId === pol.tierId);
            tierBenefits = tier?.benefits || [];
            pol.coverageAmount = tier?.baseCoverageAmount || 0;
        }

        this.selectedPolicy.set({ ...pol, fullDetails: details, benefits: tierBenefits });
        this.showPolicyDetailModal.set(true);
    }

    payPremium() {
        const pol = this.selectedPolicy();
        if (!pol) return;

        if (!confirm(`Confirm payment of ₹${pol.calculatedPremium} for ${pol.tierId} policy?`)) return;

        this.isPaying.set(true);
        this.policyService.processPayment(pol.id, pol.calculatedPremium).subscribe({
            next: (res) => {
                this.isPaying.set(false);
                this.showPolicyDetailModal.set(false);
                alert('Payment Successful! Your policy is now ACTIVE.');
                this.loadMyPolicies(); // Refresh
            },
            error: (err) => {
                this.isPaying.set(false);
                alert('Payment failed: ' + (err.error?.message || 'Processing error'));
            }
        });
    }

    // Claim Raising
    selectedPolicyForClaim = signal<any | null>(null);
    claimForm: any = {
        incidentDate: '',
        incidentType: 'Accidental Injury',
        incidentLocation: '',
        description: '',
        hospitalName: '',
        hospitalizationRequired: false,
        estimatedClaimAmount: 0,
        affectedMemberName: '',
        affectedMemberRelation: ''
    };
    claimFiles: File[] = [];

    initiateClaim(pol: any) {
        this.selectedPolicyForClaim.set(pol);
        this.claimForm.affectedMemberName = '';
        this.claimForm.affectedMemberRelation = '';
        this.claimFiles = [];
        this.switchView('raise-claim');
    }

    onFileChange(event: any) {
        if (event.target.files.length > 0) {
            this.claimFiles = Array.from(event.target.files);
        }
    }

    submitClaim() {
        if (!this.selectedPolicyForClaim()) return;

        this.isSubmitting.set(true);
        const formData = new FormData();
        formData.append('policyApplicationId', this.selectedPolicyForClaim()!.id);
        formData.append('incidentDate', this.claimForm.incidentDate);
        formData.append('incidentType', this.claimForm.incidentType);
        formData.append('incidentLocation', this.claimForm.incidentLocation);
        formData.append('description', this.claimForm.description);
        formData.append('hospitalName', this.claimForm.hospitalName);
        formData.append('hospitalizationRequired', this.claimForm.hospitalizationRequired.toString());
        formData.append('estimatedClaimAmount', this.claimForm.estimatedClaimAmount.toString());

        if (this.selectedPolicyForClaim()!.policyCategory === 'FAMILY') {
            formData.append('affectedMemberName', this.claimForm.affectedMemberName);
            formData.append('affectedMemberRelation', this.claimForm.affectedMemberRelation);
        }

        this.claimFiles.forEach(file => {
            formData.append('documents', file, file.name);
        });

        this.claimService.raiseClaim(formData).subscribe({
            next: () => {
                alert('Claim Raised Successfully! Admin will assign an officer to review it.');
                this.isSubmitting.set(false);
                this.loadMyClaims();
                this.switchView('my-claims');
            },
            error: (err) => {
                this.isSubmitting.set(false);
                alert('Failed to raise claim: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    logout() {
        this.authService.logout();
    }
}
