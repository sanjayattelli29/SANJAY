import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Guard for public routes that clears the session if the user visits them
// This prevents direct URL deep leaks and back button re-auth
export const publicGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);

  if (authService.isLoggedIn()) {
    console.log('[PublicGuard] User is logged in on a public route. Clearing session.');
    authService.logout(false); // Clear session WITHOUT redirecting to avoid infinite loops
  }

  return true; // Always allow entry to the public page
};
