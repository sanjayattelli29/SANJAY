import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AgentLoginPage } from './agent-login.page';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('AgentLoginPage', () => {
    let component: AgentLoginPage;
    let fixture: ComponentFixture<AgentLoginPage>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AgentLoginPage, HttpClientTestingModule, RouterTestingModule]
        }).compileComponents();

        fixture = TestBed.createComponent(AgentLoginPage);
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
