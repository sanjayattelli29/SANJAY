import { Component, signal, computed, inject, OnInit, AfterViewInit } from '@angular/core';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PolicyService } from '../../services/policy.service';
import { ClaimService } from '../../services/claim.service';
import { ChatService } from '../../services/chat.service';

import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NotificationPanelComponent } from '../../components/notification-panel/notification-panel.component';
import { GooglePlacesInputComponent } from '../../components/incident-location/incident-location.component';
import { LocationMapComponent } from '../../components/location-map/location-map.component';
import { NomineeVerificationComponent } from './customer-components/nominee-verification/nominee-verification.component';
import { VoiceAgent } from './customer-components/voice-agent/voice-agent';
import { CustomerPart1Component } from './customer-components/part1/customer-part1';
import { CustomerPart2Component } from './customer-components/part2/customer-part2';
import { SafePipe } from '../../pipes/safe.pipe';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { environment } from '../../../environments/environment';

// customer dashboard main page component
// handles policy buying, claim raising, viewing policies and claims
// multi view single page component with lots of functionality
@Component({
    selector: 'app-customer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, NotificationPanelComponent, VoiceAgent, CustomerPart1Component, CustomerPart2Component],
    templateUrl: './customer-dashboard.page.html',
    styleUrls: ['./customer-dashboard.page.css']
})
export class CustomerDashboardPage implements OnInit, AfterViewInit {
    // inject all required services
    private authService = inject(AuthService);
    private policyService = inject(PolicyService);
    private claimService = inject(ClaimService);
    private chatService = inject(ChatService);
    private router = inject(Router);
    private http = inject(HttpClient);

    // get current logged in user from localstorage
    user = this.authService.getUser();
    // active view state for switching between sections
    activeView = signal<'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims' | 'policy-details' | 'claim-details' | 'chat' | 'kyc-verification' | 'profile'>('dashboard');
    // sidebar toggle for mobile
    sidebarOpen = signal<boolean>(false);

    // ─── Profile ─────────────────────────────────────────────────────────────────
    // load profile image: prefer CDN url saved on login, fall back to locally stored base64 (offline)
    profileImage = signal<string | null>(
        localStorage.getItem('user_profile_image_' + (this.authService.getUser().id || 'guest')) ||
        localStorage.getItem('profile_image_' + (this.authService.getUser().id || 'guest'))
    );
    profileIsUploading = signal<boolean>(false);
    profileUploadError = signal<string>('');
    profileLocationText = signal<string>('');
    profileMapUrl = signal<string>('');
    profileIsFetchingLocation = signal<boolean>(false);
    profileSaveSuccess = signal<boolean>(false);

    profileForm = {
        name:     this.authService.getUser().name || '',
        email:    this.authService.getUser().email || '',
        phone:    this.authService.getUser().phone || '',
        city:     localStorage.getItem('profile_city_' + (this.authService.getUser().id || 'guest')) || '',
        bio:      localStorage.getItem('profile_bio_' + (this.authService.getUser().id || 'guest')) || '',
        occupation: localStorage.getItem('profile_occupation_' + (this.authService.getUser().id || 'guest')) || '',
        bankAccount: localStorage.getItem('profile_bank_' + (this.authService.getUser().id || 'guest')) || '',
        ifscCode:   localStorage.getItem('profile_ifsc_' + (this.authService.getUser().id || 'guest')) || ''
    };

    // Bank validation helpers
    get isBankAccountValid(): boolean {
        const val = this.profileForm.bankAccount;
        return !val || /^\d{9,18}$/.test(val);
    }

    get isIfscValid(): boolean {
        const val = this.profileForm.ifscCode;
        return !val || /^[A-Z]{4}0[A-Z0-9]{6}$/.test(val);
    }

    // Profile image upload handler — uploads to ImageKit via backend and stores CDN url
    onProfileImageChange(event: Event) {
        const file = (event.target as HTMLInputElement).files?.[0];
        if (!file) return;
        if (file.size > 3 * 1024 * 1024) { alert('Image must be under 3 MB'); return; }

        this.profileIsUploading.set(true);
        this.profileUploadError.set('');

        const reader = new FileReader();
        reader.onload = () => {
            const base64 = reader.result as string;
            // Show preview instantly while uploading
            this.profileImage.set(base64);

            // Upload to ImageKit via backend
            this.authService.uploadProfileImage(
                this.user.id || 'guest',
                base64,
                file.name
            ).subscribe({
                next: (res) => {
                    // Replace preview base64 with the permanent CDN URL
                    this.profileImage.set(res.imageUrl);
                    localStorage.setItem('user_profile_image_' + (this.user.id || 'guest'), res.imageUrl);
                    this.profileIsUploading.set(false);
                },
                error: () => {
                    // Keep local preview if upload fails so user still sees their pic
                    localStorage.setItem('profile_image_' + (this.user.id || 'guest'), base64);
                    this.profileUploadError.set('Upload failed — saved locally.');
                    this.profileIsUploading.set(false);
                }
            });
        };
        reader.readAsDataURL(file);
    }

    // Remove profile image
    removeProfileImage() {
        this.profileImage.set(null);
        localStorage.removeItem('profile_image_' + (this.user.id || 'guest'));
        localStorage.removeItem('user_profile_image_' + (this.user.id || 'guest'));
    }

    // Fetch the user's current location via browser API and build a Google Maps embed URL
    fetchUserLocation() {
        if (!navigator.geolocation) { alert('Geolocation not supported by your browser.'); return; }
        this.profileIsFetchingLocation.set(true);
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const lat = pos.coords.latitude.toFixed(5);
                const lng = pos.coords.longitude.toFixed(5);
                const mapUrl = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;
                this.profileMapUrl.set(mapUrl);
                this.profileLocationText.set(`${lat}° N, ${lng}° E`);
                localStorage.setItem('profile_lat_' + this.user.id, lat);
                localStorage.setItem('profile_lng_' + this.user.id, lng);
                // Reverse geocode with free API
                fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`)
                    .then(r => r.json())
                    .then(data => {
                        const city = data.address?.city || data.address?.town || data.address?.village || '';
                        const state = data.address?.state || '';
                        const readable = [city, state].filter(Boolean).join(', ');
                        this.profileLocationText.set(readable || `${lat}° N, ${lng}° E`);
                        this.profileForm.city = readable;
                    })
                    .catch(() => {})
                    .finally(() => this.profileIsFetchingLocation.set(false));
            },
            () => {
                this.profileIsFetchingLocation.set(false);
                alert('Unable to fetch your location. Please allow location access.');
            }
        );
    }

    // Restore saved map from localStorage
    restoreSavedLocation() {
        const lat = localStorage.getItem('profile_lat_' + this.user.id);
        const lng = localStorage.getItem('profile_lng_' + this.user.id);
        if (lat && lng) {
            this.profileMapUrl.set(`https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`);
            this.profileLocationText.set(this.profileForm.city || `${lat}° N, ${lng}° E`);
        }
    }

    // Save editable profile fields to localStorage
    saveProfile() {
        if (!this.isBankAccountValid || !this.isIfscValid) {
            alert('Please correct the errors in Bank Details before saving.');
            return;
        }
        const uid = this.user.id || 'guest';
        localStorage.setItem('profile_city_' + uid, this.profileForm.city);
        localStorage.setItem('profile_bio_' + uid, this.profileForm.bio);
        localStorage.setItem('profile_occupation_' + uid, this.profileForm.occupation);
        localStorage.setItem('profile_bank_' + uid, this.profileForm.bankAccount);
        localStorage.setItem('profile_ifsc_' + uid, this.profileForm.ifscCode);
        this.profileSaveSuccess.set(true);
        setTimeout(() => this.profileSaveSuccess.set(false), 3000);
    }

    // Handles location search result from Google Places input
    onProfileLocationSelected(location: any) {
        if (!location) return;
        this.profileForm.city = location.formatted_address || location.name;
        if (location.geometry?.location) {
            const lat = typeof location.geometry.location.lat === 'function' ? location.geometry.location.lat() : location.geometry.location.lat;
            const lng = typeof location.geometry.location.lng === 'function' ? location.geometry.location.lng() : location.geometry.location.lng;
            this.profileMapUrl.set(`https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`);
            this.profileLocationText.set(this.profileForm.city);
            localStorage.setItem('profile_lat_' + this.user.id, lat.toString());
            localStorage.setItem('profile_lng_' + this.user.id, lng.toString());
        }
    }

    // Initials helper for default avatar
    get userInitials(): string {
        const n = this.user.name || 'User';
        return n.split(' ').map((p: string) => p[0]).slice(0, 2).join('').toUpperCase();
    }
    // ─── End Profile ──────────────────────────────────────────────────────────────

    // sub-view state for detail pages
    selectedPolicyId = signal<string | null>(null);
    selectedClaimId = signal<string | null>(null);
    detailedPolicy = signal<any | null>(null);
    detailedClaim = signal<any | null>(null);
    detailedClaimsForPolicy = signal<any[]>([]);
    isSubmittingClaim = signal<boolean>(false);
    isPaying = signal<boolean>(false);

    // prevents selecting future dates 
    today: string = new Date().toISOString().split('T')[0];

    // data arrays from backend
    config: any = null; // policy configuration
    myPolicies = signal<any[]>([]); // user's policies
    myClaims = signal<any[]>([]); // user's claims
    myChats = signal<any[]>([]); // chat rooms

    // calculated totals for dashboard cards
    totalCoverage = signal<number>(0);
    totalClaimsPaid = signal<number>(0);
    remainingBalance = signal<number>(0);
    requestedClaimAmount = signal<number>(0);
    approvedClaimAmount = signal<number>(0);

    // policy selection state for buy flow
    selectedCategory: any = null;
    selectedTier: any = null;

    // policy application form data
    applicationForm: any = {
        applicant: {
            fullName: '',
            age: 22,
            profession: '',
            alcoholHabit: 'Non Drinker',
            smokingHabit: 'Non Smoker',
            travelKmPerMonth: 0,
            vehicleType: 'None'
        },
        annualIncome: 0,
        paymentMode: 'yearly',
        nominee: {
            name: '',
            relationship: '',
            phone: '',
            email: '',
            bankAccount: '',
            ifsc: '',
            aadharNumber: '',
            aadharCardUrl: ''
        },
        location: {
            address: '',
            latitude: null,
            longitude: null,
            state: '',
            district: '',
            area: '',
            pincode: ''
        },
        familyMembers: [] // for family policies
    };

    // signal for policy application location coordinates
    selectedPolicyLocationCoords = signal<{ lat: number, lng: number } | null>(null);

    // store uploaded policy documents
    policyDocuments: { type: string, file: File, name: string }[] = [];
    isUploadingDocs = signal<boolean>(false);
    isExtractingAadhar = signal<boolean>(false);
    aadharSuccess = signal<boolean>(false);

    // calculated premium from backend
    calculatedPremium = signal<number>(0);
    isSubmitting = signal<boolean>(false);

    // Buy Policy Multi-Step Flow State
    buyFlowStep = signal<number>(1); // 1 = Form, 2 = Review/Timeline
    paymentTimeline = signal<any[]>([]);

    // ai chat helper state for policy assistance
    isChatOpen = signal<boolean>(false);
    chatMessages = signal<any[]>([]);
    currentChatPolicy = signal<any | null>(null);
    isChatLoading = signal<boolean>(false);
    chatUserMessage = signal<string>('');
    
    // voice agent chat extension state
    activeChatId = signal<string | null>(null);
    isVoiceMode = signal<boolean>(false);
    isVoiceProcessing = signal<boolean>(false);
    private currentAudio: HTMLAudioElement | null = null; // tracks playing ElevenLabs audio

    // policy detail modal legacy state
    showPolicyDetailModal = signal(false);
    showPaymentModal = signal(false);
    showKycModal = signal(false);
    showAppSuccessModal = signal(false);
    showClaimSuccessModal = signal(false);
    selectedPolicy = signal<any | null>(null);

    // KYC Verification State
    isKycVerified = signal<boolean>(false);
    kycStep = signal<number>(1);
    aadharFile = signal<File | null>(null);
    aadharPreview = signal<string | null>(null);
    aadharText = signal<string | null>(null);
    selfieFile = signal<File | null>(null);
    selfiePreview = signal<string | null>(null);
    isCameraActive = signal<boolean>(false);
    cameraStream = signal<MediaStream | null>(null);
    isKycVerifying = signal<boolean>(false);
    kycError = signal<string | null>(null);
    kycSuccessMsg = signal<string | null>(null);

    // Death Claim State
    isNomineeVerified = signal<boolean>(false);
    hasDeathClaim = signal<boolean>(false);
    nomineeAadharFile = signal<File | null>(null);
    nomineePhotoFile = signal<File | null>(null);
    nomineeAadharUrl = signal<string>('');
    nomineePhotoUrl = signal<string>('');

    // This function loads all required data when the component initializes including configuration, policies, claims and chat list from the backend services.
    ngOnInit() {
        // Prefill name if available
        if (this.user.name) {
            this.applicationForm.applicant.fullName = this.user.name;
        }

        const kycStatus = localStorage.getItem('isKycVerified_' + this.user.id);
        if (kycStatus === 'true') {
            this.isKycVerified.set(true);
        }

        this.loadConfig();
        this.loadMyPolicies();
        this.loadMyClaims();
        this.loadChatList();
        this.restoreSavedLocation();
    }

    // This function is called after Angular renders the view and is used to initialize the Chart.js charts on the dashboard.
    ngAfterViewInit() {
        // Wait a moment for @if blocks to be rendered
        setTimeout(() => this.renderDashboardCharts(), 300);
    }

    // Destroy and re-create all dashboard charts based on current data
    private chartInstances: { [key: string]: Chart } = {};

    renderDashboardCharts() {
        this.renderCoverageBarChart();
        this.renderClaimDonutChart();
        this.renderActivityLineChart();
    }

    private getOrCreateChart(id: string, config: any): Chart | null {
        const canvas = document.getElementById(id) as HTMLCanvasElement;
        if (!canvas) return null;
        if (this.chartInstances[id]) {
            this.chartInstances[id].destroy();
        }
        const chart = new Chart(canvas, config);
        this.chartInstances[id] = chart;
        return chart;
    }

    renderCoverageBarChart() {
        const activePolicies = this.myPolicies().filter((p: any) => p.status === 'Active');
        const labels = activePolicies.map((p: any) => p.tierId?.replace('IND_', '').replace('FAM_', '') || 'Policy');
        const coverageData = activePolicies.map((p: any) => p.totalCoverageAmount || 0);
        const claimedData = activePolicies.map((p: any) => {
            const policyClaims = this.myClaims().filter((c: any) => c.policyApplicationId === p.id && (c.status === 'Approved' || c.status === 'Paid'));
            return policyClaims.reduce((sum: number, c: any) => sum + (c.approvedAmount || 0), 0);
        });

        if (labels.length === 0) {
            labels.push('No Policy');
            coverageData.push(0);
            claimedData.push(0);
        }

        this.getOrCreateChart('coverageBarChart', {
            type: 'bar',
            data: {
                labels,
                datasets: [
                    { label: 'Total Coverage (₹)', data: coverageData, backgroundColor: 'rgba(15,31,20,0.85)', borderRadius: 8, borderSkipped: false },
                    { label: 'Amount Claimed (₹)', data: claimedData, backgroundColor: 'rgba(249,115,22,0.85)', borderRadius: 8, borderSkipped: false }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: true,
                plugins: { legend: { position: 'top', labels: { font: { family: 'Inter, sans-serif', weight: 'bold', size: 11 }, color: '#64748b' } } },
                scales: {
                    x: { grid: { display: false }, ticks: { font: { family: 'Inter', weight: 'bold', size: 10 }, color: '#94a3b8' } },
                    y: { grid: { color: '#f1f5f9' }, ticks: { font: { family: 'Inter', size: 10 }, color: '#94a3b8', callback: (v: any) => '₹' + (v / 100000).toFixed(0) + 'L' } }
                }
            }
        });
    }

    renderClaimDonutChart() {
        const approved = this.myClaims().filter((c: any) => c.status === 'Approved' || c.status === 'Paid').length;
        const processing = this.myClaims().filter((c: any) => c.status === 'Processing' || c.status === 'Pending' || c.status === 'UnderReview').length;
        const rejected = this.myClaims().filter((c: any) => c.status === 'Rejected').length;

        const hasData = approved + processing + rejected > 0;
        const data = hasData ? [approved, processing, rejected] : [1];
        const bgColors = hasData ? ['#10b981', '#f59e0b', '#ef4444'] : ['#e2e8f0'];
        const labels = hasData ? ['Approved / Paid', 'In Processing', 'Rejected'] : ['No Claims Yet'];
        const counts = hasData ? [approved, processing, rejected] : [0];

        // Build custom HTML legend on the right side
        const legendEl = document.getElementById('claimDonutLegend');
        if (legendEl) {
            const legendItems = hasData
                ? [
                    { color: '#10b981', label: 'Approved / Paid', count: approved },
                    { color: '#f59e0b', label: 'In Processing', count: processing },
                    { color: '#ef4444', label: 'Rejected', count: rejected }
                  ]
                : [{ color: '#e2e8f0', label: 'No Claims Yet', count: 0 }];

            legendEl.innerHTML = legendItems.map(item => `
                <div style="display:flex; align-items:center; justify-content:space-between; padding:10px 14px; background:#f8fafc; border-radius:12px; border:1px solid #f1f5f9;">
                    <div style="display:flex; align-items:center; gap:10px;">
                        <span style="display:inline-block; width:12px; height:12px; border-radius:50%; background:${item.color}; flex-shrink:0;"></span>
                        <span style="font-size:11px; font-weight:700; color:#475569; font-family:Inter,sans-serif;">${item.label}</span>
                    </div>
                    <span style="font-size:14px; font-weight:900; color:#0f1f14; font-family:Inter,sans-serif;">${item.count}</span>
                </div>
            `).join('');
        }

        this.getOrCreateChart('claimDonutChart', {
            type: 'doughnut',
            data: { labels, datasets: [{ data, backgroundColor: bgColors, borderWidth: 3, borderColor: '#fff', hoverBorderColor: '#fff', hoverOffset: 6 }] },
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '68%',
                plugins: {
                    legend: { display: false },
                    tooltip: { callbacks: { label: (ctx: any) => hasData ? ` ${ctx.label}: ${ctx.parsed} claim(s)` : ' No claims submitted' } }
                }
            }
        });
    }

    renderActivityLineChart() {
        // Build last 6 months labels
        const months: string[] = [];
        const now = new Date();
        for (let i = 5; i >= 0; i--) {
            const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
            months.push(d.toLocaleString('default', { month: 'short', year: '2-digit' }));
        }

        // count policies purchased per month
        const policyByMonth = months.map((_, idx) => {
            const d = new Date(now.getFullYear(), now.getMonth() - (5 - idx), 1);
            return this.myPolicies().filter((p: any) => {
                const pd = new Date(p.submissionDate);
                return pd.getFullYear() === d.getFullYear() && pd.getMonth() === d.getMonth();
            }).length;
        });

        // count claims raised per month
        const claimsByMonth = months.map((_, idx) => {
            const d = new Date(now.getFullYear(), now.getMonth() - (5 - idx), 1);
            return this.myClaims().filter((c: any) => {
                const cd = new Date(c.submissionDate);
                return cd.getFullYear() === d.getFullYear() && cd.getMonth() === d.getMonth();
            }).length;
        });

        this.getOrCreateChart('activityLineChart', {
            type: 'line',
            data: {
                labels: months,
                datasets: [
                    { label: 'Policies Started', data: policyByMonth, borderColor: '#0f1f14', backgroundColor: 'rgba(15,31,20,0.08)', fill: true, tension: 0.4, pointBackgroundColor: '#0f1f14', pointBorderColor: '#fff', pointBorderWidth: 2, pointRadius: 5, pointHoverRadius: 7 },
                    { label: 'Claims Raised', data: claimsByMonth, borderColor: '#800020', backgroundColor: 'rgba(128,0,32,0.08)', fill: true, tension: 0.4, pointBackgroundColor: '#800020', pointBorderColor: '#fff', pointBorderWidth: 2, pointRadius: 5, pointHoverRadius: 7 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: true,
                plugins: { legend: { position: 'top', labels: { font: { family: 'Inter', weight: 'bold', size: 11 }, color: '#64748b' } } },
                scales: {
                    x: { grid: { display: false }, ticks: { font: { family: 'Inter', weight: 'bold', size: 10 }, color: '#94a3b8' } },
                    y: { grid: { color: '#f1f5f9' }, beginAtZero: true, ticks: { precision: 0, font: { family: 'Inter', size: 10 }, color: '#94a3b8' } }
                }
            }
        });
    }

    // This function fetches the policy configuration data from the backend database which contains all available policy categories, tiers and benefits information.
    loadConfig() {
        this.policyService.getConfiguration().subscribe({
            next: (config) => {
                this.config = config;
                console.log('Policy Configuration loaded:', config);
            }
        });
    }

    // This function loads all policies associated with the current logged in user from the backend and recalculates the dashboard totals.
    loadMyPolicies() {
        this.policyService.getMyPolicies().subscribe({
            next: (policies) => {
                this.myPolicies.set(policies);
                this.calculateTotals(); // recalc dashboard totals
                console.log('User policies loaded:', policies);
                // Re-render charts with real data once loaded
                setTimeout(() => this.renderDashboardCharts(), 150);
            }
        });
    }

    // This function loads all insurance claims submitted by the current user from the backend using the claim service and updates the dashboard statistics.
    loadMyClaims() {
        this.claimService.getMyClaims().subscribe({
            next: (claims) => {
                this.myClaims.set(claims.map((c: any) => {
                    let details: any = null;
                    if (c.policy?.applicationDataJson) {
                        try {
                            const raw = JSON.parse(c.policy.applicationDataJson);
                            const normalize = (obj: any) => {
                                if (!obj) return null;
                                const normalized: any = {};
                                Object.keys(obj).forEach(key => {
                                    const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                                    normalized[normalizedKey] = obj[key];
                                });
                                return normalized;
                            };

                            const fullDetails = normalize(raw);
                            if (fullDetails) {
                                fullDetails.nominee = normalize(fullDetails.nominee || raw.Nominee) || {};
                                fullDetails.nominee.name = fullDetails.nominee.name || fullDetails.nominee.nomineeName || '--';
                                fullDetails.nominee.email = fullDetails.nominee.email || fullDetails.nominee.nomineeEmail || '--';
                                fullDetails.nominee.phone = fullDetails.nominee.phone || fullDetails.nominee.nomineePhone || '--';
                                fullDetails.nominee.bankAccount = fullDetails.nominee.bankAccount || fullDetails.nominee.nomineeBankAccountNumber || '--';
                                fullDetails.nominee.relationship = fullDetails.nominee.relationship || fullDetails.nominee.nomineeRelationship || '--';
                            }
                            details = fullDetails;
                        } catch (e) { }
                    }
                    return { ...c, fullDetails: details };
                }));
                this.calculateTotals(); // recalc totals
                
                // Check if user has any active/past death claim to show on dashboard
                const deathClaim = this.myClaims().find((c: any) => c.incidentType === 'Death' || (c.incidentDataJson && c.incidentDataJson.includes('Death')));
                if (deathClaim) {
                    this.hasDeathClaim.set(true);
                }

                console.log('User claims loaded:', this.myClaims());
                // Re-render charts with real data once loaded
                setTimeout(() => this.renderDashboardCharts(), 150);
            }
        });
    }

    // This function retrieves all active chat conversations between the customer and insurance agents from the backend chat service.
    loadChatList() {
        this.chatService.getChatList().subscribe({
            next: (chats) => this.myChats.set(chats),
            error: (err) => console.error('Failed to load chat list', err)
        });
    }

    // This function calculates the total coverage amount, total claims paid, remaining balance and claim amounts by aggregating data from all policies and claims.
    calculateTotals() {
        let coverage = 0;
        let claims = 0;
        let requested = 0;
        let approved = 0;

        // sum up coverage from active policies
        this.myPolicies().forEach(p => {
            if (p.status === 'Active') {
                coverage += p.totalCoverageAmount || 0;
            }
        });

        // sum up claim amounts by status
        this.myClaims().forEach(c => {
            requested += c.requestedAmount || 0;
            if (c.status === 'Approved' || c.status === 'Paid') {
                claims += c.approvedAmount || 0;
                approved += c.approvedAmount || 0;
            }
        });

        // update signals for reactive ui
        this.totalCoverage.set(coverage);
        this.totalClaimsPaid.set(claims);
        this.remainingBalance.set(coverage - claims);
        this.requestedClaimAmount.set(requested);
        this.approvedClaimAmount.set(approved);
    }

    // This function allows users to navigate between different sections of the dashboard like policies, claims, buy policy and chat by updating the active view.
    switchView(view: 'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims' | 'chat' | 'kyc-verification' | 'profile' | 'policy-details' | 'claim-details') {
        this.activeView.set(view);
        // reset selections when entering buy policy
        if (view === 'buy-policy') {
            this.selectedCategory = null;
            this.selectedTier = null;
        }
        // reload chats when entering chat view
        if (view === 'chat') {
            this.loadChatList();
        }
    }

    proceedToKyc() {
        this.showKycModal.set(false);
        this.switchView('kyc-verification');
    }

    // This function navigates to the chat page for a specific policy and initializes a new chat session with an agent if it doesn't already exist.
    navigateToChat(chat: any) {
        // if new chat init it first via backend
        if (chat.id && chat.id.startsWith('new_')) {
            const initData = {
                policyId: chat.policyId,
                customerId: chat.customerId,
                agentId: chat.agentId
            };
            this.chatService.initChat(initData).subscribe({
                next: (res) => {
                    this.router.navigate(['/chat', chat.policyId]);
                },
                error: (err) => alert('Failed to initialize chat: ' + (err.error?.message || 'Server error'))
            });
        } else {
            // existing chat just navigate
            this.router.navigate(['/chat', chat.policyId]);
        }
    }

    // This function checks whether the user already has an active policy in a specific category to prevent purchasing duplicate policies.
    hasActivePolicy(categoryId: string): boolean {
        return this.myPolicies().some(p => p.policyCategory === categoryId && p.status === 'Active');
    }

    // This function handles the user selection of a policy category such as individual or family and resets the tier selection and family members list.
    selectCategory(category: any) {
        this.selectedCategory = category;
        this.selectedTier = null;
        this.applicationForm.familyMembers = [];
    }

    // This function handles the user selection of a specific tier within the chosen policy category like silver, gold or platinum and triggers premium calculation.
    selectTier(tier: any) {
        if (!this.isKycVerified()) {
            this.showKycModal.set(true);
            return;
        }

        this.selectedTier = tier;

        // Prefill full name from user profile if not already set
        if (!this.applicationForm.applicant.fullName && this.user.name) {
            this.applicationForm.applicant.fullName = this.user.name;
        }

        this.updatePremium(); // calc premium when tier selected
    }

    // Family member management
    addFamilyMember() {
        this.applicationForm.familyMembers.push({
            fullName: '',
            relation: '',
            dateOfBirth: '',
            healthConditions: ''
        });
    }

    removeFamilyMember(index: number) {
        this.applicationForm.familyMembers.splice(index, 1);
    }

    // Nominee Aadhar management
    onNomineeAadharUpload(event: any) {
        const file = event.target.files[0];
        if (file) {
            this.aadharSuccess.set(false);
            // Upload to ImageKit service first, then extract via Vision API
            this.uploadNomineeAadharToImageKit(file);
        }
    }

    async extractNomineeAadharNumber(imageUrl: string) {
        this.isExtractingAadhar.set(true);
        try {
            console.log('[VisionAPI] Extracting from URL:', imageUrl);
            
            const res = await fetch(
                `https://vision.googleapis.com/v1/images:annotate?key=${environment.googleVisionApiKey}`,
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        requests: [{
                            image: { source: { imageUri: imageUrl } },
                            features: [{ type: 'TEXT_DETECTION' }]
                        }]
                    })
                }
            );

            const json = await res.json();
            const fullText = json.responses?.[0]?.fullTextAnnotation?.text || '';
            
            console.log('[VisionAPI] Full Text Result:', fullText);

            // Regex to find 12-digit Aadhar number
            // 1. Match XXXX XXXX XXXX (spaced)
            const spacedMatch = fullText.match(/\b\d{4}[\s]\d{4}[\s]\d{4}\b/);
            // 2. Or match a solid 12 digits block
            const solidMatch = fullText.replace(/\s/g, '').match(/\d{12}/);

            const raw = spacedMatch ? spacedMatch[0].replace(/\s/g, '') : solidMatch?.[0] ?? null;

            if (raw && raw.length === 12) {
                this.applicationForm.nominee.aadharNumber = raw;
                console.log('[VisionAPI] Successfully Extracted Aadhar:', raw);
            } else {
                console.warn('[VisionAPI] Could not detect a valid 12-digit Aadhar number.');
            }

        } catch (error) {
            console.error('[VisionAPI] Extraction Error:', error);
        } finally {
            this.isExtractingAadhar.set(false);
        }
    }

    async uploadNomineeAadharToImageKit(file: File) {
        try {
            this.isUploadingDocs.set(true);
            this.policyService.uploadDocument(file, 'nominee-aadhar').subscribe({
                next: (res) => {
                    this.applicationForm.nominee.aadharCardUrl = res.url;
                    this.isUploadingDocs.set(false);
                    this.aadharSuccess.set(true); // Show green success message
                    console.log('Nominee Aadhar uploaded successfully:', res.url);
                    
                    // Trigger Vision API extraction using the ImageKit URL
                    this.extractNomineeAadharNumber(res.url);
                },
                error: (err) => {
                    console.error('Error uploading Aadhar to ImageKit:', err);
                    this.isUploadingDocs.set(false);
                    this.aadharSuccess.set(false);
                    alert('Failed to upload Aadhar card. Please try again.');
                }
            });
        } catch (error) {
            console.error('Error in uploadNomineeAadharToImageKit:', error);
            this.isUploadingDocs.set(false);
        }
    }

    getNomineeAadharFileName(): string {
        if (this.applicationForm.nominee.aadharCardUrl) {
            // Extract filename from URL
            const parts = this.applicationForm.nominee.aadharCardUrl.split('/');
            return parts[parts.length - 1].replace(/^nominee_aadhar_\d+_/, '');
        }
        return '';
    }

    removeNomineeAadhar() {
        this.applicationForm.nominee.aadharCardUrl = '';
        this.aadharSuccess.set(false);
    }

    // calc premium when tier selected
    updatePremium() {
        if (!this.selectedCategory || !this.selectedTier) return;

        const request = {
            policyCategory: this.selectedCategory.categoryId,
            tierId: this.selectedTier.tierId,
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? { ...this.applicationForm.applicant, annualIncome: this.applicationForm.annualIncome } : null,
            primaryApplicant: this.selectedCategory.categoryId === 'FAMILY' ? { ...this.applicationForm.applicant, annualIncome: this.applicationForm.annualIncome } : null,
            familyMembers: this.applicationForm.familyMembers,
            paymentMode: this.applicationForm.paymentMode,
            annualIncome: this.applicationForm.annualIncome,
            vehicleType: this.applicationForm.applicant.vehicleType
        };

        this.policyService.calculatePremium(request).subscribe({
            next: (res) => this.calculatedPremium.set(res.premium)
        });
    }

    // Helper to get formatted payment intervals with interests info
    getPaymentIntervals() {
        if (!this.selectedTier) return [];

        return [
            {
                mode: 'monthly',
                title: 'Monthly Subscription',
                interest: 10, // Matches backend config 1.1 multiplier
                count: 12,
                label: 'Per Month',
                description: 'Convenient monthly payments with 10% processing fee.'
            },
            {
                mode: 'halfYearly',
                title: 'Bi-Annual Savings',
                interest: 5, // Matches backend config 1.05 multiplier
                count: 2,
                label: 'Every 6 Months',
                description: 'Pay twice a year. 5% convenience fee applied.'
            },
            {
                mode: 'yearly',
                title: 'Lump-Sum (Best Value)',
                interest: -5, // Matches backend config 0.95 multiplier (discount)
                count: 1,
                label: 'Pay Once / Year',
                description: 'Cleanest, most economical choice with 5% discount.'
            }
        ];
    }

    // Calc approximate payment amount for a mode based on current calculated premium
    getApproximatePayment(mode: string): number {
        const currentTotal = this.calculatedPremium();
        if (currentTotal <= 0) return 0;

        // Multipliers from backend config
        const multipliers: { [key: string]: number } = {
            'monthly': 1.1,
            'halfYearly': 1.05,
            'yearly': 0.95
        };

        const counts: { [key: string]: number } = {
            'monthly': 12,
            'halfYearly': 2,
            'yearly': 1
        };

        const currentMode = this.applicationForm.paymentMode || 'yearly';
        const currentMult = multipliers[currentMode] || 1.0;

        // 1. Find "Pure Base" (all risk factors, but 1.0 payment mult)
        const pureBase = currentTotal / currentMult;

        // 2. Derive target mode total
        const targetMult = multipliers[mode] || 1.0;
        const targetTotal = pureBase * targetMult;

        // 3. Divide by count
        return targetTotal / (counts[mode] || 1);
    }

    // Step navigation: Proceed to Addresses (Step 2)
    proceedToAddress() {
        if (!this.isStep1Valid()) {
            alert('Please fill all mandatory details (Applicant & Nominee) before proceeding.');
            return;
        }
        this.buyFlowStep.set(2);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Step navigation: Proceed to Payment (Step 3)
    proceedToPayment() {
        if (!this.isStep2Valid()) {
            alert('Please provide address and upload all required documents before proceeding.');
            return;
        }
        this.buyFlowStep.set(3);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Step navigation: Proceed to Review (Step 4)
    proceedToReview() {
        this.calculatePaymentTimeline();
        this.buyFlowStep.set(4);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Step navigation: Back to Step 1
    goBackToStep1() {
        this.buyFlowStep.set(1);
    }

    // Step navigation: Back to Step 2
    goBackToStep2() {
        this.buyFlowStep.set(2);
    }

    // Step navigation: Back to Step 3
    goBackToStep3() {
        this.buyFlowStep.set(3);
    }

    // Generates a 12-month schedule showing when payments occur
    calculatePaymentTimeline() {
        const months = [];
        const mode = this.applicationForm.paymentMode;
        const totalPremium = this.calculatedPremium();
        const basePeriodic = this.getApproximatePayment(mode);

        const startDate = new Date();

        for (let i = 0; i < 12; i++) {
            const currentDate = new Date(startDate);
            currentDate.setMonth(startDate.getMonth() + i);

            let isPaymentMonth = false;
            let amount = 0;

            if (mode === 'monthly') {
                isPaymentMonth = true;
                amount = basePeriodic;
            } else if (mode === 'halfYearly') {
                if (i === 0 || i === 6) {
                    isPaymentMonth = true;
                    amount = basePeriodic;
                }
            } else if (mode === 'yearly') {
                if (i === 0) {
                    isPaymentMonth = true;
                    amount = totalPremium;
                }
            }

            months.push({
                monthIndex: i + 1,
                date: currentDate,
                isPaymentMonth,
                amount,
                status: isPaymentMonth ? 'Payment Due' : 'Active Coverage'
            });
        }
        this.paymentTimeline.set(months);
    }

    // handle document selection for policy application

    // Generates a 12-month schedule for an active policy
    generatePolicyTimeline(pol: any) {
        if (!pol) return [];
        const months = [];
        const mode = pol.paymentMode || 'yearly';
        const totalPremium = pol.calculatedPremium || 0;

        let multiplier = 1.0;
        let count = 1;
        if (mode === 'monthly') { multiplier = 1.1; count = 12; }
        else if (mode === 'halfYearly') { multiplier = 1.05; count = 2; }
        else { multiplier = 0.95; count = 1; }

        const pureBase = totalPremium / multiplier;
        const basePeriodic = totalPremium / count;

        const startDate = pol.startDate ? new Date(pol.startDate) : new Date();

        for (let i = 0; i < 12; i++) {
            const currentDate = new Date(startDate);
            currentDate.setMonth(startDate.getMonth() + i);

            let isPaymentMonth = false;
            let amount = 0;

            if (mode === 'monthly') {
                isPaymentMonth = true;
                amount = basePeriodic;
            } else if (mode === 'halfYearly') {
                if (i === 0 || i === 6) {
                    isPaymentMonth = true;
                    amount = basePeriodic;
                }
            } else if (mode === 'yearly' || mode === 'Yearly') {
                if (i === 0) {
                    isPaymentMonth = true;
                    amount = totalPremium;
                }
            } else {
                // fallback if case mismatch
                if (i === 0) {
                    isPaymentMonth = true;
                    amount = totalPremium;
                }
            }

            months.push({
                monthIndex: i + 1,
                date: currentDate,
                isPaymentMonth,
                amount,
                status: isPaymentMonth ? 'Payment Due' : 'Active Coverage'
            });
        }
        return months;
    }

    onPolicyFileChange(event: any, type: string) {
        if (event.target.files.length > 0) {
            const file = event.target.files[0];

            // Only accept PDF
            if (file.type !== 'application/pdf') {
                alert('Only PDF documents are allowed for verification.');
                event.target.value = ''; // Reset input
                return;
            }

            // Remove existing doc of same type if any
            this.policyDocuments = this.policyDocuments.filter(d => d.type !== type);
            this.policyDocuments.push({ type, file, name: file.name });
            console.log(`Document added: ${type}`, file.name);
        }
    }

    // helper to check if a doc type is uploaded
    hasUploadedDocument(type: string): boolean {
        return this.policyDocuments.some(d => d.type === type);
    }

    // helper to get name of uploaded doc
    getUploadedFileName(type: string): string | null {
        const doc = this.policyDocuments.find(d => d.type === type);
        return doc ? doc.name : null;
    }

    // helper to remove uploaded doc
    removeDocument(type: string) {
        this.policyDocuments = this.policyDocuments.filter(d => d.type !== type);
        console.log(`Document removed: ${type}`);
    }

    // New Validation Methods
    isValidName(name: string): boolean {
        if (!name) return false;
        // Letters and spaces only
        return /^[a-zA-Z\s]*$/.test(name);
    }

    isValidEmail(email: string): boolean {
        if (!email) return false;
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    isValidIFSC(ifsc: string): boolean {
        if (!ifsc) return false;
        // IFSC: 4 uppercase letters, 0, then 6 alphanumeric
        return /^[A-Z]{4}0[A-Z0-9]{6}$/.test(ifsc);
    }

    isValidBankAccount(acc: string): boolean {
        if (!acc) return false;
        // Standard bank account: 9 to 18 digits 
        return /^\d{9,18}$/.test(acc);
    }

    isValidPhone(phone: string): boolean {
        if (!phone) return false;
        return /^[0-9]{10}$/.test(phone);
    }

    isValidAadhar(aadhar: string): boolean {
        if (!aadhar) return false;
        // Aadhar: exactly 12 digits
        return /^\d{12}$/.test(aadhar);
    }

    isStep1Valid(): boolean {
        const app = this.applicationForm;
        // Basic Applicant Validation
        if (!this.isValidName(app.applicant.fullName)) return false;
        if (app.applicant.age < 22) return false;
        if (app.annualIncome <= 0) return false;
        
        // Nominee Validation
        if (!app.nominee.name || !this.isValidName(app.nominee.name)) return false;
        if (!app.nominee.relationship) return false;
        if (!this.isValidEmail(app.nominee.email)) return false;
        if (!this.isValidPhone(app.nominee.phone)) return false;
        if (!this.isValidBankAccount(app.nominee.bankAccount)) return false;
        if (!this.isValidIFSC(app.nominee.ifsc)) return false;
        if (!this.isValidAadhar(app.nominee.aadharNumber)) return false;

        // Family Members Validation (if applicable)
        if (this.selectedCategory?.categoryId === 'FAMILY') {
            if (app.familyMembers.length === 0) return false;
            for (const member of app.familyMembers) {
                if (!this.isValidName(member.fullName) || !member.relation || !member.dateOfBirth) return false;
            }
        }
        return true;
    }

    isStep2Valid(): boolean {
        const app = this.applicationForm;
        // Address Validation
        if (!app.location.address || !app.location.pincode) return false;

        // Mandatory Documents Validation
        const mandatoryDocs = ['IdentityProof', 'AgeProof', 'IncomeProof', 'MedicalReport'];
        for (const type of mandatoryDocs) {
            if (!this.hasUploadedDocument(type)) return false;
        }
        return true;
    }

    isApplicationFormValid(): boolean {
        return this.isStep1Valid() && this.isStep2Valid();
    }

    // submit application and then upload files
    submitApplication() {
        if (this.isSubmitting()) return;

        this.isSubmitting.set(true);
        const request = {
            policyCategory: this.selectedCategory.categoryId,
            tierId: this.selectedTier.tierId,
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? this.applicationForm.applicant : null,
            primaryApplicant: this.selectedCategory.categoryId === 'FAMILY' ? this.applicationForm.applicant : null,
            familyMembers: this.applicationForm.familyMembers.map((fm: any) => ({
                ...fm,
                dateOfBirth: fm.dateOfBirth || this.today // fallback
            })),
            paymentMode: this.applicationForm.paymentMode,
            nominee: this.applicationForm.nominee,
            annualIncome: this.applicationForm.annualIncome,
            location: this.applicationForm.location,
            vehicleType: this.applicationForm.applicant.vehicleType
        };

        this.policyService.applyForPolicy(request).subscribe({
            next: (res) => {
                const applicationId = res.message; // Id returned from backend

                // If there are documents, upload them
                if (this.policyDocuments.length > 0) {
                    this.isUploadingDocs.set(true);
                    this.policyService.submitDocuments(applicationId, this.policyDocuments).subscribe({
                        next: () => {
                            this.isUploadingDocs.set(false);
                            this.isSubmitting.set(false);
                            this.showAppSuccessModal.set(true);
                            this.loadMyPolicies();
                        },
                        error: (err) => {
                            this.isUploadingDocs.set(false);
                            this.isSubmitting.set(false);
                            alert('Application submitted but document upload failed. You can upload them later from policy details.');
                            this.loadMyPolicies();
                            this.switchView('my-policies');
                        }
                    });
                } else {
                    this.isSubmitting.set(false);
                    this.showAppSuccessModal.set(true);
                    this.loadMyPolicies();
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                alert('Failed to submit application: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    closeAppSuccessModal() {
        this.showAppSuccessModal.set(false);
        this.switchView('my-policies');
    }

    // This function opens the detailed view of a specific policy by loading its complete information including associated claims from the database.
    openPolicyDetails(polId: string) {
        const pol = this.myPolicies().find(p => p.id === polId);
        if (!pol) return;

        // Parse JSON data robustly
        let raw: any = {};
        try {
            raw = typeof pol.applicationDataJson === 'string'
                ? JSON.parse(pol.applicationDataJson)
                : (pol.applicationDataJson || {});
        } catch (e) {
            raw = {};
        }

        const normalize = (obj: any) => {
            if (!obj) return null;
            const normalized: any = {};
            Object.keys(obj).forEach(key => {
                const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                normalized[normalizedKey] = obj[key];
            });
            return normalized;
        };

        const fullDetails = normalize(raw) || {};

        // Match the robust nominee mapping used in Admin/Agent
        const n = normalize(fullDetails.nominee || raw.Nominee) || {};
        fullDetails.nominee = {
            name: n.name || n.nomineeName || 'N/A',
            relationship: n.relationship || n.nomineeRelationship || '--',
            email: n.email || n.nomineeEmail || '--',
            phone: n.phone || n.nomineePhone || '--',
            bankAccount: n.bankAccount || n.nomineeBankAccountNumber || n.bankAccountNumber || '--',
            ifsc: n.ifsc || n.nomineeIfsc || '--',
            aadharNumber: n.aadharNumber || n.AadharNumber || '--',
            aadharCardUrl: n.aadharCardUrl || n.AadharCardUrl || null
        };

        // Enrich applicant
        let applicant = normalize(fullDetails.applicant || raw.Applicant) || {};
        if (!applicant.fullName) applicant.fullName = pol.user?.fullName || pol.user?.userName || 'N/A';

        applicant.age = applicant.age || raw.Age || pol.age || '--';
        applicant.profession = applicant.profession || raw.Profession || pol.profession || 'Standard';
        applicant.annualIncome = applicant.annualIncome || raw.AnnualIncome || pol.annualIncome || 0;
        applicant.alcoholHabit = applicant.alcoholHabit || raw.AlcoholHabit || pol.alcoholHabit || 'None';
        applicant.smokingHabit = applicant.smokingHabit || raw.SmokingHabit || pol.smokingHabit || 'None';
        applicant.vehicleType = applicant.vehicleType || raw.VehicleType || pol.vehicleType || 'None';
        applicant.travelKmPerMonth = applicant.travelKmPerMonth || raw.TravelKmPerMonth || pol.travelKmPerMonth || 0;

        fullDetails.applicant = applicant;

        // Ensure location is normalized with fallback to flat fields
        const loc = normalize(fullDetails.location || raw.Location || raw.location || {});
        fullDetails.location = {
            address: loc.address || pol.address || 'No address provided',
            latitude: loc.latitude || pol.latitude || null,
            longitude: loc.longitude || pol.longitude || null,
            state: loc.state || pol.state || '',
            district: loc.district || pol.district || '',
            pincode: loc.pincode || pol.pincode || ''
        };

        pol.fullDetails = fullDetails;

        // Enrich from config if available
        if (this.config) {
            const cat = this.config.policyCategories?.find((c: any) => c.categoryId === pol.policyCategory);
            const tier = cat?.tiers?.find((t: any) => t.tierId === pol.tierId);
            pol.coverageAmount = tier?.baseCoverageAmount || (pol.totalCoverageAmount || pol.sumInsured || 0);
            pol.policyName = tier?.tierName || pol.tierId;
            pol.basePremiumAmount = tier?.basePremiumAmount || 0;
        }

        // Add monthly premium
        pol.monthlyPremium = (pol.calculatedPremium || 0) / 12;

        this.detailedPolicy.set({ ...pol });
        this.detailedClaimsForPolicy.set(this.myClaims().filter(c => c.policyApplicationId === polId));
        this.selectedPolicyId.set(polId);
        this.activeView.set('policy-details');
    }


    // This function processes the premium payment for a policy from the detailed policy view and activates the policy upon successful payment.
    payPremiumFromDetails() {
        const pol = this.detailedPolicy();
        if (!pol) return;

        if (!confirm(`Confirm payment of ₹${pol.calculatedPremium} for ${pol.tierId} policy?`)) return;

        this.executePayment();
    }

    // New consolidated payment execution for the modal
    executePayment() {
        const pol = this.detailedPolicy();
        if (!pol) return;

        this.isPaying.set(true);
        this.policyService.processPayment(pol.id, pol.calculatedPremium).subscribe({
            next: () => {
                this.isPaying.set(false);
                this.showPaymentModal.set(false);

                // ✅ Send invoice email via n8n
                this.sendInvoiceEmail(pol);

                alert('Payment Successful! Your policy is now ACTIVE. Invoice sent to your email.');
                
                // Refresh policies and navigate to the detailed view of the paid policy
                this.policyService.getMyPolicies().subscribe((policies) => {
                    this.myPolicies.set(policies);
                    this.calculateTotals();
                    this.openPolicyDetails(pol.id);
                });
            },
            error: (err) => {
                this.isPaying.set(false);
                const errorMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.message || '');
                // If it's a backend mismatch but payment succeeded, or if it explicitly says AwaitingPayment
                // Also bypass 500 timeout from N8N webhooks (shows as 'unexpected error') since the DB tx works
                if (errorMsg.includes('status') || errorMsg.includes('AwaitingPayment') || errorMsg.includes('unexpected error') || errorMsg.includes('An unexpected error')) {
                    this.showPaymentModal.set(false);
                    // ✅ Send invoice email even on pseudo-error if it means success
                    this.sendInvoiceEmail(pol);
                    
                    alert('Payment processed. Your policy is now ACTIVE. Invoice sent to your email.');

                    // Refresh policies and navigate to the detailed view of the paid policy
                    this.policyService.getMyPolicies().subscribe((policies) => {
                        this.myPolicies.set(policies);
                        this.calculateTotals();
                        this.openPolicyDetails(pol.id);
                    });
                } else {
                    this.showPaymentModal.set(false);
                    alert('Payment failed: ' + errorMsg);
                }
            }
        });
    }

    // This function opens the detailed view of a specific insurance claim showing all information about the claim including status and associated policy details.
    openClaimDetails(claimId: string) {
        const claim = this.myClaims().find(c => c.id === claimId);
        if (!claim) return;

        // Parse policy json to get nominee info
        let details: any = null;
        if (claim.policy?.applicationDataJson) {
            try {
                const raw = JSON.parse(claim.policy.applicationDataJson);
                const normalize = (obj: any) => {
                    if (!obj) return null;
                    const normalized: any = {};
                    Object.keys(obj).forEach(key => {
                        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                        normalized[normalizedKey] = obj[key];
                    });
                    return normalized;
                };

                const fullDetails = normalize(raw);
                if (fullDetails) {
                    fullDetails.nominee = normalize(fullDetails.nominee || raw.Nominee) || {};
                    // ensure keys are safe
                    fullDetails.nominee.name = fullDetails.nominee.name || fullDetails.nominee.nomineeName || '--';
                    fullDetails.nominee.email = fullDetails.nominee.email || fullDetails.nominee.nomineeEmail || '--';
                    fullDetails.nominee.phone = fullDetails.nominee.phone || fullDetails.nominee.nomineePhone || '--';
                    fullDetails.nominee.bankAccount = fullDetails.nominee.bankAccount || fullDetails.nominee.nomineeBankAccountNumber || '--';
                    fullDetails.nominee.relationship = fullDetails.nominee.relationship || fullDetails.nominee.nomineeRelationship || '--';
                }
                details = fullDetails;
            } catch (e) { }
        }

        this.detailedClaim.set({ ...claim, fullDetails: details });
        this.selectedClaimId.set(claimId);
        this.activeView.set('claim-details');
    }

    // This function processes the premium payment for a selected policy from the modal view and closes the modal after successful payment.
    payPremium() {
        const pol = this.selectedPolicy();
        if (!pol) return;

        if (!confirm(`Confirm payment of ₹${pol.calculatedPremium} for ${pol.tierId} policy?`)) return;

        this.isPaying.set(true);
        this.policyService.processPayment(pol.id, pol.calculatedPremium).subscribe({
            next: (res) => {
                this.isPaying.set(false);
                this.showPolicyDetailModal.set(false);

                // ✅ Send invoice email via n8n
                this.sendInvoiceEmail(pol);

                alert('Payment Successful! Your policy is now ACTIVE. Invoice sent to your email.');
                
                // Refresh policies and navigate to the detailed view of the paid policy
                this.policyService.getMyPolicies().subscribe((policies) => {
                    this.myPolicies.set(policies);
                    this.calculateTotals();
                    this.openPolicyDetails(pol.id);
                });
            },
            error: (err) => {
                this.isPaying.set(false);
                const errorMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.message || '');
                if (errorMsg.includes('status') || errorMsg.includes('AwaitingPayment') || errorMsg.includes('unexpected error') || errorMsg.includes('An unexpected error')) {
                    
                    // ✅ Send invoice email even on pseudo-error if it means success
                    this.sendInvoiceEmail(pol);
                    
                    alert('Payment processed. Your policy is now ACTIVE. Invoice sent to your email.');

                    // Refresh policies and navigate to the detailed view of the paid policy
                    this.policyService.getMyPolicies().subscribe((policies) => {
                        this.myPolicies.set(policies);
                        this.calculateTotals();
                        this.openPolicyDetails(pol.id);
                    });
                } else {
                    alert('Payment failed: ' + errorMsg);
                }
            }
        });
    }

    // claim raising section
    selectedPolicyForClaim = signal<any | null>(null);
    nomineeData = computed(() => {
        const policy = this.selectedPolicyForClaim();
        if (!policy) return null;
        
        let raw: any = {};
        try {
            raw = typeof policy.applicationDataJson === 'string'
                ? JSON.parse(policy.applicationDataJson)
                : (policy.applicationDataJson || {});
        } catch (e) { }

        const n = raw.nominee || raw.Nominee || {};
        return {
            name: n.name || n.nomineeName || 'N/A',
            relationship: n.relationship || n.nomineeRelationship || '--',
            email: n.email || n.nomineeEmail || '--',
            phone: n.phone || n.nomineePhone || '--',
            bankAccount: n.bankAccount || n.nomineeBankAccountNumber || n.bankAccountNumber || '--',
            ifsc: n.ifsc || n.nomineeIfsc || '--',
            aadharNumber: n.aadharNumber || n.AadharNumber || '--',
            aadharCardUrl: n.aadharCardUrl || n.AadharCardUrl || null
        };
    });
    claimStep = signal<number>(1);

    // claim form data
    claimForm: any = {
        incidentDate: '',
        incidentTime: '',
        incidentType: 'Accidental Injury',
        accidentCause: 'Vehicle Accident',
        policeCaseFiled: false,
        firNumber: '',
        incidentLocation: '',
        description: '',
        hospitalName: '',
        hospitalizationRequired: false,
        admissionDate: '',
        dischargeDate: '',
        injuryType: 'Minor Injury',
        bodyPartInjured: 'Head',
        estimatedMedicalCost: 0,
        hospitalBill: 0,
        medicines: 0,
        otherExpenses: 0,
        requestedAmount: 0,
        affectedMemberName: '',
        affectedMemberRelation: ''
    };
    claimFiles: File[] = []; // uploaded documents
    hasFirReport = signal<boolean>(false);
    hasHospitalBill = signal<boolean>(false);
    hasDeathCertificate = signal<boolean>(false);
    selectedLocationCoords = signal<{ lat: number, lng: number } | null>(null);
    selectedHospitalCoords = signal<{ lat: number, lng: number } | null>(null);
    selectedHospitalDetails = signal<any>(null);

    // handles event from Nominee Verification component
    onNomineeVerified(event: {aadhar: File | null, photo: File, aadharUrl?: string, photoUrl?: string}) {
        this.nomineeAadharFile.set(event.aadhar);
        this.nomineePhotoFile.set(event.photo);
        this.nomineeAadharUrl.set(event.aadharUrl || '');
        this.nomineePhotoUrl.set(event.photoUrl || '');
        this.isNomineeVerified.set(true);
    }

    // this function initializes the claim submission form for a selected policy by resetting all form fields and navigating to the raise claim view.
    initiateClaim(pol: any) {
        this.selectedPolicyForClaim.set(pol);
        this.claimForm = {
            incidentDate: '',
            incidentTime: '',
            incidentType: 'Accidental Injury',
            accidentCause: 'Vehicle Accident',
            policeCaseFiled: false,
            firNumber: '',
            incidentLocation: '',
            description: '',
            hospitalName: '',
            hospitalizationRequired: false,
            admissionDate: '',
            dischargeDate: '',
            injuryType: 'Minor Injury',
            bodyPartInjured: 'Head',
            estimatedMedicalCost: 0,
            hospitalBill: 0,
            medicines: 0,
            otherExpenses: 0,
            requestedAmount: 0,
            affectedMemberName: '',
            affectedMemberRelation: ''
        };
        this.claimStep.set(1);
        this.claimFiles = [];
        this.hasFirReport.set(false);
        this.hasHospitalBill.set(false);
        this.hasDeathCertificate.set(false);
        this.isNomineeVerified.set(false);
        this.nomineeAadharFile.set(null);
        this.nomineePhotoFile.set(null);
        this.selectedLocationCoords.set(null);
        this.switchView('raise-claim');
    }

    get hospitalDays(): number {
        if (!this.claimForm.admissionDate || !this.claimForm.dischargeDate) return 0;
        const d1 = new Date(this.claimForm.admissionDate);
        const d2 = new Date(this.claimForm.dischargeDate);
        const diffTime = Math.abs(d2.getTime() - d1.getTime());
        return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    }

    calculateTotalMedicalCost() {
        this.claimForm.estimatedMedicalCost = (this.claimForm.hospitalBill || 0) + (this.claimForm.medicines || 0) + (this.claimForm.otherExpenses || 0);
    }

    get suggestedClaimAmount(): number {
        return this.claimForm.estimatedMedicalCost * 0.9;
    }

    populateSuggestedAmount() {
        this.claimForm.requestedAmount = this.suggestedClaimAmount;
    }

    get isClaimDataComplete(): boolean {
        if (this.claimForm.incidentType === 'Death') {
            return this.hasHospitalBill() && this.hasFirReport() && this.hasDeathCertificate() && this.isNomineeVerified();
        }
        return this.hasHospitalBill() && this.claimForm.hospitalName !== '';
    }

    // this function handles the file upload event when users select supporting documents like medical reports and bills for their insurance claims.
    onFileChange(event: any, type: 'fir' | 'bill' | 'death' | 'others' = 'others') {
        if (event.target.files.length > 0) {
            const files = Array.from(event.target.files) as File[];
            this.claimFiles = [...this.claimFiles, ...files];

            if (type === 'fir') this.hasFirReport.set(true);
            if (type === 'bill') this.hasHospitalBill.set(true);
            if (type === 'death') this.hasDeathCertificate.set(true);
        }
    }

    // This function submits a new insurance claim to the backend by sending the claim form data along with uploaded supporting documents as multipart form data.
    submitClaim() {
        const pol = this.selectedPolicyForClaim();
        if (!pol) return;

        // Validation: Incident date cannot be before policy start date
        if (pol.startDate && this.claimForm.incidentDate) {
            const start = new Date(pol.startDate);
            const incident = new Date(this.claimForm.incidentDate);
            // reset hours to compare only dates
            start.setHours(0, 0, 0, 0);
            incident.setHours(0, 0, 0, 0);

            if (incident < start) {
                alert(`Cannot raise a claim for an incident before the policy start date (${start.toLocaleDateString()}).`);
                return;
            }
        }

        this.isSubmitting.set(true);
        // build formdata for multipart upload with files
        const formData = new FormData();
        formData.append('policyApplicationId', this.selectedPolicyForClaim()!.id);
        formData.append('incidentDate', this.claimForm.incidentDate);
        formData.append('incidentTime', this.claimForm.incidentTime || '');
        formData.append('incidentType', this.claimForm.incidentType);
        formData.append('accidentCause', this.claimForm.accidentCause);
        formData.append('policeCaseFiled', this.claimForm.policeCaseFiled.toString());
        formData.append('firNumber', this.claimForm.firNumber || '');
        formData.append('incidentLocation', this.claimForm.incidentLocation);

        const coords = this.selectedLocationCoords();
        if (coords) {
            formData.append('latitude', coords.lat.toString());
            formData.append('longitude', coords.lng.toString());
        }

        formData.append('description', this.claimForm.description);
        formData.append('hospitalName', this.claimForm.hospitalName);
        formData.append('hospitalizationRequired', this.claimForm.hospitalizationRequired.toString());
        formData.append('admissionDate', this.claimForm.admissionDate || '');
        formData.append('dischargeDate', this.claimForm.dischargeDate || '');
        formData.append('injuryType', this.claimForm.injuryType);
        formData.append('bodyPartInjured', this.claimForm.bodyPartInjured);
        formData.append('estimatedMedicalCost', this.claimForm.estimatedMedicalCost.toString());
        formData.append('hospitalBill', this.claimForm.hospitalBill.toString());
        formData.append('medicines', this.claimForm.medicines.toString());
        formData.append('otherExpenses', this.claimForm.otherExpenses.toString());
        formData.append('requestedAmount', this.claimForm.requestedAmount.toString());

        // add family member info if family policy
        if (this.selectedPolicyForClaim()!.policyCategory === 'FAMILY') {
            formData.append('affectedMemberName', this.claimForm.affectedMemberName);
            formData.append('affectedMemberRelation', this.claimForm.affectedMemberRelation);
        }

        // attach uploaded files
        this.claimFiles.forEach(file => {
            formData.append('documents', file, file.name);
        });

        // Add Nominee Verification documents if Death claim
        if (this.claimForm.incidentType === 'Death') {
            if (this.nomineeAadharFile()) {
                formData.append('documents', this.nomineeAadharFile()!, 'Nominee_Aadhar.jpg');
            }
            if (this.nomineePhotoFile()) {
                formData.append('documents', this.nomineePhotoFile()!, 'Nominee_Photo.jpg');
            }
        }

        // post to backend which saves claim and files to db
        this.claimService.raiseClaim(formData).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.showClaimSuccessModal.set(true);
                this.loadMyClaims(); // refresh claims list
            },
            error: (err) => {
                this.isSubmitting.set(false);
                alert('Failed to raise claim: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    closeClaimSuccessModal() {
        this.showClaimSuccessModal.set(false);
        this.switchView('my-claims');
    }

    // This function handles the location selection from the Google Places autocomplete and updates the incident location with address and coordinates.
    onLocationSelected(data: any) {
        if (typeof data === 'string') {
            this.claimForm.incidentLocation = data;
        } else {
            this.claimForm.incidentLocation = data.address;
            this.selectedLocationCoords.set({ lat: data.lat, lng: data.lng });
        }
        console.log('Location updated in dashboard form:', data);
    }

    getMinDateForClaim(): string {
        const pol = this.selectedPolicyForClaim();
        if (!pol || !pol.startDate) return '';
        const d = new Date(pol.startDate);
        return d.toISOString().split('T')[0];
    }

    // Handles map location selection for policy buying flow
    onPolicyLocationSelected(data: any) {
        if (typeof data === 'string') {
            this.applicationForm.location.address = data;
        } else {
            this.applicationForm.location.address = data.address;
            this.applicationForm.location.latitude = data.lat;
            this.applicationForm.location.longitude = data.lng;
            this.selectedPolicyLocationCoords.set({ lat: data.lat, lng: data.lng });

            // Set sliced components if available
            if (data.components) {
                this.applicationForm.location.state = data.components.state || '';
                this.applicationForm.location.district = data.components.district || '';
                this.applicationForm.location.area = data.components.area || '';
                this.applicationForm.location.pincode = data.components.pincode || '';
            }
        }
        console.log('Policy location updated:', data);
    }

    // This function updates the hospital name in the claim form when the user selects a hospital from the autocomplete dropdown.
    onHospitalChanged(data: any) {
        if (typeof data === 'string') {
            this.claimForm.hospitalName = data;
            this.selectedHospitalDetails.set(null);
        } else {
            this.claimForm.hospitalName = data.address;
            this.selectedHospitalCoords.set({ lat: data.lat, lng: data.lng });
            this.selectedHospitalDetails.set(data.components || null);
        }
        console.log('Hospital updated with details:', data);
    }

    // Generates a PDF invoice locally, uploads to ImageKit, then sends email via n8n
    private sendInvoiceEmail(pol: any) {
        const user = this.authService.getUser();
        const doc = new jsPDF();
        
        // 1. Generate PDF Content
        doc.setFontSize(22);
        doc.text('ACCISURE INSURANCE INVOICE', 105, 20, { align: 'center' });
        doc.setFontSize(10);
        doc.text(`Date: ${new Date().toLocaleDateString()}`, 105, 28, { align: 'center' });
        
        doc.line(20, 35, 190, 35);
        
        doc.setFontSize(12);
        doc.text('Customer Details:', 20, 45);
        doc.setFontSize(10);
        doc.text(`Name: ${user.name || 'Valued Customer'}`, 20, 52);
        doc.text(`Email: ${user.email}`, 20, 58);
        
        doc.setFontSize(12);
        doc.text('Policy Information:', 120, 45);
        doc.setFontSize(10);
        doc.text(`Policy ID: ${pol.id}`, 120, 52);
        doc.text(`Plan: ${pol.tierId || pol.policyName}`, 120, 58);
        doc.text(`Category: ${pol.policyCategory || 'Standard'}`, 120, 64);

        autoTable(doc, {
            startY: 80,
            head: [['Description', 'Amount']],
            body: [
                ['Initial Premium Payment', `INR ${pol.calculatedPremium}`],
                ['Service Tax (GST)', 'Included'],
            ],
            foot: [['Total Paid', `INR ${pol.calculatedPremium}`]],
            theme: 'striped',
            headStyles: { fillColor: [1, 33, 67] }
        });

        // 2. Convert to Base64 and Upload
        const pdfBase64 = doc.output('datauristring');
        
        this.policyService.uploadInvoice(pol.id, pdfBase64, `Invoice_${pol.id}.pdf`).subscribe({
            next: (uploadRes) => {
                const permanentUrl = uploadRes.invoiceUrl;
                
                // 3. Send payload to n8n with working link
                const payload = {
                    customerEmail: user.email,
                    customerName: user.name || user.email,
                    phoneNumber: user.phone || 'Not Provided',
                    policyId: pol.id,
                    policyName: pol.tierId || pol.policyName || 'Accidental Insurance Policy',
                    amount: pol.calculatedPremium,
                    paymentDate: new Date().toLocaleDateString('en-IN', {
                        day: '2-digit', month: 'short', year: 'numeric'
                    }),
                    invoiceLink: permanentUrl
                };
                this.http.post('https://nextglidesol.app.n8n.cloud/webhook/send-invoice', payload)
                    .subscribe({
                        next: (res) => console.log('Invoice email sent with working link:', res),
                        error: (err) => console.error('Invoice email failed:', err)
                    });
            },
            error: (uploadErr) => {
                console.error('Invoice PDF upload failed. Sending email with fallback.', uploadErr);
                // Fallback to sending email even if upload failed (link might be broken but email gets through)
                this.http.post('https://nextglidesol.app.n8n.cloud/webhook/send-invoice', {
                    customerEmail: user.email,
                    customerName: user.name || user.email,
                    phoneNumber: user.phone || 'Not Provided',
                    policyId: pol.id,
                    invoiceLink: `https://ik.imagekit.io/nextbyteind/invoices/Invoice_${pol.id}.pdf`
                }).subscribe();
            }
        });
    }

    // This function logs out the current user by calling the authentication service logout method which clears the session and redirects to login page.
    logout() {
        this.authService.logout();
    }

    // Exposes this component instance to child partial components
    get self(): this { return this; }

    // This function opens the AI chat assistant helper for a specific policy tier and sends an initial greeting message to start the conversation.
    openChatHelper(tier: any) {
        if (!this.selectedCategory) return;

        const policyData = {
            policyId: tier.tierId,
            policyName: tier.tierName,
            category: this.selectedCategory.categoryName,
            coverageAmount: tier.baseCoverageAmount,
            premium: tier.basePremiumAmount,
            benefits: tier.benefits
        };

        this.currentChatPolicy.set(policyData);
        this.chatMessages.set([]);
        this.isChatOpen.set(true);
        this.isChatLoading.set(true);

        // Initial trigger
        const initialText = "Hi"; // To trigger the first response from AI
        this.sendChatMessage(initialText, true);
    }

    // This function sends a chat message to the AI policy assistant by posting the customer details, policy information and message to the backend AI service.
    sendChatMessage(messageText?: string, isInitial: boolean = false) {
        const text = messageText || this.chatUserMessage();
        if (!text && !isInitial) return;

        if (!isInitial) {
            this.chatMessages.update((msgs: any[]) => [...msgs, { role: 'user', content: text }]);
            this.chatUserMessage.set('');
        }

        this.isChatLoading.set(true);

        // Fetch fresh user data to ensure all fields are current
        const freshUser = this.authService.getUser();

        const payload = {
            customer: {
                id: freshUser.id,
                name: freshUser.name,
                email: freshUser.email,
                phone: freshUser.phone
            },
            policy: this.currentChatPolicy(),
            message: text,
            question: text // Included as a fallback for n8n AI Agent nodes
        };

        console.log('[ChatHelper] Sending payload to n8n:', JSON.stringify(payload));

        this.policyService.sendChatQuestion(payload).subscribe({
            next: (res) => {
                this.isChatLoading.set(false);


                // const aiReply = res.reply || res.answer || "I'm' sorry, I couldn't get a response. Please try again.";
                let aiReply = res.reply || res.answer ||
                    "I'm sorry, I couldn't get a response. Please try again.";

                // Remove markdown special characters like *, -, _, `, #
                aiReply = aiReply
                    .replace(/[*_`#>-]/g, '')     // remove markdown symbols
                    .replace(/\n{2,}/g, '\n')     // remove extra line breaks
                    .trim();

                this.chatMessages.update((msgs: any[]) => [
                    ...msgs,
                    { role: 'bot', content: aiReply }
                ]);



                // this.chatMessages.update((msgs: any[]) => [...msgs, { role: 'bot', content: aiReply }]);

                // Scroll to bottom logic would go here if using ViewChild
            },
            error: (err) => {
                this.isChatLoading.set(false);
                this.chatMessages.update((msgs: any[]) => [...msgs, { role: 'bot', content: "Error: Could not reach the policy assistant." }]);
            }
        });
    }

    // This function closes the AI chat assistant dialog by setting the chat open state to false.
    closeChat() {
        this.isChatOpen.set(false);
    }

    // KYC Logic
    onAadharFileChange(event: any) {
        if (event.target.files.length > 0) {
            const file = event.target.files[0];
            this.aadharFile.set(file);
            const reader = new FileReader();
            reader.onload = async () => {
                this.aadharPreview.set(reader.result as string);

                // Extract text details via Google Vision API
                this.aadharText.set("Extracting text details directly from your document... Please wait...");
                try {
                    const base64Content = (reader.result as string).split(',')[1];
                    const response = await fetch(
                        `https://vision.googleapis.com/v1/images:annotate?key=${environment.googleVisionApiKey}`,
                        {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                requests: [{
                                    image: { content: base64Content },
                                    features: [{ type: 'TEXT_DETECTION' }]
                                }]
                            })
                        }
                    );

                    const json = await response.json();
                    const fullText = json.responses?.[0]?.fullTextAnnotation?.text || '';
                    console.log('[KYC Vision] Result:', fullText);

                    if (fullText && fullText.trim().length > 0) {
                        let parsedDetails = [];
                        const lines = fullText.split('\n').filter((l: { trim: () => { (): any; new(): any; length: number; }; }) => l.trim().length > 0);
                        
                        // Extract Name heuristic
                        const dobIndex = lines.findIndex((l: string) => /DOB|Year of Birth/i.test(l));
                        if (dobIndex > 0) {
                            let possibleName = lines[dobIndex - 1].trim();
                            possibleName = possibleName.replace(/^[^a-zA-Z]+/, '').trim();
                            if (possibleName.length > 3) parsedDetails.push(`Name: ${possibleName}`);
                        }

                        // Extract DOB
                        const dobMatch = fullText.match(/DOB[:\.\s]*(\d{2}\/\d{2}\/\d{4})/i) || fullText.match(/Year of Birth.*(\d{4})/i);
                        if (dobMatch) parsedDetails.push(`Date of Birth: ${dobMatch[1]}`);

                        // Extract 12-digit Aadhar pattern
                        const aadharMatch = fullText.match(/\d{4}[\s-]+\d{4}[\s-]+\d{4}/) || fullText.match(/\d{12}/);
                        if (aadharMatch) parsedDetails.push(`Aadhar Number: ${aadharMatch[0].trim()}`);

                        if (parsedDetails.length > 0) {
                            this.aadharText.set(`Key Details Extracted Successfully!\n\n${parsedDetails.join('\n')}\n\nNote: These details are for verification convenience.`);
                        } else {
                            this.aadharText.set(`Key details could not be parsed automatically.\n\nRaw Text Start:\n${fullText.substring(0, 100)}...`);
                        }
                    } else {
                        this.aadharText.set("Could not automatically extract readable text. Please ensure the image is clear.");
                    }
                } catch (e) {
                    console.error("KYC Vision API failed:", e);
                    this.aadharText.set("Extraction temporarily unavailable. Verification will proceed via face recognition.");
                }
            };
            reader.readAsDataURL(file);
        }
    }

    async activateCamera() {
        this.isCameraActive.set(true);
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            this.cameraStream.set(stream);
            setTimeout(() => {
                const video = document.getElementById('camera-video') as HTMLVideoElement;
                if (video) video.srcObject = stream;
            }, 100);
        } catch (err) {
            console.error('Error accessing camera:', err);
            alert('Could not access camera.');
        }
    }

    takePicture() {
        const video = document.getElementById('camera-video') as HTMLVideoElement;
        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d')?.drawImage(video, 0, 0);

        canvas.toBlob((blob) => {
            if (blob) {
                const file = new File([blob], 'selfie.jpg', { type: 'image/jpeg' });
                this.selfieFile.set(file);
                this.selfiePreview.set(URL.createObjectURL(blob));
                this.stopCamera();
            }
        }, 'image/jpeg');
    }

    stopCamera() {
        if (this.cameraStream()) {
            this.cameraStream()?.getTracks().forEach(track => track.stop());
            this.cameraStream.set(null);
        }
        this.isCameraActive.set(false);
    }

    async verifyKyc() {
        if (!this.aadharFile() || !this.selfieFile()) return;

        this.isKycVerifying.set(true);
        this.kycError.set(null);
        this.kycSuccessMsg.set(null);

        try {
            const formData = new FormData();
            formData.append('api_key', 'uY335TET_DRXQ0_t8pRDeVJj-CySDDIx');
            formData.append('api_secret', 'OJhLwUWImSdiMM5GwC0w_w2GZys32bdl');
            formData.append('image_file1', this.selfieFile()!);
            formData.append('image_file2', this.aadharFile()!);

            const response = await fetch('https://api-us.faceplusplus.com/facepp/v3/compare', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();
            console.log('Face++ API Response:', result);

            if (result.confidence !== undefined) {
                if (result.confidence > 80) {
                    this.kycSuccessMsg.set('Face Verified Successfully! Same Person. Similarity Score: ' + result.confidence);
                    this.isKycVerified.set(true);
                    if (this.user.id) localStorage.setItem('isKycVerified_' + this.user.id, 'true');

                    // Call backend api
                    if (this.user.id) {
                        this.authService.completeKyc(this.user.id).subscribe({
                            next: () => console.log('Backend KYC Status updated successfully'),
                            error: (err) => console.error('Failed to sync KYC with backend', err)
                        });
                    }

                    setTimeout(() => {
                        this.switchView('dashboard');
                    }, 3000); // give them 3 seconds to see the success message
                } else {
                    this.kycError.set('Face Not Matching. Similarity Score: ' + result.confidence);
                }
            } else {
                this.kycError.set('Error: confidence value not found. ' + (result.error_message || ''));
            }
        } catch (err: any) {
            console.error(err);
            this.kycError.set('Failed to connect to verification API.');
        } finally {
            this.isKycVerifying.set(false);
        }
    }

    // --- Voice Agent Integration ---

    toggleVoiceMode() {
        const newState = !this.isVoiceMode();
        this.isVoiceMode.set(newState);
        if (newState) {
            // Do NOT await — let the Voice Agent mount and ask for mic permission immediately
            this.sendGreeting();
        }
    }

    sendGreeting() {
        const greetingText = "Hi Sanjay! I'm AcciSure. How can I help you today?";
        
        // Show greeting in the chat UI
        this.chatMessages.update(msgs => [
             ...msgs,
             { role: 'agent', content: greetingText }
        ]);

        // Set processing = true so VoiceAgent waits while greeting plays
        this.isVoiceProcessing.set(true);
        console.log('[VoiceAgent] Greeting started, mic is paused while speaking...');

        if ('speechSynthesis' in window) {
            this.speakResponse(greetingText, () => {
                console.log('[VoiceAgent] Greeting finished. Ready for user input.');
                this.isVoiceProcessing.set(false);
            });
        } else {
            console.warn('[VoiceAgent] SpeechSynthesis not supported by browser. Starting mic immediately.');
            this.isVoiceProcessing.set(false);
        }
    }

    private speakResponse(text: string, onEnd: () => void) {
        if (!('speechSynthesis' in window)) {
            onEnd();
            return;
        }

        window.speechSynthesis.cancel();
        
        // Clean text: remove markdown artifacts, extra spaces, and weird symbols
        const cleanText = text
            .replace(/[*_#`]/g, '') 
            .replace(/[-]{2,}/g, ' ') 
            .replace(/\s+/g, ' ')
            .trim();

        const utterance = new SpeechSynthesisUtterance(cleanText);
        
        const setVoice = () => {
            const voices = window.speechSynthesis.getVoices();
            
            // Priority: Premium/Natural Voices -> Google/Microsoft Voices -> English Voices
            let selectedVoice = voices.find(v => (v.name.toLowerCase().includes('natural') || v.name.toLowerCase().includes('enhanced')) && v.lang.startsWith('en'))
                || voices.find(v => (v.name.includes('Google') || v.name.includes('Microsoft')) && v.lang.startsWith('en'))
                || voices.find(v => v.lang.startsWith('en'))
                || voices[0];

            if (selectedVoice) {
                utterance.voice = selectedVoice;
                console.log(`[VoiceAgent] Using Intelligent Voice: ${selectedVoice.name}`);
            }
        };

        // Voices are often loaded asynchronously
        if (window.speechSynthesis.getVoices().length === 0) {
            window.speechSynthesis.onvoiceschanged = () => {
                setVoice();
                window.speechSynthesis.speak(utterance);
                window.speechSynthesis.onvoiceschanged = null; // one-time use
            };
        } else {
            setVoice();
            window.speechSynthesis.speak(utterance);
        }

        // Voice characteristics for a more "human" feel
        utterance.rate = 1.05; // Slightly faster sounds more confident/intelligent
        utterance.pitch = 1.0; 
        utterance.volume = 1.0;

        utterance.onend = () => onEnd();
        utterance.onerror = (e) => {
            console.error('[VoiceAgent] TTS Error:', e);
            onEnd();
        };
    }

    async handleAudioCaptured(event: any) {
        const audioBlob = event as Blob;
        console.log(`[VoiceAgent] ── STEP 1: Audio captured. Size=${audioBlob?.size} bytes, Type=${audioBlob?.type}`);

        if (!audioBlob || audioBlob.size === 0) {
            console.warn('[VoiceAgent] ── Captured blob is empty, skipping.');
            return;
        }

        const currentUser = this.authService.getCurrentUser();
        if (!currentUser) {
            console.error('[VoiceAgent] ── No authenticated user, aborting.');
            return;
        }

        this.isVoiceProcessing.set(true);

        const policy = this.currentChatPolicy();
        const policyId = policy?.policyId || 'General';

        // Build the same customer + policy payload that sendChatMessage() sends to n8n
        const freshUser = this.authService.getUser();
        const customerContext = JSON.stringify({
            id: freshUser?.id || currentUser?.id,
            name: freshUser?.name || currentUser?.name,
            email: freshUser?.email || currentUser?.email,
            phone: freshUser?.phone || currentUser?.phone
        });
        const policyContext = policy ? JSON.stringify(policy) : '{}';

        const formData = new FormData();
        formData.append('audioFile', audioBlob, 'audio.webm');
        formData.append('policyId', policyId);
        formData.append('customerId', freshUser?.id || '');
        formData.append('policyContext', policyContext);      // Full policy object → n8n
        formData.append('customerContext', customerContext);  // Full customer object → n8n

        console.log(`[VoiceAgent] ── STEP 2: Sending to backend → policy:`, policy, '| customer:', freshUser);
        console.log('[VoiceAgent] ── POST http://localhost:5078/api/VoiceAgent/Process');

        try {
            const response = await this.http.post<any>('http://localhost:5078/api/VoiceAgent/Process', formData).toPromise();
            console.log('[VoiceAgent] ── STEP 3: Backend response received:', response);

            if (response) {
                const transcript = response.transcript || response.Transcript || '';
                const aiResponse = response.aiResponse || response.AiResponse || "I'm sorry, I'm having trouble thinking right now.";
                const audioBase64 = response.audioBase64 || response.AudioBase64;

                console.log(`[VoiceAgent] ── STEP 4: Transcript from Deepgram: "${transcript}"`);
                console.log(`[VoiceAgent] ── STEP 5: AI Response from Groq: "${aiResponse}"`);
                console.log(`[VoiceAgent] ── STEP 6: ElevenLabs audio present: ${!!audioBase64}, length: ${audioBase64?.length ?? 0}`);

                this.chatMessages.update(msgs => [
                    ...msgs,
                    { role: 'user', content: transcript || '(no transcript)' },
                    { role: 'agent', content: aiResponse }
                ]);

                if (audioBase64) {
                    console.log('[VoiceAgent] ── STEP 7: Playing ElevenLabs audio response...');
                    this.playAudioBase64(audioBase64);
                } else {
                    console.warn('[VoiceAgent] ── STEP 7: No audio in response, using SpeechSynthesis fallback...');
                    this.speakResponse(aiResponse, () => {
                        console.log('[VoiceAgent] ── Fallback TTS finished, resuming listening...');
                        this.isVoiceProcessing.set(false);
                    });
                    return; // don't set isVoiceProcessing=false yet, wait for speech to end
                }
            } else {
                console.warn('[VoiceAgent] ── STEP 3: Empty response from backend.');
                this.isVoiceProcessing.set(false);
            }
        } catch (err: any) {
            console.error('[VoiceAgent] ── ERROR in pipeline:', err);
            console.error('[VoiceAgent] ── Status:', err?.status, 'URL:', err?.url);
            this.isVoiceProcessing.set(false);
        }
    }

    private playAudioBase64(base64String: string) {
        try {
            const binaryString = window.atob(base64String);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            const blob = new Blob([bytes], { type: 'audio/mpeg' });
            const url = URL.createObjectURL(blob);
            const audio = new Audio(url);
            this.currentAudio = audio; // store reference so cancelVoiceMode can stop it

            // Keep isVoiceProcessing=true while AI is speaking
            // so the VoiceAgent component does NOT start recording over the response.
            this.isVoiceProcessing.set(true);

            audio.onended = () => {
                URL.revokeObjectURL(url);
                this.currentAudio = null;
                console.log('[VoiceAgent] ── AI audio playback finished, resuming mic listening...');
                this.isVoiceProcessing.set(false); // VoiceAgent.ngOnChanges will auto-restart mic
            };

            audio.onerror = (e) => {
                console.error('[VoiceAgent] ── Audio playback error:', e);
                URL.revokeObjectURL(url);
                this.isVoiceProcessing.set(false);
            };

            audio.play().catch(e => {
                console.error('[VoiceAgent] ── audio.play() failed:', e);
                this.isVoiceProcessing.set(false);
            });

        } catch (err) {
            console.error("Audio playback failed", err);
        }
    }

    cancelVoiceMode() {
        // Stop any ElevenLabs audio currently playing
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio.src = '';
            this.currentAudio = null;
        }
        // Stop any browser TTS (greeting)
        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel();
        }
        this.isVoiceMode.set(false);
        this.isVoiceProcessing.set(false);
    }
}