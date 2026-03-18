import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { n8nWebhooks } from '../../environments/n8n/n8n';

// admin service for admin panel operations
// talks to backend admin controller for admin stuff
@Injectable({
    providedIn: 'root'
})
export class AdminService {
    // http for making backend calls
    private http = inject(HttpClient);
    // admin controller url in backend
    private apiUrl = `${environment.apiUrl}/Admin`;

    // get all agents from backend users table filtered by role
    getAgents(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/agents`);
    }

    // get claim officers list from db via backend controller
    getClaimOfficers(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/claim-officers`);
    }

    // create new agent user in backend db via identity system
    createAgent(agentData: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/create-agent`, agentData);
    }

    // create claim officer in backend db
    createClaimOfficer(officerData: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/create-claim-officer`, officerData);
    }

    // delete user from db by id via backend controller
    deleteUser(userId: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/delete-user/${userId}`);
    }

    // get pending policy applications waiting for agent assignment
    getPolicyRequests(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/policy-requests`);
    }

    // get agents with their workload count for assignment
    getAgentsWithLoad(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/agents-with-load?t=${Date.now()}`);
    }

    // assign agent to policy application in db
    assignAgent(applicationId: string, agentId: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/assign-agent`, { applicationId, agentId });
    }

    // get all users from backend for admin analytics
    getAllUsers(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/all-users?t=${Date.now()}`);
    }

    // get all claims for admin dashboard stats
    getAllClaims(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/all-claims?t=${Date.now()}`);
    }

    // get dashboard statistics from backend calculated data
    getAdminStats(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/admin-stats?t=${Date.now()}`);
    }

    // get payment records from report controller
    getUnifiedPayments(): Observable<any[]> {
        return this.http.get<any[]>(`${environment.apiUrl}/Report/unified-payments?t=${Date.now()}`);
    }

    // send email via n8n webhook external service not backend db
    sendAdminEmail(payload: { toEmail: string, subject: string, htmlBody: string }): Observable<any> {
        const webhookUrl = n8nWebhooks.agentSendEmail;
        return this.http.post(webhookUrl, payload);
    }

    // ask AI for data analysis using Semantic Kernel + Groq
    askAI(prompt: string): Observable<{ answer: string }> {
        return this.http.post<{ answer: string }>(`${this.apiUrl}/AI/ask`, { prompt });
    }
}
