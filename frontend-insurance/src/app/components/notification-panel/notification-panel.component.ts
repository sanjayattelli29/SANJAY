import { Component, OnInit, inject, signal, HostListener, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, Notification } from '../../services/notification.service';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-notification-panel',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './notification-panel.component.html',
    styleUrl: './notification-panel.component.css'
})
export class NotificationPanelComponent implements OnInit {
    public notificationService = inject(NotificationService);
    private authService = inject(AuthService);
    private router = inject(Router);

    isOpen = signal(false);

    ngOnInit() {
        // Start connection if user is logged in
        const user = this.authService.getCurrentUser();
        if (user) {
            this.notificationService.startConnection();
        }
    }

    togglePanel() {
        this.isOpen.update(v => !v);
    }

    @HostListener('document:click', ['$event'])
    onDocumentClick(event: MouseEvent) {
        const target = event.target as HTMLElement;
        if (!target.closest('.notification-wrapper')) {
            this.isOpen.set(false);
        }
    }

    markAsRead(id: string, event: Event) {
        event.stopPropagation();
        this.notificationService.markAsRead(id).subscribe();
    }

    handleNotificationClick(notification: Notification, event: Event) {
        event.stopPropagation();

        // 1. Mark as read
        if (!notification.isRead) {
            this.notificationService.markAsRead(notification.id).subscribe();
        }

        // 2. Parse target and navigate
        const type = notification.notificationType || '';
        if (type.startsWith('Policy:')) {
            const policyId = type.split(':')[1];
            this.router.navigate(['/customer/policy', policyId]);
            this.isOpen.set(false);
        } else if (type.startsWith('Claim:')) {
            const claimId = type.split(':')[1];
            this.router.navigate(['/customer/claim', claimId]);
            this.isOpen.set(false);
        }
    }

    markAllAsRead() {
        this.notificationService.markAllAsRead().subscribe();
    }

    getRelativeTime(dateStr: string): string {
        const now = new Date();
        const date = new Date(dateStr);
        const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

        if (diffInSeconds < 60) return 'Just now';
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
        return date.toLocaleDateString();
    }

    getFormattedHeader(type: string): string {
        if (!type) return 'NOTIFICATION';
        if (type.startsWith('Policy:')) return 'POLICY UPDATE';
        if (type.startsWith('Claim:')) return 'CLAIM ALERT';
        if (type === 'PolicyApplication') return 'APPLICATION UPDATE';
        return type.toUpperCase();
    }
}
