import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import {n8nWebhooks} from '../../environments/n8n/n8n';
// auth service handles all login logout stuff for frontend
// talks to asp.net backend api controllers for authentication
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // check if user logged in then give back user data
  getCurrentUser() {
    if (this.isLoggedIn()) {
      return this.getUser();
    }
    return null;
  }
  // http client to make api calls to backend
  private http = inject(HttpClient);
  // router for navigation after login/logout
  private router = inject(Router);
   // backend api base url pointing to .net core api
  private apiUrl = environment.apiUrl;

  // register new customer by sending data to auth controller in backend
  // hits Auth/register endpoint which creates user in db via controller
  register(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register`, data);
  }

  // login user by hitting backend api auth controller
  // backend checks credentials against db and returns jwt token
  // we store token in localstorage for future api calls
  login(data: any): Observable<any> {
    console.log('[AuthService] Attempting login for:', data.emailId);
    return this.http.post(`${this.apiUrl}/Auth/login`, data).pipe(
      tap((response: any) => {
        console.log('[AuthService] Login response received:', JSON.stringify(response));

        // backend might send token in different field names so checking all
        const token = response.token || response.Token || response.auth_token;
        const role = response.role || response.Role || response.auth_role;
        const userId = response.id || response.Id;
        const userName = response.fullName || response.FullName;
        const userPhone = response.phoneNumber || response.PhoneNumber;

        // saving all user data to browser localstorage
        // token needed for api auth, role for access control
        if (token) {
          localStorage.setItem('auth_token', token);
          localStorage.setItem('auth_role', role || '');
          localStorage.setItem('user_email', response.email || '');
          localStorage.setItem('user_id', userId || '');
          localStorage.setItem('user_name', userName || '');
          localStorage.setItem('user_phone', userPhone || '');
          // save profile image url from backend so it persists across sessions
          if (response.profileImageUrl || response.ProfileImageUrl) {
            localStorage.setItem('user_profile_image_' + userId, response.profileImageUrl || response.ProfileImageUrl);
          }
          console.log('[AuthService] Token and user info saved to localStorage using auth_ keys');
        } else {
          console.warn('[AuthService] No token found in login response! Keys present:', Object.keys(response));
        }
      })
    );
  }

  // logout clears all user data from browser
  // removes token so user cant make authorized api calls anymore
  // redirects to home page
  logout(redirect: boolean = true) {
    localStorage.clear(); // Clear all user data from browser to prevent leakage
    if (redirect) {
      this.router.navigate(['/']);
    }
  }

  // get jwt token from localstorage to attach to api calls
  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  // pull all user info from localstorage and return as object
  getUser() {
    return {
      id: localStorage.getItem('user_id'),
      name: localStorage.getItem('user_name'),
      email: localStorage.getItem('user_email'),
      phone: localStorage.getItem('user_phone'),
      role: localStorage.getItem('auth_role')
    };
  }

  // quick check if user logged in by seeing if token exists
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  // get user role for access control like admin, customer, agent
  getUserRole(): string | null {
    return localStorage.getItem('auth_role');
  }

  // update current role in localstorage when switching roles
  setCurrentRole(role: string) {
    localStorage.setItem('auth_role', role);
  }

  // update backend KYC status
  completeKyc(userId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/complete-kyc/${userId}`, {});
  }

  // upload a profile image to imagekit via the backend and return the CDN URL
  uploadProfileImage(userId: string, base64Image: string, fileName: string): Observable<{ imageUrl: string }> {
    return this.http.post<{ imageUrl: string }>(`${this.apiUrl}/Auth/upload-profile-image`,
      { userId, base64Image, fileName },
      { headers: { Authorization: 'Bearer ' + this.getToken() } }
    );
  }

  // Send OTP for forget password using n8n webhook
  sendForgetPasswordOtp(email: string, name: string = 'Customer'): Observable<any> {
    const webhookUrl = n8nWebhooks.forgetSendOtp;
    return this.http.post(webhookUrl, { email, name });
  }

  // Reset password in .NET backend
  resetPassword(email: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/reset-password`, { email, newPassword });
  }
}
