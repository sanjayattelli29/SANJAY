import { Component, EventEmitter, Input, Output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PolicyService } from '../../../services/policy.service';

@Component({
  selector: 'app-create-policy-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-policy-section.component.html'
})
export class CreatePolicySectionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private policyService = inject(PolicyService);

  @Input() config: any;
  @Output() configUpdated = new EventEmitter<void>();

  categoryForm!: FormGroup;
  tierForm!: FormGroup;

  isSubmittingCategory = false;
  isSubmittingTier = false;
  message = { text: '', type: '' };

  ngOnInit() {
    this.categoryForm = this.fb.group({
      categoryId: ['', [Validators.required, Validators.pattern(/^[A-Z0-9_]+$/)]],
      categoryName: ['', Validators.required],
      maxMembersAllowed: [1, [Validators.required, Validators.min(1)]],
      premiumBasedOn: ['PrimaryApplicantOnly', Validators.required]
    });

    this.tierForm = this.fb.group({
      selectedCategoryId: ['', Validators.required],
      tierId: ['', [Validators.required, Validators.pattern(/^[A-Z0-9_]+$/)]],
      tierName: ['', Validators.required],
      baseCoverageAmount: ['', [Validators.required, Validators.min(1)]],
      basePremiumAmount: ['', [Validators.required, Validators.min(1)]],
      validityInYears: [1, [Validators.required, Validators.min(1)]],
      benefitsList: ['', Validators.required] // we will split by comma
    });
  }

  submitCategory() {
    if (this.categoryForm.invalid) return;

    this.isSubmittingCategory = true;
    const formValue = this.categoryForm.value;

    const newCategory: any = {
      categoryId: formValue.categoryId.toUpperCase(),
      categoryName: formValue.categoryName,
      maxMembersAllowed: formValue.maxMembersAllowed,
      premiumBasedOn: formValue.premiumBasedOn,
      applicationRequirements: {
        documentsRequired: [
          { docType: 'IdentityProof', required: true },
          { docType: 'AgeProof', required: true },
          { docType: 'MedicalReport', required: true }
        ],
        applicantQuestions: ['Full Name', 'Date Of Birth', 'Gender'],
        nomineeInformation: ['Nominee Name', 'Relationship', 'Nominee Phone', 'Bank Account Number', 'IFSC Code']
      },
      tiers: []
    };

    if (formValue.maxMembersAllowed > 1) {
        newCategory.applicationRequirements['familyMemberQuestions'] = ['Member Name', 'Relationship', 'Date Of Birth'];
    }

    this.policyService.createCategory(newCategory).subscribe({
      next: () => {
        this.isSubmittingCategory = false;
        this.showMessage('Category created successfully!', 'success');
        this.categoryForm.reset({ maxMembersAllowed: 1, premiumBasedOn: 'PrimaryApplicantOnly' });
        this.configUpdated.emit(); // refresh config in parent
      },
      error: (err) => {
        this.isSubmittingCategory = false;
        this.showMessage(err.error?.Message || 'Failed to create category', 'error');
      }
    });
  }

  submitTier() {
    if (this.tierForm.invalid) return;

    this.isSubmittingTier = true;
    const formValue = this.tierForm.value;

    const benefits = formValue.benefitsList.split(',').map((b: string) => b.trim()).filter((b: string) => b.length > 0);

    const newTier = {
      tierId: formValue.tierId.toUpperCase(),
      tierName: formValue.tierName,
      baseCoverageAmount: Number(formValue.baseCoverageAmount),
      basePremiumAmount: Number(formValue.basePremiumAmount),
      validityInYears: Number(formValue.validityInYears),
      benefits: benefits
    };

    this.policyService.createTier(formValue.selectedCategoryId, newTier).subscribe({
      next: () => {
        this.isSubmittingTier = false;
        this.showMessage('Policy Tier created successfully!', 'success');
        this.tierForm.reset({ validityInYears: 1 });
        this.configUpdated.emit(); // refresh config in parent
      },
      error: (err) => {
        this.isSubmittingTier = false;
        this.showMessage(err.error?.Message || 'Failed to create tier', 'error');
      }
    });
  }

  private showMessage(text: string, type: string) {
    this.message = { text, type };
    setTimeout(() => {
      this.message = { text: '', type: '' };
    }, 5000);
  }
}
