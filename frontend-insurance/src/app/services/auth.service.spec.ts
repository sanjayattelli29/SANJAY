import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from './auth.service';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('AuthService', () => {
    let service: AuthService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule, RouterTestingModule],
            providers: [AuthService]
        });
        service = TestBed.inject(AuthService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should return null for user when not logged in', () => {
        vi.spyOn(service, 'isLoggedIn').mockReturnValue(false);
        expect(service.getCurrentUser()).toBeNull();
    });

    it('should have logout method', () => {
        expect(service.logout).toBeDefined();
    });

    it('should clear storage on logout', () => {
        localStorage.setItem('auth_token', 'mock-token');
        service.logout();
        expect(localStorage.getItem('auth_token')).toBeNull();
    });
});
