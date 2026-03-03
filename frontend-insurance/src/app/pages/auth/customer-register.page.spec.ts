import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { CustomerRegisterPage } from './customer-register.page';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('CustomerRegisterPage', () => {
    let component: CustomerRegisterPage;
    let fixture: ComponentFixture<CustomerRegisterPage>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CustomerRegisterPage, HttpClientTestingModule, RouterTestingModule]
        }).compileComponents();

        fixture = TestBed.createComponent(CustomerRegisterPage);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should be defined', () => {
        expect(component).toBeDefined();
    });
});
