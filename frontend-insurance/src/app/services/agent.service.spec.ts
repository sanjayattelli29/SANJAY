import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AgentService } from './agent.service';

describe('AgentService', () => {
    let service: AgentService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [AgentService]
        });
        service = TestBed.inject(AgentService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should have getMyRequests method', () => {
        expect(service.getMyRequests).toBeDefined();
    });

    it('should have reviewRequest method', () => {
        expect(service.reviewRequest).toBeDefined();
    });

    it('should fetch commission stats', () => {
        expect(service.getCommissionStats).toBeDefined();
    });
});
