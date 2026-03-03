import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { CustomerLoginPage } from './customer-login.page';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('CustomerLoginPage', () => {
    let component: CustomerLoginPage;
    let fixture: ComponentFixture<CustomerLoginPage>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CustomerLoginPage, HttpClientTestingModule, RouterTestingModule]
        }).compileComponents();

        fixture = TestBed.createComponent(CustomerLoginPage);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have an invalid form when empty', () => {
        expect(component.loginForm.valid).toBe(false);
    });

    it('should validate captcha correctly', () => {
        component.num1.set(5);
        component.num2.set(5);
        component.loginForm.get('captchaInput')?.setValue('10');
        expect(component.isCaptchaCorrect()).toBe(true);
    });

    it('should generate new captcha on init', () => {
        const spy = vi.spyOn(component, 'generateCaptcha');
        component.ngOnInit();
        expect(spy).toHaveBeenCalled();
    });
});
