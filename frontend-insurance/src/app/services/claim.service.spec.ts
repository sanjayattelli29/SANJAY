import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ClaimService } from './claim.service';

describe('ClaimService', () => {
    let service: ClaimService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ClaimService]
        });
        service = TestBed.inject(ClaimService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should have raiseClaim method', () => {
        expect(service.raiseClaim).toBeDefined();
    });

    it('should fetch my claims', () => {
        expect(service.getMyClaims).toBeDefined();
    });

    it('should fetch pending claims for admin', () => {
        expect(service.getPendingClaims).toBeDefined();
    });
});
