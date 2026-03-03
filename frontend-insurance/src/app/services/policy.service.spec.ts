import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PolicyService } from './policy.service';

describe('PolicyService', () => {
    let service: PolicyService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [PolicyService]
        });
        service = TestBed.inject(PolicyService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should fetch policy configuration', () => {
        expect(service.getConfiguration).toBeDefined();
    });

    it('should calculate premium', () => {
        expect(service.calculatePremium).toBeDefined();
    });

    it('should apply for policy', () => {
        expect(service.applyForPolicy).toBeDefined();
    });
});
