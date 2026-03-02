import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { Observable, tap } from 'rxjs';

export interface Notification {
    id: string;
    userId: string;
    title: string;
    message: string;
    createdAt: string;
    isRead: boolean;
    notificationType: string;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private http = inject(HttpClient);
    private authService = inject(AuthService);
    private apiUrl = 'https://localhost:7140/api/Notifications';
    private hubUrl = 'https://localhost:7140/notificationhub';

    private hubConnection: signalR.HubConnection | null = null;
    
    // Signals for reactive UI
    notifications = signal<Notification[]>([]);
    unreadCount = signal<number>(0);

    /**
     * Initializes SignalR connection for notifications.
     */
    async startConnection() {
        if (this.hubConnection) return;

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl, {
                accessTokenFactory: () => this.authService.getToken() || ''
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
            console.log('New notification received:', notification);
            this.notifications.update(prev => [notification, ...prev]);
            this.unreadCount.update(count => count + 1);
            
            // Play a subtle sound or trigger a toast if needed
        });

        try {
            await this.hubConnection.start();
            console.log('SignalR NotificationHub connected');
            this.loadInitialData();
        } catch (err) {
            console.error('Error while starting SignalR NotificationHub:', err);
        }
    }

    private loadInitialData() {
        this.getNotifications().subscribe(data => {
            this.notifications.set(data);
        });
        this.getUnreadCount().subscribe(count => {
            this.unreadCount.set(count);
        });
    }

    /**
     * Fetch all notifications for the user.
     */
    getNotifications(): Observable<Notification[]> {
        return this.http.get<Notification[]>(this.apiUrl);
    }

    /**
     * Get the count of unread notifications.
     */
    getUnreadCount(): Observable<number> {
        return this.http.get<number>(`${this.apiUrl}/unread-count`);
    }

    /**
     * Mark a specific notification as read.
     */
    markAsRead(id: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/read`, {}).pipe(
            tap(() => {
                this.notifications.update(list => 
                    list.map(n => n.id === id ? { ...n, isRead: true } : n)
                );
                this.unreadCount.update(count => Math.max(0, count - 1));
            })
        );
    }

    /**
     * Mark all notifications as read.
     */
    markAllAsRead(): Observable<any> {
        return this.http.post(`${this.apiUrl}/read-all`, {}).pipe(
            tap(() => {
                this.notifications.update(list => 
                    list.map(n => ({ ...n, isRead: true }))
                );
                this.unreadCount.set(0);
            })
        );
    }

    /**
     * Stops the SignalR connection.
     */
    async stopConnection() {
        if (this.hubConnection) {
            await this.hubConnection.stop();
            this.hubConnection = null;
        }
    }
}
