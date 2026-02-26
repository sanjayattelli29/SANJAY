import { Component, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-customer-register',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, RouterLink],
    templateUrl: './customer-register.page.html'
})
export class CustomerRegisterPage {
    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private router = inject(Router);

    currentStep = signal(1);
    isLoading = signal(false);
    errorMessage = signal('');

    registrationForm = this.fb.group({
        name: ['', [Validators.required]],
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        mobileNumber: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]]
    });

    get email() { return this.registrationForm.get('emailId'); }
    get name() { return this.registrationForm.get('name'); }
    get password() { return this.registrationForm.get('password'); }
    get mobile() { return this.registrationForm.get('mobileNumber'); }

    nextStep() {
        if (this.currentStep() === 1) {
            if (this.name?.valid && this.email?.valid) {
                this.currentStep.set(2);
            } else {
                this.registrationForm.markAllAsTouched();
            }
        }
    }

    prevStep() {
        this.currentStep.set(1);
    }

    onSubmit() {
        if (this.registrationForm.valid) {
            this.isLoading.set(true);
            this.authService.register(this.registrationForm.value).subscribe({
                next: () => {
                    this.router.navigate(['/customer/login']); // Go to login after registration
                },
                error: (err) => {
                    this.errorMessage.set(err.error?.message || 'Registration failed. Please try again.');
                    this.isLoading.set(false);
                }
            });
        } else {
            this.registrationForm.markAllAsTouched();
        }
    }
}
