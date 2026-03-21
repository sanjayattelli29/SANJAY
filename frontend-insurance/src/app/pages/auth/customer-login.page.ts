import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

// customer login page component
// handles login form with captcha validation before backend auth
@Component({
    selector: 'app-customer-login',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, RouterLink],
    templateUrl: './customer-login.page.html'
})
export class CustomerLoginPage implements OnInit, OnDestroy {
    // inject angular services
    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private router = inject(Router);

    // ui state signals for reactive updates
    isLoading = signal(false);
    errorMessage = signal('');
    // captcha math challenge numbers
    num1 = signal(0);
    num2 = signal(0);
    showPassword = false;

    // forget password states
    isForgetPasswordMode = signal(false);
    isForgetOtpSent = signal(false);
    isForgetOtpVerified = signal(false);
    forgetEmail = signal('');
    correctForgetOtp = signal(''); // store returned otp for verification

    loginForm = this.fb.group({
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]],
        captchaInput: ['', [Validators.required]]
    });

    forgetForm = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        otp: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]]
    });

    ngOnInit() {
        console.log('[CustomerLogin] ngOnInit called');
        this.generateCaptcha();
        
        // Force clear to beat browser autofill restoring old values during Back/Forward navigation
        setTimeout(() => {
            const val = this.loginForm.get('captchaInput')?.value;
            if (val) {
                console.log('[CustomerLogin] Clearing lingering captcha autofill value:', val);
            }
            this.loginForm.get('captchaInput')?.setValue('');
        });
    }

    // create simple math sum for captcha
    generateCaptcha() {
        this.num1.set(Math.floor(Math.random() * 9) + 1);
        this.num2.set(Math.floor(Math.random() * 9) + 1);
        this.loginForm.get('captchaInput')?.reset();
    }

    togglePassword() {
        this.showPassword = !this.showPassword;
    }

    // verify user entered correct sum
    isCaptchaCorrect(): boolean {
        const userInput = parseInt(this.loginForm.get('captchaInput')?.value || '0');
        const expected = this.num1() + this.num2();
        console.log(`[CustomerLogin] isCaptchaCorrect: UserValue="${this.loginForm.get('captchaInput')?.value}", UserInput=${userInput}, Expected=${expected}`);
        return userInput === expected;
    }

    // submit login form to backend via auth service
    onSubmit() {
        console.log(`[CustomerLogin] onSubmit called. Form Valid=${this.loginForm.valid}, CaptchaInput="${this.loginForm.get('captchaInput')?.value}", Email="${this.loginForm.get('emailId')?.value}"`);
        // check form validity first
        if (this.loginForm.valid) {
            // verify captcha before calling backend
            if (!this.isCaptchaCorrect()) {
                this.errorMessage.set('Wrong captcha. Please re-enter.');
                this.generateCaptcha(); // regenerate on failure
                return;
            }
            // show loading spinner
            this.isLoading.set(true);
            // call auth service which hits backend auth controller
            this.authService.login(this.loginForm.value).subscribe({
                next: (res) => {
                    // get role from backend response
                    const role = res.role || res.Role || res.auth_role;
                    // verify user is customer before allowing access
                    if (role === 'Customer') {
                        this.router.navigate(['/customer/dashboard']);
                    } else {
                        // logout if role mismatch for security
                        this.authService.logout();
                        this.errorMessage.set('Invalid user for this login portal.');
                        this.isLoading.set(false);
                    }
                },
                error: (err) => {
                    // show backend error message
                    this.errorMessage.set(err.error?.message || 'Invalid email or password.');
                    this.isLoading.set(false);
                }
            });
        }
    }

    toggleForgetPassword() {
        this.isForgetPasswordMode.set(!this.isForgetPasswordMode());
        this.errorMessage.set('');
        this.isForgetOtpSent.set(false);
        this.isForgetOtpVerified.set(false);
        this.forgetForm.reset();
    }

    sendForgetOtp() {
        const email = this.forgetForm.get('email')?.value;
        if (!email) return;

        this.isLoading.set(true);
        this.authService.sendForgetPasswordOtp(email).subscribe({
            next: (res) => {
                this.isLoading.set(false);
                let response = res;
                if (typeof res === 'string') {
                    try { response = JSON.parse(res); } catch (e) {}
                }

                if (response && response.status === 'success') {
                    this.isForgetOtpSent.set(true);
                    this.correctForgetOtp.set(response.otp); // Capture OTP
                    this.forgetEmail.set(email);
                    this.errorMessage.set('');
                } else {
                    this.errorMessage.set(response?.message || 'Failed to send OTP.');
                }
            },
            error: () => {
                this.isLoading.set(false);
                this.errorMessage.set('Network error. Check backend connection.');
            }
        });
    }

    verifyForgetOtp() {
        const enteredOtp = this.forgetForm.get('otp')?.value;
        if (enteredOtp === this.correctForgetOtp()) {
            this.isForgetOtpVerified.set(true);
            this.errorMessage.set('');
        } else {
            this.errorMessage.set('Incorrect OTP. Please try again.');
        }
    }

    submitNewPassword() {
        const newPass = this.forgetForm.get('newPassword')?.value;
        const confirmPass = this.forgetForm.get('confirmPassword')?.value;

        if (newPass !== confirmPass) {
            this.errorMessage.set('Passwords do not match.');
            return;
        }

        this.isLoading.set(true);
        this.authService.resetPassword(this.forgetEmail(), newPass!).subscribe({
            next: (res) => {
                this.isLoading.set(false);
                let response = res;
                if (typeof res === 'string') {
                    try { response = JSON.parse(res); } catch (e) {}
                }

                if (response && response.status === 'Success') {
                    alert('Password reset successful! Please login.');
                    this.toggleForgetPassword(); // Back to login
                } else {
                    this.errorMessage.set(response?.message || 'Reset failed.');
                }
            },
            error: (err) => {
                this.isLoading.set(false);
                this.errorMessage.set(err.error?.message || 'Server error.');
            }
        });
    }

    ngOnDestroy() {
        console.log('[CustomerLogin] ngOnDestroy called');
    }
}