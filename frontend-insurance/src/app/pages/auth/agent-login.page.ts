import { Component, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

// agent login page for insurance agents
// handles login and role verification before dashboard access
@Component({
    selector: 'app-agent-login',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, RouterLink],
    templateUrl: './agent-login.page.html'
})
export class AgentLoginPage {
    // inject services
    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private router = inject(Router);

    // ui state
    isLoading = signal(false);
    errorMessage = signal('');
    showPassword = false;

    // forget password states
    isForgetPasswordMode = signal(false);
    isForgetOtpSent = signal(false);
    isForgetOtpVerified = signal(false);
    forgetEmail = signal('');
    correctForgetOtp = signal(''); // store returned otp for verification

    togglePassword() {
        this.showPassword = !this.showPassword;
    }

    // login form
    loginForm = this.fb.group({
        emailId: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]]
    });

    forgetForm = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        otp: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]]
    });

    // submit to backend auth controller
    onSubmit() {
        if (this.loginForm.valid) {
            this.isLoading.set(true);
            // backend verifies credentials against db
            this.authService.login(this.loginForm.value).subscribe({
                next: (res) => {
                    // check role matches agent
                    const role = res.role || res.Role || res.auth_role;
                    if (role === 'Agent') {
                        this.router.navigate(['/agent/dashboard']);
                    } else {
                        // clear token if role mismatch
                        this.authService.logout();
                        this.errorMessage.set('Invalid agent user.');
                        this.isLoading.set(false);
                    }
                },
                error: (err) => {
                    this.errorMessage.set(err.error?.message || 'Invalid agent credentials.');
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
                    this.correctForgetOtp.set(response.otp);
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
                    this.toggleForgetPassword();
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
}
