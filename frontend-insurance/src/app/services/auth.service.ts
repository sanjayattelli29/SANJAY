import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

/**
 * Service for handling all authentication and authorization logic in the frontend.
 * Communicates with the ASP.NET Core Identity backend.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = 'https://localhost:7140/api';

  /**
   * Registers a new customer.
   */
  register(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register`, data);
  }

  /**
   * Logs in a user and stores the JWT token and user info.
   */
  login(data: any): Observable<any> {
    console.log('[AuthService] Attempting login for:', data.emailId);
    return this.http.post(`${this.apiUrl}/Auth/login`, data).pipe(
      tap((response: any) => {
        console.log('[AuthService] Login response received:', JSON.stringify(response));

        // Try all possible token variations
        const token = response.token || response.Token || response.auth_token;
        const role = response.role || response.Role || response.auth_role;

        if (token) {
          localStorage.setItem('auth_token', token);
          localStorage.setItem('auth_role', role || '');
          localStorage.setItem('user_email', response.email || '');
          console.log('[AuthService] Token and user info saved to localStorage using auth_ keys');
        } else {
          console.warn('[AuthService] No token found in login response! Keys present:', Object.keys(response));
        }
      })
    );
  }

  /**
   * Logs out the user and clears stored data.
   */
  logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_role');
    localStorage.removeItem('user_email');
    localStorage.removeItem('user'); // Also remove old keys just in case
    localStorage.removeItem('token');
    this.router.navigate(['/']);
  }

  /**
   * Returns the stored JWT token.
   */
  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  /**
   * Returns the stored user info.
   */
  getUser() {
    return {
      email: localStorage.getItem('user_email'),
      role: localStorage.getItem('auth_role')
    };
  }

  /**
   * Checks if the user is authenticated.
   */
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  /**
   * Returns the user's role.
   */
  getUserRole(): string | null {
    return localStorage.getItem('auth_role');
  }
}
