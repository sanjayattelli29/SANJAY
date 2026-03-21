import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.page';
import { LearningPage } from './pages/learning/learning.page';
import { CustomerRegisterPage } from './pages/auth/customer-register.page';
import { CustomerLoginPage } from './pages/auth/customer-login.page';
import { AdminLoginPage } from './pages/auth/admin-login.page';
import { AgentLoginPage } from './pages/auth/agent-login.page';
import { ClaimsOfficerLoginPage } from './pages/auth/claims-officer-login.page';
import { authGuard } from './guards/auth.guard';
import { publicGuard } from './guards/public.guard';

// define all app routes for navigation
export const routes: Routes = [
  // public routes
  { path: '', component: LandingComponent, canActivate: [publicGuard] }, 
  { path: 'learning', component: LearningPage, canActivate: [publicGuard] }, 
  { path: 'register', component: CustomerRegisterPage, canActivate: [publicGuard] }, 
  
  // login routes for different user types
  { path: 'customer/login', component: CustomerLoginPage, canActivate: [publicGuard] },
  { path: 'admin/login', component: AdminLoginPage, canActivate: [publicGuard] },
  { path: 'agent/login', component: AgentLoginPage, canActivate: [publicGuard] },
  { path: 'claims-officer/login', component: ClaimsOfficerLoginPage, canActivate: [publicGuard] },

  // dashboards lazy loaded to reduce initial bundle size - PROTECTED
  { 
    path: 'customer/dashboard', 
    loadComponent: () => import('./pages/dashboard/customer-dashboard.page').then(m => m.CustomerDashboardPage),
    canActivate: [authGuard]
  },
  { 
    path: 'customer/policy/:id', 
    loadComponent: () => import('./pages/details/policy-details.page').then(m => m.PolicyDetailsPage),
    canActivate: [authGuard]
  },
  { 
    path: 'customer/claim/:id', 
    loadComponent: () => import('./pages/details/claim-details.page').then(m => m.ClaimDetailsPage),
    canActivate: [authGuard]
  },
  
  // admin agent officer dashboards - PROTECTED
  { 
    path: 'admin/dashboard', 
    loadComponent: () => import('./pages/dashboard/admin-dashboard.page').then(m => m.AdminDashboardPage),
    canActivate: [authGuard]
  },
  { 
    path: 'agent/dashboard', 
    loadComponent: () => import('./pages/dashboard/agent-dashboard.page').then(m => m.AgentDashboardPage),
    canActivate: [authGuard]
  },
  { 
    path: 'claims-officer/dashboard', 
    loadComponent: () => import('./pages/dashboard/claims-officer-dashboard.page').then(m => m.ClaimsOfficerDashboardPage),
    canActivate: [authGuard]
  },
  
  // chat page - PROTECTED
  { 
    path: 'chat/:policyId', 
    loadComponent: () => import('./pages/dashboard/chat.page').then(m => m.ChatPage),
    canActivate: [authGuard]
  },

  // redirect unknown routes to home
  { path: '**', redirectTo: '' }
];
