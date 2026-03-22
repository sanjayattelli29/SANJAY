import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Functional Route Guard protecting dashboards from unauthenticated deep links
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // 1. Check if token exists in localStorage
  if (authService.isLoggedIn()) {
    const role = authService.getUserRole()?.toLowerCase();
    const url = state.url.toLowerCase();

    // 2. Enforce Role-Based Access controls over path prefixes
    if (url.startsWith('/admin') && role !== 'admin') {
      router.navigate(['/admin/login']);
      return false;
    }
    if (url.startsWith('/agent') && role !== 'agent') {
      router.navigate(['/agent/login']);
      return false;
    }
    if (url.startsWith('/claims-officer') && role !== 'claimofficer') {
      router.navigate(['/claims-officer/login']);
      return false;
    }

    return true; // Authorized
  }

  // 3. Not Logged In - Redirect to matching role login frame
  const url = state.url.toLowerCase();
  if (url.startsWith('/admin')) {
    router.navigate(['/admin/login']);
  } else if (url.startsWith('/agent')) {
    router.navigate(['/agent/login']);
  } else if (url.startsWith('/claims-officer')) {
    router.navigate(['/claims-officer/login']);
  } else {
    router.navigate(['/customer/login']); // Fallback to customer login
  }

  return false;
};
