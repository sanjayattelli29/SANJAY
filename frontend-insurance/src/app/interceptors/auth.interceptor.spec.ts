import { HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';
import { of, firstValueFrom } from 'rxjs';
import { vi, describe, it, expect } from 'vitest';

describe('authInterceptor', () => {
    it('should add authorization header if token exists', async () => {
        vi.spyOn(localStorage, 'getItem').mockReturnValue('mock-token');
        const req = new HttpRequest('GET', '/api/test');
        const next: HttpHandlerFn = (request) => {
            expect(request.headers.has('Authorization')).toBe(true);
            expect(request.headers.get('Authorization')).toBe('Bearer mock-token');
            return of({} as HttpEvent<any>);
        };

        await firstValueFrom(authInterceptor(req, next));
    });

    it('should not add header if token is missing', async () => {
        vi.spyOn(localStorage, 'getItem').mockReturnValue(null);
        const req = new HttpRequest('GET', '/api/test');
        const next: HttpHandlerFn = (request) => {
            expect(request.headers.has('Authorization')).toBe(false);
            return of({} as HttpEvent<any>);
        };

        await firstValueFrom(authInterceptor(req, next));
    });
});
