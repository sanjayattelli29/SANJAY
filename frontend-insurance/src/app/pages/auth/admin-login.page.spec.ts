import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AdminLoginPage } from './admin-login.page';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('AdminLoginPage', () => {
    let component: AdminLoginPage;
    let fixture: ComponentFixture<AdminLoginPage>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AdminLoginPage, HttpClientTestingModule, RouterTestingModule]
        }).compileComponents();

        fixture = TestBed.createComponent(AdminLoginPage);
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
