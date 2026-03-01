import { Component, signal, inject, OnInit } from '@angular/core';
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
export class CustomerRegisterPage implements OnInit {
    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private router = inject(Router);

    currentStep = signal(1);
    isLoading = signal(false);
    errorMessage = signal('');
    num1 = signal(0);
    num2 = signal(0);

    registrationForm = this.fb.group({
        name: ['', [Validators.required]],
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [
            Validators.required,
            Validators.minLength(8),
            Validators.pattern(/^(?=.*[0-9])(?=.*[!@#$%^&*(),.?":{}|<>]).*$/)
        ]],
        mobileNumber: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
        captchaInput: ['', [Validators.required]]
    });

    get hasMinLength(): boolean {
        return (this.password?.value?.length || 0) >= 8;
    }

    get hasNumber(): boolean {
        return /[0-9]/.test(this.password?.value || '');
    }

    get hasSpecialChar(): boolean {
        return /[!@#$%^&*(),.?":{}|<>]/.test(this.password?.value || '');
    }

    ngOnInit() {
        this.generateCaptcha();
    }

    generateCaptcha() {
        this.num1.set(Math.floor(Math.random() * 10) + 1);
        this.num2.set(Math.floor(Math.random() * 10) + 1);
        this.registrationForm.get('captchaInput')?.reset();
    }

    isCaptchaCorrect(): boolean {
        const answer = parseInt(this.registrationForm.get('captchaInput')?.value || '0');
        return answer === (this.num1() + this.num2());
    }

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
            if (!this.isCaptchaCorrect()) {
                this.errorMessage.set('Incorrect captcha answer.');
                return;
            }
            this.isLoading.set(true);
            this.authService.register(this.registrationForm.value).subscribe({
                next: () => {
                    this.router.navigate(['/customer/login']); // Go to login after registration
                },
                error: (err) => {
                    this.errorMessage.set(err.error?.message || 'Registration failed. Please try again.');
                    this.isLoading.set(false);
                    this.generateCaptcha();
                }
            });
        } else {
            this.registrationForm.markAllAsTouched();
        }
    }
}
