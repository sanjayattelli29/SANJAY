import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AdminService } from './admin.service';

describe('AdminService', () => {
    let service: AdminService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [AdminService]
        });
        service = TestBed.inject(AdminService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should fetch all users', () => {
        expect(service.getAllUsers).toBeDefined();
    });

    it('should fetch agents', () => {
        expect(service.getAgents).toBeDefined();
    });

    it('should get admin stats', () => {
        expect(service.getAdminStats).toBeDefined();
    });
});
