import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ClaimsOfficerLoginPage } from './claims-officer-login.page';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('ClaimsOfficerLoginPage', () => {
    let component: ClaimsOfficerLoginPage;
    let fixture: ComponentFixture<ClaimsOfficerLoginPage>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ClaimsOfficerLoginPage, HttpClientTestingModule, RouterTestingModule]
        }).compileComponents();

        fixture = TestBed.createComponent(ClaimsOfficerLoginPage);
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
