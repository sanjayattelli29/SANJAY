// importing angular core basics - component, signal, lifecycle hooks
import { Component, signal, computed, inject, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
// importing Driver.js for user onboarding tour
import { driver, Driver } from "driver.js";
// importing chart library to display graphs and charts
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
// importing router for navigation between pages
import { Router } from '@angular/router';
// importing common angular directives and pipes
import { CommonModule } from '@angular/common';
// importing form handling modules
import { FormsModule } from '@angular/forms';
// service for user login logout and authentication
import { AuthService } from '../../services/auth.service';
// service to fetch and manage insurance policies
import { PolicyService } from '../../services/policy.service';
// service for claim related operations from backend
import { ClaimService } from '../../services/claim.service';
// service for chat messaging between user and agent
import { ChatService } from '../../services/chat.service';

// router module for routing configuration
import { RouterModule } from '@angular/router';
// http client to make api calls to backend
import { HttpClient } from '@angular/common/http';
// component that shows notifications to user
import { NotificationPanelComponent } from '../../components/notification-panel/notification-panel.component';
// component for searching location using google places
import { GooglePlacesInputComponent } from '../../components/incident-location/incident-location.component';
// component for displaying maps
import { LocationMapComponent } from '../../components/location-map/location-map.component';
// component for nominee verification screen
import { NomineeVerificationComponent } from './customer-components/nominee-verification/nominee-verification.component';
// component for voice call agent feature
import { VoiceAgent } from './customer-components/voice-agent/voice-agent';
// first part of customer dashboard component
import { CustomerPart1Component } from './customer-components/part1/customer-part1';
// second part of customer dashboard component
import { CustomerPart2Component } from './customer-components/part2/customer-part2';
// component for ai voice call popup
import { AICallPopupComponent } from './customer-components/ai-call-popup.component';
// service for vapi voice calling functionality
import { VapiService } from '../../services/vapi.service';
// pipe for marking data as safe for angular
import { SafePipe } from '../../pipes/safe.pipe';
// library to generate pdf documents
import { jsPDF } from 'jspdf';
// library to add tables in pdf
import autoTable from 'jspdf-autotable';
// environment variables like api urls
import { environment } from '../../../environments/environment';
// n8n webhooks for backend integration
import { n8nWebhooks } from '../../../environments/n8n/n8n';

// customer dashboard main page component
// handles policy buying, claim raising, viewing policies and claims
// multi view single page component with lots of functionality
@Component({
    selector: 'app-customer-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, NotificationPanelComponent, VoiceAgent, CustomerPart1Component, CustomerPart2Component, AICallPopupComponent],
    templateUrl: './customer-dashboard.page.html',
    styleUrls: ['./customer-dashboard.page.css']
})
export class CustomerDashboardPage implements OnInit, AfterViewInit {
    // injecting services so they can be used in this component
    // auth service for user login and authentication
    private authService = inject(AuthService);
    // policy service to get policy information
    private policyService = inject(PolicyService);
    // claim service to manage insurance claims
    private claimService = inject(ClaimService);
    // chat service for messaging
    private chatService = inject(ChatService);
    // vapi service for voice calls
    protected vapiService = inject(VapiService);
    // router for navigation
    private router = inject(Router);
    // http client for api requests
    private http = inject(HttpClient);
    // change detector to update ui
    private cdr = inject(ChangeDetectorRef);

    // storing current user data from local storage
    user = this.authService.getUser();
    // signal that tracks which view is currently showing - dashboard or policies or claims etc
    activeView = signal<'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims' | 'policy-details' | 'claim-details' | 'chat' | 'kyc-verification' | 'profile'>('dashboard');
    // signal for sidebar - open or closed on mobile devices
    sidebarOpen = signal<boolean>(false);

    // â”€â”€â”€ Profile â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // storing user profile image url from local storage
    profileImage = signal<string | null>(
        localStorage.getItem('user_profile_image_' + (this.authService.getUser().id || 'guest')) ||
        localStorage.getItem('profile_image_' + (this.authService.getUser().id || 'guest'))
    );
    // flag showing if image is currently uploading
    profileIsUploading = signal<boolean>(false);
    // error message if image upload fails
    profileUploadError = signal<string>('');
    // text showing user location coordinates
    profileLocationText = signal<string>('');
    // url of google map for user location
    profileMapUrl = signal<string>('');
    // flag showing if browser location is being fetched
    profileIsFetchingLocation = signal<boolean>(false);
    // flag showing if profile save was successful
    profileSaveSuccess = signal<boolean>(false);

    // profile form object storing all user profile details
    profileForm = {
        // user name
        name: this.authService.getUser().name || '',
        // user email address
        email: this.authService.getUser().email || '',
        // user phone number
        phone: this.authService.getUser().phone || '',
        // user city from local storage
        city: localStorage.getItem('profile_city_' + (this.authService.getUser().id || 'guest')) || '',
        // user bio description
        bio: localStorage.getItem('profile_bio_' + (this.authService.getUser().id || 'guest')) || '',
        // user job occupation
        occupation: localStorage.getItem('profile_occupation_' + (this.authService.getUser().id || 'guest')) || '',
        // user bank account number
        bankAccount: localStorage.getItem('profile_bank_' + (this.authService.getUser().id || 'guest')) || '',
        // user ifsc code for bank
        ifscCode: localStorage.getItem('profile_ifsc_' + (this.authService.getUser().id || 'guest')) || ''
    };

    // validation function to check if bank account number is valid (9 to 18 digits)
    get isBankAccountValid(): boolean {
        const val = this.profileForm.bankAccount;
        // returns true if account is empty or has 9-18 digits
        return !val || /^\d{9,18}$/.test(val);
    }

    // validation function to check if ifsc code follows correct bank format
    get isIfscValid(): boolean {
        const val = this.profileForm.ifscCode;
        // returns true if ifsc is empty or matches bank ifsc format
        return !val || /^[A-Z]{4}0[A-Z0-9]{6}$/.test(val);
    }

    // function runs when user selects profile image from file input
    onProfileImageChange(event: Event) {
        // get the selected file from input element
        const file = (event.target as HTMLInputElement).files?.[0];
        // if no file selected return immediately
        if (!file) return;
        // check if file size is less than 3 MB
        if (file.size > 3 * 1024 * 1024) { alert('Image must be under 3 MB'); return; }

        // set uploading flag to true
        this.profileIsUploading.set(true);
        // clear any previous error message
        this.profileUploadError.set('');

        // create file reader to convert image to base64
        const reader = new FileReader();
        // when file is successfully loaded
        reader.onload = () => {
            const base64 = reader.result as string;
            // show image preview immediately to user
            this.profileImage.set(base64);

            // send image to backend api for uploading to imagekit cdn
            this.authService.uploadProfileImage(
                this.user.id || 'guest',
                base64,
                file.name
            ).subscribe({
                // when upload is successful
                next: (res) => {
                    // replace preview with permanent cdn url
                    this.profileImage.set(res.imageUrl);
                    // save cdn url to local storage
                    localStorage.setItem('user_profile_image_' + (this.user.id || 'guest'), res.imageUrl);
                    // stop uploading indicator
                    this.profileIsUploading.set(false);
                },
                // if upload fails
                error: () => {
                    // keep local preview so user still sees image
                    localStorage.setItem('profile_image_' + (this.user.id || 'guest'), base64);
                    // show error message
                    this.profileUploadError.set('Upload failed â€” saved locally.');
                    // stop uploading indicator
                    this.profileIsUploading.set(false);
                }
            });
        };
        // read file as base64 string
        reader.readAsDataURL(file);
    }

    // function to remove profile image
    removeProfileImage() {
        // clear the image signal
        this.profileImage.set(null);
        // remove image from local storage
        localStorage.removeItem('profile_image_' + (this.user.id || 'guest'));
        // also remove cdn url if exists
        localStorage.removeItem('user_profile_image_' + (this.user.id || 'guest'));
    }

    // function to get user current location using browser geolocation
    fetchUserLocation() {
        // check if browser supports geolocation
        if (!navigator.geolocation) { alert('Geolocation not supported by your browser.'); return; }
        // show loading indicator
        this.profileIsFetchingLocation.set(true);
        // get current position from browser
        navigator.geolocation.getCurrentPosition(
            // success callback with position coordinates
            (pos) => {
                // get latitude and round to 5 decimal places
                const lat = pos.coords.latitude.toFixed(5);
                // get longitude and round to 5 decimal places
                const lng = pos.coords.longitude.toFixed(5);
                // create google maps url with coordinates
                const mapUrl = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;
                // save map url to signal
                this.profileMapUrl.set(mapUrl);
                // show coordinates as text
                this.profileLocationText.set(`${lat}Â° N, ${lng}Â° E`);
                // save coordinates to local storage
                localStorage.setItem('profile_lat_' + this.user.id, lat);
                localStorage.setItem('profile_lng_' + this.user.id, lng);
                // reverse geocode to get city and state name
                fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`)
                    .then(r => r.json())
                    .then(data => {
                        // get city name from response
                        const city = data.address?.city || data.address?.town || data.address?.village || '';
                        // get state name from response
                        const state = data.address?.state || '';
                        // combine city and state with comma
                        const readable = [city, state].filter(Boolean).join(', ');
                        // show readable location or coordinates
                        this.profileLocationText.set(readable || `${lat}Â° N, ${lng}Â° E`);
                        // update form with city name
                        this.profileForm.city = readable;
                    })
                    .catch(() => { })
                    // finally stop loading
                    .finally(() => this.profileIsFetchingLocation.set(false));
            },
            // error callback
            () => {
                // stop loading indicator
                this.profileIsFetchingLocation.set(false);
                // show error alert
                alert('Unable to fetch your location. Please allow location access.');
            }
        );
    }

    // function to restore saved location from local storage when page loads
    restoreSavedLocation() {
        // get latitude from local storage
        const lat = localStorage.getItem('profile_lat_' + this.user.id);
        // get longitude from local storage
        const lng = localStorage.getItem('profile_lng_' + this.user.id);
        // if both coordinates exist then restore map
        if (lat && lng) {
            // set map url with saved coordinates
            this.profileMapUrl.set(`https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`);
            // show location text as city or coordinates
            this.profileLocationText.set(this.profileForm.city || `${lat}Â° N, ${lng}Â° E`);
        }
    }

    // function to save all profile changes to local storage
    saveProfile() {
        // check if bank details are valid before saving
        if (!this.isBankAccountValid || !this.isIfscValid) {
            // show alert if any field is invalid
            alert('Please correct the errors in Bank Details before saving.');
            return;
        }
        // get user id
        const uid = this.user.id || 'guest';
        // save city to local storage
        localStorage.setItem('profile_city_' + uid, this.profileForm.city);
        // save bio to local storage
        localStorage.setItem('profile_bio_' + uid, this.profileForm.bio);
        // save occupation to local storage
        localStorage.setItem('profile_occupation_' + uid, this.profileForm.occupation);
        // save bank account to local storage
        localStorage.setItem('profile_bank_' + uid, this.profileForm.bankAccount);
        // save ifsc code to local storage
        localStorage.setItem('profile_ifsc_' + uid, this.profileForm.ifscCode);
        // show success message
        this.profileSaveSuccess.set(true);
        // auto hide success message after 3 seconds
        setTimeout(() => this.profileSaveSuccess.set(false), 3000);
    }

    // function runs when user selects location from google places autocomplete
    onProfileLocationSelected(location: any) {
        // return if no location selected
        if (!location) return;
        // set city to selected location name
        this.profileForm.city = location.formatted_address || location.name;
        // if location has geometry coordinates
        // if location has geometry coordinates
        if (location.geometry?.location) {
            // check if lat is function or value
            const lat = typeof location.geometry.location.lat === 'function' ? location.geometry.location.lat() : location.geometry.location.lat;
            // check if lng is function or value
            const lng = typeof location.geometry.location.lng === 'function' ? location.geometry.location.lng() : location.geometry.location.lng;
            // create google maps url with new coordinates
            this.profileMapUrl.set(`https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`);
            // show selected location as text
            this.profileLocationText.set(this.profileForm.city);
            // save latitude to local storage
            localStorage.setItem('profile_lat_' + this.user.id, lat.toString());
            // save longitude to local storage
            localStorage.setItem('profile_lng_' + this.user.id, lng.toString());
        }
    }

    // getter function to get user initials for default avatar
    get userInitials(): string {
        // get user name or default to 'User'
        const n = this.user.name || 'User';
        // split name by space, get first letter of each word, take first 2 letters and make uppercase
        return n.split(' ').map((p: string) => p[0]).slice(0, 2).join('').toUpperCase();
    }
    // â”€â”€â”€ End Profile section â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // POLICIES AND CLAIMS SECTION START
    // signal for currently selected policy id
    selectedPolicyId = signal<string | null>(null);
    // signal for currently selected claim id
    selectedClaimId = signal<string | null>(null);
    // signal storing detailed information of selected policy
    detailedPolicy = signal<any | null>(null);
    // signal storing detailed information of selected claim
    detailedClaim = signal<any | null>(null);
    // signal for all claims related to a specific policy
    detailedClaimsForPolicy = signal<any[]>([]);
    // flag showing if claim is being submitted
    isSubmittingClaim = signal<boolean>(false);
    // flag showing if payment is being processed
    isPaying = signal<boolean>(false);

    // prevent selecting future dates for claim - set to today
    today: string = new Date().toISOString().split('T')[0];

    // data from backend - policy configuration data
    config: any = null;
    // signal storing all user policies from backend
    myPolicies = signal<any[]>([]);
    // signal storing all user claims from backend
    myClaims = signal<any[]>([]);
    // signal storing all chat conversations
    myChats = signal<any[]>([]);

    // DASHBOARD TOTALS SECTION
    // total coverage amount from all active policies
    totalCoverage = signal<number>(0);
    // total amount paid from approved or paid claims
    totalClaimsPaid = signal<number>(0);
    // remaining balance = total coverage - total claims paid
    remainingBalance = signal<number>(0);
    // total amount requested in all claims
    requestedClaimAmount = signal<number>(0);
    // total amount approved or paid in claims
    approvedClaimAmount = signal<number>(0);

    // BUY POLICY FLOW SECTION
    // currently selected policy category (Individual, Family, etc)
    selectedCategory: any = null;
    // currently selected policy tier (Silver, Gold, Platinum)
    selectedTier: any = null;


    // POLICY APPLICATION FORM - contains all data entered by user
    applicationForm: any = {
        // applicant (person applying for insurance) details
        applicant: {
            // full name of applicant
            fullName: '',
            // age of applicant
            age: 22,
            // profession/job of applicant
            profession: '',
            // alcohol drinking habit
            alcoholHabit: 'Non Drinker',
            // smoking habit
            smokingHabit: 'Non Smoker',
            // kilometers traveled per month
            travelKmPerMonth: 0,
            // type of vehicle used
            vehicleType: 'None'
        },
        // annual income of applicant
        annualIncome: 0,
        // how often to pay premium - yearly/half-yearly/quarterly/monthly
        paymentMode: 'yearly',
        // nominee (person who gets money if claim approved) details
        nominee: {
            // nominee name
            name: '',
            // relationship to applicant
            relationship: '',
            // nominee phone number
            phone: '',
            // nominee email
            email: '',
            // nominee bank account
            bankAccount: '',
            // nominee bank ifsc code
            ifsc: '',
            // nominee aadhar number
            aadharNumber: '',
            // nominee aadhar card image url
            aadharCardUrl: ''
        },
        // location where policy will be active
        location: {
            // street address
            address: '',
            // latitude coordinate
            latitude: null,
            // longitude coordinate
            longitude: null,
            // state name
            state: '',
            // district name
            district: '',
            // area name
            area: '',
            // postal code
            pincode: ''
        },
        // for family policies - list of family members
        familyMembers: []
    };

    // signal storing coordinates of policy location
    selectedPolicyLocationCoords = signal<{ lat: number, lng: number } | null>(null);

    // DOCUMENT UPLOAD SECTION
    // array storing uploaded policy documents (like aadhar, id proof, etc)
    policyDocuments: { type: string, file: File, name: string }[] = [];
    // flag showing if documents are being uploaded
    isUploadingDocs = signal<boolean>(false);
    // flag showing if aadhar is being extracted from image
    isExtractingAadhar = signal<boolean>(false);
    // flag showing if aadhar extraction was successful
    aadharSuccess = signal<boolean>(false);

    // PREMIUM CALCULATION SECTION
    // premium amount calculated by backend based on tier and other factors
    calculatedPremium = signal<number>(0);
    // flag showing if form is being submitted
    isSubmitting = signal<boolean>(false);

    // BUY POLICY MULTI-STEP FLOW
    // tracks current step in policy buying - step 1 is form, step 2 is review
    // tracks current step in policy buying - step 1 is form, step 2 is review
    buyFlowStep = signal<number>(1);
    // array of payment dates and amounts for policy
    paymentTimeline = signal<any[]>([]);

    // AI CHAT ASSISTANT SECTION START
    // flag showing if ai chat popup is open
    isChatOpen = signal<boolean>(false);
    // array of all messages in current chat
    chatMessages = signal<any[]>([]);
    // currently selected policy for ai chat
    currentChatPolicy = signal<any | null>(null);
    // flag showing if waiting for ai response
    isChatLoading = signal<boolean>(false);
    // user message being typed in chat
    chatUserMessage = signal<string>('');

    // VOICE AGENT SECTION START
    // id of current active chat with voice agent
    activeChatId = signal<string | null>(null);
    // flag showing if voice mode is on
    isVoiceMode = signal<boolean>(false);
    // flag showing if voice is being processed
    isVoiceProcessing = signal<boolean>(false);
    // flag showing if ai voice call popup is open
    isAiCallPopupOpen = signal<boolean>(false);
    // reference to audio element that is currently playing
    private currentAudio: HTMLAudioElement | null = null;

    // --- Onboarding Tour ---
    // signal to show/hide the initial onboarding popup
    showOnboardingModal = signal<boolean>(false);
    // driver instance for the tour
    tourDriver: any = null;

    // function to start ai voice call with vapi service
    startAiVoiceCall() {
        // open the ai call popup
        this.isAiCallPopupOpen.set(true);
        // start the voice call
        this.vapiService.startCall();
    }

    // MODAL DIALOGS SECTION
    // flag showing if policy detail modal is open
    showPolicyDetailModal = signal(false);
    // flag showing if payment modal is open
    showPaymentModal = signal(false);
    // flag showing if kyc verification modal is open
    showKycModal = signal(false);
    // flag showing if application success modal is open
    showAppSuccessModal = signal(false);
    // flag showing if claim success modal is open
    showClaimSuccessModal = signal(false);
    // signal for currently selected policy in modal
    selectedPolicy = signal<any | null>(null);

    // KYC (Know Your Customer) VERIFICATION SECTION START
    // flag showing if user has completed kyc verification
    isKycVerified = signal<boolean>(false);
    // flag showing if kyc form retry is being shown
    showKycFormRetry = signal<boolean>(false);
    // tracks current step in kyc - step 1 is upload aadhar, step 2 is selfie
    kycStep = signal<number>(1);
    // selected aadhar file by user
    aadharFile = signal<File | null>(null);
    // preview of aadhar image
    aadharPreview = signal<string | null>(null);
    // text extracted from aadhar image by ocr
    aadharText = signal<string | null>(null);
    // selected selfie file by user
    selfieFile = signal<File | null>(null);
    // preview of selfie image
    selfiePreview = signal<string | null>(null);
    // flag showing if camera is currently active for selfie
    isCameraActive = signal<boolean>(false);
    // media stream from user camera
    cameraStream = signal<MediaStream | null>(null);
    // flag showing if kyc verification is in progress
    isKycVerifying = signal<boolean>(false);
    // error message from kyc verification
    kycError = signal<string | null>(null);
    // success message after kyc verification
    kycSuccessMsg = signal<string | null>(null);

    // Death Claim Section
    // flag showing if nominee has been verified
    isNomineeVerified = signal<boolean>(false);
    // flag showing if user has filed death claim
    hasDeathClaim = signal<boolean>(false);
    // aadhar file of nominee
    nomineeAadharFile = signal<File | null>(null);
    // photo file of nominee
    nomineePhotoFile = signal<File | null>(null);
    // url of uploaded nominee aadhar
    nomineeAadharUrl = signal<string>('');
    // url of uploaded nominee photo
    nomineePhotoUrl = signal<string>('');

    // INITIALIZATION FUNCTION - runs when component is created
    ngOnInit() {
        // prefill applicant name from user profile if available
        if (this.user.name) {
            this.applicationForm.applicant.fullName = this.user.name;
        }

        // check if user has already completed kyc from local storage
        const kycStatus = localStorage.getItem('isKycVerified_' + this.user.id);
        // if kyc is completed before, set the flag
        if (kycStatus === 'true') {
            this.isKycVerified.set(true);
        }

        // load policy configuration from backend
        this.loadConfig();
        // load user policies from backend
        this.loadMyPolicies();
        // load user claims from backend
        this.loadMyClaims();
        // load chat list from backend
        this.loadChatList();
        // restore previously saved location
        this.restoreSavedLocation();

        // --- Onboarding Tour Initialization ---
        // Get the fresh user data directly from service
        const currentUser = this.authService.getUser();
        const userId = currentUser.id || 'guest';
        // check if user has already completed the tour
        const tourStatus = localStorage.getItem('tourCompleted_' + userId);
        
        if (tourStatus !== 'true') {
            console.log('[Onboarding] Starting tour modal immediately for user:', userId);
            // show the onboarding popup immediately for a responsive feel
            this.showOnboardingModal.set(true);
            // trigger change detection to be sure it renders
            setTimeout(() => this.cdr.detectChanges(), 0);
        }
    }

    // This function is called after Angular renders the view
    // Used to initialize Chart.js charts on the dashboard
    ngAfterViewInit() {
        // wait 300ms for dom to render completely
        // then render all charts
        setTimeout(() => this.renderDashboardCharts(), 300);
    }

    // object to store all chart instances so we can update them later
    private chartInstances: { [key: string]: Chart } = {};

    // function to render all dashboard charts at once
    renderDashboardCharts() {
        // render bar chart showing coverage and claimed amounts
        this.renderCoverageBarChart();
        // render donut chart showing claim statuses
        this.renderClaimDonutChart();
        // render line chart showing activities over time
        this.renderActivityLineChart();
    }

    // helper function to get existing chart or create new one
    private getOrCreateChart(id: string, config: any): Chart | null {
        // get canvas element where chart will be drawn
        const canvas = document.getElementById(id) as HTMLCanvasElement;
        // if canvas not found return null
        if (!canvas) return null;
        // if chart already exists destroy it first
        if (this.chartInstances[id]) {
            this.chartInstances[id].destroy();
        }
        // create new chart with given config
        const chart = new Chart(canvas, config);
        // store reference to this chart
        this.chartInstances[id] = chart;
        // return the created chart
        return chart;
    }

    // function to render bar chart showing coverage vs claimed amounts
    renderCoverageBarChart() {
        // get all active policies only
        const activePolicies = this.myPolicies().filter((p: any) => p.status === 'Active');
        // extract policy tier names for x-axis labels
        const labels = activePolicies.map((p: any) => p.tierId?.replace('IND_', '').replace('FAM_', '') || 'Policy');
        // get coverage amount for each policy
        const coverageData = activePolicies.map((p: any) => p.totalCoverageAmount || 0);
        // calculate claimed amount for each policy
        const claimedData = activePolicies.map((p: any) => {
            // get claims for this policy that are approved or paid
            const policyClaims = this.myClaims().filter((c: any) => c.policyApplicationId === p.id && (c.status === 'Approved' || c.status === 'Paid'));
            // sum up approved amounts
            return policyClaims.reduce((sum: number, c: any) => sum + (c.approvedAmount || 0), 0);
        });

        // if no policies show empty state
        if (labels.length === 0) {
            // add placeholder
            labels.push('No Policy');
            // add zero data
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

    // function to render donut chart showing claim statuses
    renderClaimDonutChart() {
        // count claims that are approved or paid
        const approved = this.myClaims().filter((c: any) => c.status === 'Approved' || c.status === 'Paid').length;
        // count claims that are in processing
        const processing = this.myClaims().filter((c: any) => c.status === 'Processing' || c.status === 'Pending' || c.status === 'UnderReview').length;
        // count claims that are rejected
        const rejected = this.myClaims().filter((c: any) => c.status === 'Rejected').length;

        // check if there are any claims
        const hasData = approved + processing + rejected > 0;
        // set data for chart - if has data show counts otherwise show 1 for empty state
        const data = hasData ? [approved, processing, rejected] : [1];
        // set colors for each segment
        const bgColors = hasData ? ['#10b981', '#f59e0b', '#ef4444'] : ['#e2e8f0'];
        // set labels for each segment
        const labels = hasData ? ['Approved / Paid', 'In Processing', 'Rejected'] : ['No Claims Yet'];
        // store counts separately
        const counts = hasData ? [approved, processing, rejected] : [0];

        // get the legend element from html
        const legendEl = document.getElementById('claimDonutLegend');
        // if legend element exists
        if (legendEl) {
            // create legend items with color and count
            const legendItems = hasData
                ? [
                    { color: '#10b981', label: 'Approved / Paid', count: approved },
                    { color: '#f59e0b', label: 'In Processing', count: processing },
                    { color: '#ef4444', label: 'Rejected', count: rejected }
                ]
                : [{ color: '#e2e8f0', label: 'No Claims Yet', count: 0 }];

            // create html for legend and insert into element
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

        // create donut chart with data
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

    // function to render line chart showing activity over past 6 months
    renderActivityLineChart() {
        // array to store last 6 months labels
        const months: string[] = [];
        // get current date
        const now = new Date();
        // loop through last 6 months
        for (let i = 5; i >= 0; i--) {
            // create date for first day of month
            const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
            // push month label in short format
            months.push(d.toLocaleString('default', { month: 'short', year: '2-digit' }));
        }

        // count policies purchased per month
        const policyByMonth = months.map((_, idx) => {
            // create date for this month
            const d = new Date(now.getFullYear(), now.getMonth() - (5 - idx), 1);
            // filter policies submitted in this month
            return this.myPolicies().filter((p: any) => {
                const pd = new Date(p.submissionDate);
                // check if year and month match
                return pd.getFullYear() === d.getFullYear() && pd.getMonth() === d.getMonth();
            }).length;
        });

        // count claims raised per month
        const claimsByMonth = months.map((_, idx) => {
            // create date for this month
            const d = new Date(now.getFullYear(), now.getMonth() - (5 - idx), 1);
            // filter claims submitted in this month
            return this.myClaims().filter((c: any) => {
                const cd = new Date(c.submissionDate);
                // check if year and month match
                return cd.getFullYear() === d.getFullYear() && cd.getMonth() === d.getMonth();
            }).length;
        });

        // create line chart with data
        this.getOrCreateChart('activityLineChart', {
            type: 'line',
            data: {
                labels: months,
                datasets: [
                    // policies purchased line
                    { label: 'Policies Started', data: policyByMonth, borderColor: '#0f1f14', backgroundColor: 'rgba(15,31,20,0.08)', fill: true, tension: 0.4, pointBackgroundColor: '#0f1f14', pointBorderColor: '#fff', pointBorderWidth: 2, pointRadius: 5, pointHoverRadius: 7 },
                    // claims raised line
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

    // function to load policy configuration from backend database
    // contains all available policy categories, tiers and benefits
    loadConfig() {
        // call backend to get policy config
        this.policyService.getConfiguration().subscribe({
            // when config is received
            next: (config) => {
                // store config in component variable
                this.config = config;
                // log for debugging
                console.log('Policy Configuration loaded:', config);
            }
        });
    }

    // function to load all user policies from backend
    // also recalculates dashboard totals
    loadMyPolicies() {
        // call backend to get user policies
        this.policyService.getMyPolicies().subscribe({
            // when policies are received
            next: (policies) => {
                // store policies in signal
                this.myPolicies.set(policies);
                // recalculate dashboard totals based on policies
                this.calculateTotals();
                // log for debugging
                console.log('User policies loaded:', policies);
                // wait a bit then re-render charts with real data
                setTimeout(() => this.renderDashboardCharts(), 150);
            }
        });
    }

    // function to load all user claims from backend
    // updates dashboard statistics
    loadMyClaims() {
        // call backend to get user claims
        this.claimService.getMyClaims().subscribe({
            // when claims are received
            next: (claims) => {
                // process claims and extract application details
                this.myClaims.set(claims.map((c: any) => {
                    // variable to store claim details
                    let details: any = null;
                    // check if claim has application data json
                    if (c.policy?.applicationDataJson) {
                        try {
                            // parse json string to object
                            const raw = JSON.parse(c.policy.applicationDataJson);
                            // function to normalize object keys from uppercase to lowercase
                            const normalize = (obj: any) => {
                                if (!obj) return null;
                                const normalized: any = {};
                                // loop through all keys
                                Object.keys(obj).forEach(key => {
                                    // convert first letter to lowercase
                                    const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                                    // add to normalized object
                                    normalized[normalizedKey] = obj[key];
                                });
                                return normalized;
                            };

                            // normalize the raw data
                            const fullDetails = normalize(raw);
                            // if details exist then process nominee data
                            if (fullDetails) {
                                // normalize nominee data
                                fullDetails.nominee = normalize(fullDetails.nominee || raw.Nominee) || {};
                                // set nominee name from various possible fields
                                fullDetails.nominee.name = fullDetails.nominee.name || fullDetails.nominee.nomineeName || '--';
                                // set nominee email
                                fullDetails.nominee.email = fullDetails.nominee.email || fullDetails.nominee.nomineeEmail || '--';
                                // set nominee phone
                                fullDetails.nominee.phone = fullDetails.nominee.phone || fullDetails.nominee.nomineePhone || '--';
                                // set nominee bank account
                                fullDetails.nominee.bankAccount = fullDetails.nominee.bankAccount || fullDetails.nominee.nomineeBankAccountNumber || '--';
                                // set nominee relationship
                                fullDetails.nominee.relationship = fullDetails.nominee.relationship || fullDetails.nominee.nomineeRelationship || '--';
                            }
                            // store processed details
                            details = fullDetails;
                        } catch (e) { }
                    }
                    // return claim with full details
                    return { ...c, fullDetails: details };
                }));
                // recalculate dashboard totals
                this.calculateTotals();

                // check if user has any death claims
                const deathClaim = this.myClaims().find((c: any) => c.incidentType === 'Death' || (c.incidentDataJson && c.incidentDataJson.includes('Death')));
                // if death claim exists set flag
                if (deathClaim) {
                    this.hasDeathClaim.set(true);
                }

                // log for debugging
                console.log('User claims loaded:', this.myClaims());
                // re-render charts with real data
                setTimeout(() => this.renderDashboardCharts(), 150);
            }
        });
    }

    // function to load all chat conversations from backend
    loadChatList() {
        // call backend to get chat list
        this.chatService.getChatList().subscribe({
            // when chats are received
            next: (chats) => this.myChats.set(chats),
            // if error happens
            error: (err) => console.error('Failed to load chat list', err)
        });
    }

    // function to calculate dashboard totals from policies and claims
    calculateTotals() {
        // variable for total coverage amount
        let coverage = 0;
        // variable for total claims paid
        let claims = 0;
        // variable for total requested amount
        let requested = 0;
        // variable for total approved amount
        let approved = 0;

        // loop through all policies
        this.myPolicies().forEach(p => {
            // check if policy is active
            if (p.status === 'Active') {
                // add coverage amount
                coverage += p.totalCoverageAmount || 0;
            }
        });

        // loop through all claims
        this.myClaims().forEach(c => {
            // add to requested amount
            requested += c.requestedAmount || 0;
            // if claim is approved or paid
            if (c.status === 'Approved' || c.status === 'Paid') {
                // add to claims paid
                claims += c.approvedAmount || 0;
                // add to approved amount
                approved += c.approvedAmount || 0;
            }
        });

        // update all dashboard total signals with calculated values
        // set total coverage amount
        this.totalCoverage.set(coverage);
        // set total paid from claims
        this.totalClaimsPaid.set(claims);
        // calculate remaining balance (coverage - paid)
        this.remainingBalance.set(coverage - claims);
        // set total requested
        this.requestedClaimAmount.set(requested);
        // set total approved
        this.approvedClaimAmount.set(approved);
    }

    // function to switch between different dashboard views
    switchView(view: 'dashboard' | 'my-policies' | 'buy-policy' | 'raise-claim' | 'my-claims' | 'chat' | 'kyc-verification' | 'profile' | 'policy-details' | 'claim-details') {
        // update active view signal
        this.activeView.set(view);
        
        // --- Tour Dynamic Movement ---
        // If tour is active and moving to KYC, advance to the next step
        if (this.tourDriver && view === 'kyc-verification') {
            setTimeout(() => {
                if (this.tourDriver?.isActive()) {
                    this.tourDriver.moveNext();
                }
            }, 300);
        }

        // if buying policy then reset selections
        if (view === 'buy-policy') {
            // clear category selection
            this.selectedCategory = null;
            // clear tier selection
            this.selectedTier = null;
        }
        // if going to chat view reload chat list
        if (view === 'chat') {
            // reload chats from backend
            this.loadChatList();
        }
    }

    // function to proceed to kyc verification
    proceedToKyc() {
        // close kyc modal
        this.showKycModal.set(false);
        // switch to kyc verification view
        this.switchView('kyc-verification');
    }

    // function to navigate to chat for a specific policy
    navigateToChat(chat: any) {
        // if new chat then initialize it via backend first
        if (chat.id && chat.id.startsWith('new_')) {
            // prepare data to initialize chat
            const initData = {
                policyId: chat.policyId,
                customerId: chat.customerId,
                agentId: chat.agentId
            };
            // call backend to initialize chat
            this.chatService.initChat(initData).subscribe({
                // if initialization successful
                next: (res) => {
                    // navigate to chat page
                    this.router.navigate(['/chat', chat.policyId]);
                },
                // if initialization fails
                error: (err) => alert('Failed to initialize chat: ' + (err.error?.message || 'Server error'))
            });
        } else {
            // existing chat so just navigate
            this.router.navigate(['/chat', chat.policyId]);
        }
    }

    // function to check if user has active policy in category
    hasActivePolicy(categoryId: string): boolean {
        // return true if any policy in this category is active
        return this.myPolicies().some(p => p.policyCategory === categoryId && p.status === 'Active');
    }

    // function when user selects a policy category
    selectCategory(category: any) {
        // store selected category
        this.selectedCategory = category;
        // reset tier selection
        this.selectedTier = null;
        // reset family members list
        this.applicationForm.familyMembers = [];
    }

    // function when user selects a policy tier
    selectTier(tier: any) {
        // check if user has completed kyc
        if (!this.isKycVerified()) {
            // if not verified show kyc modal
            this.showKycModal.set(true);
            return;
        }

        // store selected tier
        this.selectedTier = tier;

        // prefill user name from profile if not already filled
        if (!this.applicationForm.applicant.fullName && this.user.name) {
            this.applicationForm.applicant.fullName = this.user.name;
        }

        // calculate premium based on selected tier
        this.updatePremium();
    }

    // FAMILY MEMBER MANAGEMENT SECTION
    // function to trigger file input click
    triggerFileInput(id: string) {
        // find input element and click it
        document.getElementById(id)?.click();
    }

    // function to add new family member
    addFamilyMember() {
        // push new family member object
        this.applicationForm.familyMembers.push({
            // family member full name
            fullName: '',
            // relationship to applicant
            relation: '',
            // date of birth
            dateOfBirth: '',
            // any health conditions
            healthConditions: '',
            // aadhar number
            aadharNumber: '',
            // aadhar card image url
            aadharCardUrl: '',
            // flag if aadhar extraction successful
            aadharSuccess: false,
            // flag if extracting aadhar
            isExtractingAadhar: false,
            // flag if uploading aadhar file
            isUploadingFile: false
        });
    }

    // function to remove family member at index
    removeFamilyMember(index: number) {
        // remove from array at index
        this.applicationForm.familyMembers.splice(index, 1);
    }

    // AADHAR UPLOAD SECTION
    // function when family member aadhar file is selected
    onFamilyMemberAadharUpload(event: any, index: number) {
        // get the selected file
        const file = event.target.files[0];
        // if file exists
        if (file) {
            // reset success flag
            this.applicationForm.familyMembers[index].aadharSuccess = false;
            // upload to imagekit
            this.uploadFamilyMemberAadharToImageKit(file, index);
        }
    }

    // function to upload aadhar to imagekit cdn
    uploadFamilyMemberAadharToImageKit(file: File, index: number) {
        // set uploading flag
        this.applicationForm.familyMembers[index].isUploadingFile = true;
        // update ui
        this.cdr.markForCheck();

        // call backend to upload
        this.policyService.uploadDocument(file, 'family-aadhars').subscribe({
            // if upload successful
            next: (res) => {
                // save url
                this.applicationForm.familyMembers[index].aadharCardUrl = res.url;
                // clear uploading flag
                this.applicationForm.familyMembers[index].isUploadingFile = false;
                // update ui
                this.cdr.markForCheck();

                // now extract aadhar number from image
                this.extractFamilyMemberAadharNumber(file, index);
            },
            // if upload fails
            error: (err) => {
                // log error
                console.error('Failed to upload Family Aadhar', err);
                // clear uploading flag
                this.applicationForm.familyMembers[index].isUploadingFile = false;
                // update ui
                this.cdr.markForCheck();
                // show error alert
                alert('Failed to upload Aadhar card. Please try again.');
            }
        });
    }

    // function to extract aadhar number from image using google vision api
    async extractFamilyMemberAadharNumber(file: File, index: number) {
        // set extracting flag
        this.applicationForm.familyMembers[index].isExtractingAadhar = true;
        try {
            // create file reader
            const reader = new FileReader();
            // read file as data url
            reader.readAsDataURL(file);

            // when file is read
            reader.onload = async () => {
                try {
                    // extract base64 content
                    const base64Content = (reader.result as string).split(',')[1];
                    // call google vision api
                    const res = await fetch(
                        `https://vision.googleapis.com/v1/images:annotate?key=${environment.googleVisionApiKey}`,
                        {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            // send image to vision api for text recognition
                            body: JSON.stringify({
                                requests: [{
                                    image: { content: base64Content },
                                    features: [{ type: 'TEXT_DETECTION' }]
                                }]
                            })
                        }
                    );

                    // get response from api
                    const json = await res.json();
                    // extract all text from image
                    const fullText = json.responses?.[0]?.fullTextAnnotation?.text || '';

                    // extract aadhar number - 12 digits
                    const aadharMatch = fullText.replace(/[\s-]/g, '').match(/\d{12}/);

                    // if aadhar found
                    if (aadharMatch) {
                        // save aadhar number
                        this.applicationForm.familyMembers[index].aadharNumber = aadharMatch[0];
                        // mark as successful
                        this.applicationForm.familyMembers[index].aadharSuccess = true;
                    }

                    // extract date of birth from text
                    // clean up newlines and extra spaces
                    const cleanText = fullText.replace(/[\r\n\s]+/g, ' ');
                    // match dob patterns
                    const dobMatch = cleanText.match(/(?:DOB|Date of Birth|YOB).*?(\d{2}\/\d{2}\/\d{4}|\d{4})/i);
                    // if dob found
                    if (dobMatch) {
                        // get dob string
                        let dobStr = dobMatch[1];
                        // if only year (4 digits)
                        if (dobStr.length === 4) {
                            // format as YYYY-01-01
                            this.applicationForm.familyMembers[index].dateOfBirth = `${dobStr}-01-01`;
                        } else {
                            // split date parts
                            const parts = dobStr.split('/');
                            // format as YYYY-MM-DD
                            this.applicationForm.familyMembers[index].dateOfBirth = `${parts[2]}-${parts[1]}-${parts[0]}`;
                        }
                    }

                    // extract name from text
                    // split into lines and filter empty
                    const lines = fullText.split('\n').map((l: string) => l.trim()).filter((l: string) => l.length > 0);
                    // find dob line index
                    const dobIndex = lines.findIndex((l: string) => /(?:DOB|Date of Birth|YOB)/i.test(l));
                    // if dob found
                    if (dobIndex > 0) {
                        // search backwards from dob line for name
                        for (let i = dobIndex - 1; i >= 0; i--) {
                            const line = lines[i];
                            // skip generic words like India, Government, Father, etc
                            if (!/India|Government|Father|Male|Female/i.test(line) && line.length > 2) {
                                // found name
                                this.applicationForm.familyMembers[index].fullName = line;
                                break;
                            }
                        }
                    }
                    // update ui
                    this.cdr.markForCheck();
                } catch (e) {
                    // log vision api error
                    console.error('Vision API error for Family Member', e);
                } finally {
                    // clear extracting flag
                    this.applicationForm.familyMembers[index].isExtractingAadhar = false;
                    // update ui
                    this.cdr.markForCheck();
                }
            };
        } catch (error) {
            // log file reading error
            console.error('Error reading family member file', error);
            // clear extracting flag
            this.applicationForm.familyMembers[index].isExtractingAadhar = false;
        }
    }


    // NOMINEE AADHAR MANAGEMENT SECTION
    // function when nominee aadhar file is selected
    onNomineeAadharUpload(event: any) {
        // get selected file
        const file = event.target.files[0];
        // if file exists
        if (file) {
            // reset success flag
            this.aadharSuccess.set(false);
            // upload to imagekit first, then extract via vision api
            this.uploadNomineeAadharToImageKit(file);
        }
    }

    // function to extract nominee aadhar number from image using vision api
    async extractNomineeAadharNumber(file: File) {
        // set extracting flag
        this.isExtractingAadhar.set(true);
        try {
            // log for debugging
            console.log('[VisionAPI] Extracting from local file:', file.name);

            // create file reader
            const reader = new FileReader();
            // read file as data url
            reader.readAsDataURL(file);

            // when file is loaded
            reader.onload = async () => {
                try {
                    // extract base64 content
                    const base64Content = (reader.result as string).split(',')[1];
                    // call google vision api
                    const res = await fetch(
                        `https://vision.googleapis.com/v1/images:annotate?key=${environment.googleVisionApiKey}`,
                        {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            // send image for text detection
                            body: JSON.stringify({
                                requests: [{
                                    image: { content: base64Content },
                                    features: [{ type: 'TEXT_DETECTION' }]
                                }]
                            })
                        }
                    );

                    // get response
                    const json = await res.json();
                    // extract all text from response
                    const fullText = json.responses?.[0]?.fullTextAnnotation?.text || '';

                    // log extracted text
                    console.log('[VisionAPI] Full Text Result:', fullText);

                    // clean up text - remove newlines and extra spaces
                    const cleanText = fullText.replace(/[\r\n\s]+/g, ' ');
                    // look for aadhar in format 1234 5678 9012
                    const spacedMatch = cleanText.match(/\b\d{4}\s\d{4}\s\d{4}\b/);
                    // look for continuous 12 digits
                    const solidMatch = fullText.replace(/[^0-9]/g, '').match(/\d{12}/);

                    // variable to store raw aadhar
                    let raw = null;
                    // if space separated format found
                    if (spacedMatch) {
                        // remove spaces
                        raw = spacedMatch[0].replace(/\s/g, '');
                    } else if (solidMatch) {
                        // use solid 12 digits
                        raw = solidMatch[0];
                    }

                    // if valid aadhar found
                    if (raw && raw.length >= 12) {
                        // take first 12 digits
                        raw = raw.substring(0, 12);
                        // save to form
                        this.applicationForm.nominee.aadharNumber = raw;
                        // log success
                        console.log('[VisionAPI] Successfully Extracted Aadhar:', raw);
                    } else {
                        // log warning if not found
                        console.warn('[VisionAPI] Could not detect a valid 12-digit Aadhar number.');
                    }
                } catch (error) {
                    // log api error
                    console.error('[VisionAPI] Extraction API Error:', error);
                } finally {
                    // clear extracting flag
                    this.isExtractingAadhar.set(false);
                }
            };

            // handle file reader error
            reader.onerror = (error) => {
                // log error
                console.error('[VisionAPI] FileReader Error:', error);
                // clear extracting flag
                this.isExtractingAadhar.set(false);
            };

        } catch (error) {
            console.error('[VisionAPI] Setup Error:', error);
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

                    // Trigger Vision API extraction using the original file
                    this.extractNomineeAadharNumber(file);
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
            // Always send applicant data in PrimaryApplicant if it's not the specific 'INDIVIDUAL' category
            // This ensures custom categories work with the backend
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? { ...this.applicationForm.applicant, annualIncome: this.applicationForm.annualIncome } : null,
            primaryApplicant: (this.selectedCategory.categoryId !== 'INDIVIDUAL') ? { ...this.applicationForm.applicant, annualIncome: this.applicationForm.annualIncome } : null,
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
            'yearly': 1.0
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

    // Helper to get pure base premium without payment multipliers
    getPureBasePremium(): number {
        const currentTotal = this.calculatedPremium();
        if (currentTotal <= 0) return 0;

        const multipliers: { [key: string]: number } = {
            'monthly': 1.1,
            'halfYearly': 1.05,
            'yearly': 1.0
        };

        const mode = this.applicationForm.paymentMode || 'yearly';
        const currentMult = multipliers[mode] || 1.0;

        return currentTotal / currentMult;
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
    // take user back to step 2 of policy buying flow
    goBackToStep2() {
        this.buyFlowStep.set(2);
    }

    // take user back to step 3 of policy buying flow
    goBackToStep3() {
        this.buyFlowStep.set(3);
    }

    // creates payment timeline for first 12 months showing when each payment is due
    calculatePaymentTimeline() {
        // read selected payment mode and total premium amount
        const months = [];
        const mode = this.applicationForm.paymentMode;
        const totalPremium = this.calculatedPremium();
        const basePeriodic = this.getApproximatePayment(mode);

        // start generating timeline from today
        const startDate = new Date();

        // create 12 months of payment schedule
        for (let i = 0; i < 12; i++) {
            const currentDate = new Date(startDate);
            currentDate.setMonth(startDate.getMonth() + i);

            // check if this month is a payment month based on payment mode
            let isPaymentMonth = false;
            let amount = 0;

            // for monthly mode, every month is a payment month
            if (mode === 'monthly') {
                isPaymentMonth = true;
                amount = basePeriodic;
            } else if (mode === 'halfYearly') {
                // for half yearly, payment at month 0 and month 6
                if (i === 0 || i === 6) {
                    isPaymentMonth = true;
                    amount = basePeriodic;
                }
            } else if (mode === 'yearly') {
                // for yearly, payment only in first month
                if (i === 0) {
                    isPaymentMonth = true;
                    amount = totalPremium;
                }
            }

            // add this month to timeline with payment status
            months.push({
                monthIndex: i + 1,
                date: currentDate,
                isPaymentMonth,
                amount,
                status: isPaymentMonth ? 'Payment Due' : 'Active Coverage'
            });
        }
        // store timeline in signal for template to use
        this.paymentTimeline.set(months);
    }

    // find the next payment date for an active policy
    getNextPaymentDate(pol: any): Date | null {
        // get full 12 month timeline for this policy
        const timeline = this.generatePolicyTimeline(pol);
        const now = new Date();
        // find upcoming payment months from today onwards
        const upcoming = timeline.filter((s: any) => s.isPaymentMonth && new Date(s.date) >= now);
        // return first upcoming payment date or null if none
        return upcoming.length > 0 ? upcoming[0].date : null;
    }

    // create 12 month payment timeline for an existing active policy
    generatePolicyTimeline(pol: any) {
        // return empty list if no policy provided
        if (!pol) return [];
        const months = [];
        // normalize payment mode to lowercase
        const mode = this.normalizePaymentMode(pol.paymentMode);
        const totalPremium = pol.calculatedPremium || 0;

        // set multiplier and count based on payment mode
        let multiplier = 1.0;
        let count = 1;
        // monthly has 10% extra cost, half yearly has 5% extra, yearly is full price
        if (mode === 'monthly') { multiplier = 1.1; count = 12; }
        else if (mode === 'halfyearly') { multiplier = 1.05; count = 2; }
        else { multiplier = 1.0; count = 1; }

        // calculate base premium without mode multiplier
        const pureBase = totalPremium / multiplier;
        // calculate periodic payment (divide by number of payments in year)
        const basePeriodic = totalPremium / count;

        // start timeline from policy start date
        const startDate = pol.startDate ? new Date(pol.startDate) : new Date();

        // loop through 12 months and mark payment months
        for (let i = 0; i < 12; i++) {
            const currentDate = new Date(startDate);
            currentDate.setMonth(startDate.getMonth() + i);

            // determine if this month has a payment due
            let isPaymentMonth = false;
            let amount = 0;

            // monthly payments are every month
            if (mode === 'monthly') {
                isPaymentMonth = true;
                amount = basePeriodic;
            } else if (mode === 'halfyearly') {
                // half yearly payments at month 0 and month 6
                if (i === 0 || i === 6) {
                    isPaymentMonth = true;
                    amount = basePeriodic;
                }
            } else if (mode === 'yearly') {
                // yearly payment only at first month
                if (i === 0) {
                    isPaymentMonth = true;
                    amount = totalPremium;
                }
            } else {
                // default case: payment due at first month
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

    // handle when user selects a document file for policy application
    onPolicyFileChange(event: any, type: string) {
        // check if file selected
        if (event.target.files.length > 0) {
            const file = event.target.files[0];

            // only accept PDF documents
            if (file.type !== 'application/pdf') {
                alert('Only PDF documents are allowed for verification.');
                event.target.value = ''; // reset input field
                return;
            }

            // remove existing doc of same type if any
            this.policyDocuments = this.policyDocuments.filter(d => d.type !== type);
            // add new document to array
            this.policyDocuments.push({ type, file, name: file.name });
            console.log(`Document added: ${type}`, file.name);
        }
    }

    // check if a specific document type is uploaded
    hasUploadedDocument(type: string): boolean {
        return this.policyDocuments.some(d => d.type === type);
    }

    // get filename of uploaded document by type
    getUploadedFileName(type: string): string | null {
        const doc = this.policyDocuments.find(d => d.type === type);
        return doc ? doc.name : null;
    }

    // remove an uploaded document by type
    removeDocument(type: string) {
        this.policyDocuments = this.policyDocuments.filter(d => d.type !== type);
        console.log(`Document removed: ${type}`);
    }

    // validate full name contains only letters and spaces
    isValidName(name: string): boolean {
        if (!name) return false;
        // letters and spaces pattern
        return /^[a-zA-Z\s]*$/.test(name);
    }

    // validate email format is correct
    isValidEmail(email: string): boolean {
        if (!email) return false;
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    // validate IFSC code format (4 letters, 0, then 6 alphanumeric)
    isValidIFSC(ifsc: string): boolean {
        if (!ifsc) return false;
        // IFSC pattern: 4 uppercase letters, 0, then 6 alphanumeric
        return /^[A-Z]{4}0[A-Z0-9]{6}$/.test(ifsc);
    }

    // validate bank account has 9 to 18 digits
    isValidBankAccount(acc: string): boolean {
        if (!acc) return false;
        // bank account must be 9 to 18 digits
        return /^\d{9,18}$/.test(acc);
    }

    // validate phone number is exactly 10 digits
    isValidPhone(phone: string): boolean {
        if (!phone) return false;
        return /^[0-9]{10}$/.test(phone);
    }

    // validate aadhar number is exactly 12 digits
    isValidAadhar(aadhar: string): boolean {
        if (!aadhar) return false;
        // aadhar is 12 digits only
        return /^\d{12}$/.test(aadhar);
    }

    // check if step 1 form data is valid (applicant and nominee info)
    isStep1Valid(): boolean {
        const app = this.applicationForm;
        // validate applicant name, age, profession, and income
        if (!this.isValidName(app.applicant.fullName)) return false;
        if (app.applicant.age < 22) return false; // must be 22 or older
        if (!app.applicant.profession) return false; // profession required
        if (app.annualIncome <= 0) return false; // income must be positive

        // validate nominee name, relationship, contact info and bank details
        if (!app.nominee.name || !this.isValidName(app.nominee.name)) return false;
        if (!app.nominee.relationship) return false; // relationship required
        if (!this.isValidEmail(app.nominee.email)) return false; // email must be valid
        if (!this.isValidPhone(app.nominee.phone)) return false; // phone must be 10 digits
        if (!this.isValidBankAccount(app.nominee.bankAccount)) return false; // account must be 9-18 digits
        if (!this.isValidIFSC(app.nominee.ifsc)) return false; // IFSC must be valid
        if (!this.isValidAadhar(app.nominee.aadharNumber)) return false; // aadhar must be 12 digits

        // validate family members if family policy selected
        if (this.selectedCategory?.categoryId === 'FAMILY') {
            // at least one family member required
            if (app.familyMembers.length === 0) return false;
            // each family member must have valid name, relation and date of birth
            for (const member of app.familyMembers) {
                if (!this.isValidName(member.fullName) || !member.relation || !member.dateOfBirth) return false;
            }
        }
        return true;
    }

    // check if step 2 form data is valid (address and documents)
    isStep2Valid(): boolean {
        const app = this.applicationForm;
        // validate address and pincode are provided
        if (!app.location.address || !app.location.pincode) return false;

        // check all mandatory documents are uploaded
        const mandatoryDocs = ['IdentityProof', 'AgeProof', 'IncomeProof', 'MedicalReport'];
        for (const type of mandatoryDocs) {
            if (!this.hasUploadedDocument(type)) return false;
        }
        return true;
    }

    // check if entire application form is valid
    isApplicationFormValid(): boolean {
        // both step 1 and step 2 must be valid
        return this.isStep1Valid() && this.isStep2Valid();
    }

    // submit insurance application with form data and upload documents
    submitApplication() {
        // don't submit if already submitting
        if (this.isSubmitting()) return;

        // set submitting flag
        this.isSubmitting.set(true);
        // build application request with all form data
        const request = {
            policyCategory: this.selectedCategory.categoryId,
            tierId: this.selectedTier.tierId,
            // send applicant as 'applicant' for INDIVIDUAL, as 'primaryApplicant' for others
            applicant: this.selectedCategory.categoryId === 'INDIVIDUAL' ? this.applicationForm.applicant : null,
            primaryApplicant: (this.selectedCategory.categoryId !== 'INDIVIDUAL') ? this.applicationForm.applicant : null,
            // map family members with fallback dates
            familyMembers: this.applicationForm.familyMembers.map((fm: any) => ({
                ...fm,
                dateOfBirth: fm.dateOfBirth || this.today // fallback to today if not set
            })),
            paymentMode: this.applicationForm.paymentMode,
            nominee: this.applicationForm.nominee,
            annualIncome: this.applicationForm.annualIncome,
            location: this.applicationForm.location,
            vehicleType: this.applicationForm.applicant.vehicleType
        };

        // send application to backend
        this.policyService.applyForPolicy(request).subscribe({
            next: (res) => {
                // get application ID from response
                const applicationId = res.message; // Id returned from backend

                // check if there are documents to upload
                if (this.policyDocuments.length > 0) {
                    // set uploading flag
                    this.isUploadingDocs.set(true);
                    // upload documents
                    this.policyService.submitDocuments(applicationId, this.policyDocuments).subscribe({
                        next: () => {
                            // success: hide uploading and submitting flags
                            this.isUploadingDocs.set(false);
                            this.isSubmitting.set(false);
                            // show success modal and refresh policies
                            this.showAppSuccessModal.set(true);
                            this.loadMyPolicies();
                        },
                        error: (err) => {
                            // error: still mark as submitted and let user proceed
                            this.isUploadingDocs.set(false);
                            this.isSubmitting.set(false);
                            alert('Application submitted but document upload failed. You can upload them later from policy details.');
                            this.loadMyPolicies();
                            this.switchView('my-policies');
                        }
                    });
                } else {
                    // no documents: simply mark as done
                    this.isSubmitting.set(false);
                    this.showAppSuccessModal.set(true);
                    this.loadMyPolicies();
                }
            },
            error: (err) => {
                // submission failed
                this.isSubmitting.set(false);
                alert('Failed to submit application: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    // close success modal and return to policies view
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
        let applicant = normalize(fullDetails.applicant || raw.Applicant || fullDetails.primaryApplicant || raw.PrimaryApplicant) || {};
        if (!applicant.fullName) applicant.fullName = pol.user?.fullName || pol.user?.userName || 'N/A';

        applicant.age = applicant.age || raw.Age || pol.age || pol.Age || '--';
        applicant.profession = applicant.profession || raw.Profession || pol.profession || pol.Profession || 'Standard';
        applicant.annualIncome = applicant.annualIncome || raw.AnnualIncome || pol.annualIncome || pol.AnnualIncome || 0;
        applicant.alcoholHabit = applicant.alcoholHabit || raw.AlcoholHabit || pol.alcoholHabit || pol.AlcoholHabit || 'None';
        applicant.smokingHabit = applicant.smokingHabit || raw.SmokingHabit || pol.smokingHabit || pol.SmokingHabit || 'None';
        applicant.vehicleType = applicant.vehicleType || raw.VehicleType || pol.vehicleType || pol.VehicleType || 'None';
        applicant.travelKmPerMonth = applicant.travelKmPerMonth || raw.TravelKmPerMonth || pol.travelKmPerMonth || pol.TravelKmPerMonth || 0;

        fullDetails.applicant = applicant;

        // Ensure location is normalized with fallback to flat fields
        const loc = normalize(fullDetails.location || raw.Location || raw.location || {});
        fullDetails.location = {
            address: loc.address || pol.address || pol.Address || 'No address provided',
            latitude: loc.latitude || pol.latitude || pol.Latitude || null,
            longitude: loc.longitude || pol.longitude || pol.Longitude || null,
            state: loc.state || pol.state || pol.State || '',
            district: loc.district || pol.district || pol.District || '',
            pincode: loc.pincode || pol.pincode || pol.Pincode || ''
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

    // convert payment mode string to standard format (yearly, halfyearly, monthly)
    normalizePaymentMode(mode: string): string {
        // convert to lowercase and remove spaces/dashes
        const s = (mode || 'yearly').toLowerCase().replace(/[\s\-_]+/g, '');
        // check for half yearly variations
        if (s.includes('half') || s.includes('biannual') || s.includes('semi')) return 'halfyearly';
        // check for monthly
        if (s.includes('month')) return 'monthly';
        // default to yearly
        return 'yearly';
    }

    // calculate periodic amount owed based on payment mode
    getPeriodicPaymentAmount(pol: any): number {
        if (!pol) return 0;
        // normalize payment mode
        const mode = this.normalizePaymentMode(pol.paymentMode);
        const total = pol.calculatedPremium || 0;
        // divide total by number of payments per year
        if (mode === 'monthly') return total / 12; // 12 payments
        if (mode === 'halfyearly') return total / 2; // 2 payments
        return total; // 1 payment for yearly
    }

    // get display label for payment frequency
    getPaymentFrequencyLabel(pol: any): string {
        if (!pol) return 'Per Year';
        const mode = this.normalizePaymentMode(pol.paymentMode);
        if (mode === 'monthly') return 'Per Month';
        if (mode === 'halfyearly') return 'Per 6 Months';
        return 'Per Year';
    }

    // process premium payment for policy from detailed view
    payPremiumFromDetails() {
        // get the detailed policy from signal
        const pol = this.detailedPolicy();
        if (!pol) return;

        // calculate payment amount based on payment mode
        const amount = this.getPeriodicPaymentAmount(pol);
        // ask user to confirm payment
        if (!confirm(`Confirm payment of â‚¹${amount.toFixed(2)} for ${pol.tierId} policy?`)) return;

        // execute payment if confirmed
        this.executePayment();
    }

    // execute payment transaction for the policy
    executePayment() {
        // get detailed policy from signal
        const pol = this.detailedPolicy();
        if (!pol) return;

        // set paying flag to show loading state
        this.isPaying.set(true);
        // calculate amount to pay based on payment mode
        const amountToPay = this.getPeriodicPaymentAmount(pol);
        // send payment request to backend
        this.policyService.processPayment(pol.id, amountToPay).subscribe({
            next: () => {
                // payment succeeded
                this.isPaying.set(false);
                // close payment modal
                this.showPaymentModal.set(false);

                // send invoice email via n8n
                this.sendInvoiceEmail(pol);

                alert('Payment Successful! Your policy is now ACTIVE. Invoice sent to your email.');

                // refresh policies and show updated policy details
                this.policyService.getMyPolicies().subscribe((policies) => {
                    this.myPolicies.set(policies);
                    this.calculateTotals();
                    this.openPolicyDetails(pol.id);
                });
            },
            error: (err) => {
                // payment failed or returned error
                this.isPaying.set(false);
                // get error message from response
                const errorMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.message || '');
                // check for false errors (payment succeeded but backend returned error code)
                // e.g., status mismatch, timeout from n8n, etc.
                if (errorMsg.includes('status') || errorMsg.includes('AwaitingPayment') || errorMsg.includes('unexpected error') || errorMsg.includes('An unexpected error')) {
                    // treat as success anyway
                    this.showPaymentModal.set(false);
                    // send invoice email even on pseudo-error
                    this.sendInvoiceEmail(pol);

                    alert('Payment processed. Your policy is now ACTIVE. Invoice sent to your email.');

                    // refresh policies and show updated details
                    this.policyService.getMyPolicies().subscribe((policies) => {
                        this.myPolicies.set(policies);
                        this.calculateTotals();
                        this.openPolicyDetails(pol.id);
                    });
                } else {
                    // real error
                    this.showPaymentModal.set(false);
                    alert('Payment failed: ' + errorMsg);
                }
            }
        });
    }

    // open detailed view of a specific claim with policy and nominee info
    openClaimDetails(claimId: string) {
        // find claim from claims list
        const claim = this.myClaims().find(c => c.id === claimId);
        if (!claim) return;

        // parse policy json to get nominee information
        let details: any = null;
        if (claim.policy?.applicationDataJson) {
            try {
                const raw = JSON.parse(claim.policy.applicationDataJson);
                // helper to normalize object keys to lowercase first letter
                const normalize = (obj: any) => {
                    if (!obj) return null;
                    const normalized: any = {};
                    Object.keys(obj).forEach(key => {
                        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                        normalized[normalizedKey] = obj[key];
                    });
                    return normalized;
                };

                // normalize full details
                const fullDetails = normalize(raw);
                if (fullDetails) {
                    // extract and normalize nominee from multiple possible keys
                    fullDetails.nominee = normalize(fullDetails.nominee || raw.Nominee) || {};
                    // map different possible key names to standard names
                    fullDetails.nominee.name = fullDetails.nominee.name || fullDetails.nominee.nomineeName || '--';
                    fullDetails.nominee.email = fullDetails.nominee.email || fullDetails.nominee.nomineeEmail || '--';
                    fullDetails.nominee.phone = fullDetails.nominee.phone || fullDetails.nominee.nomineePhone || '--';
                    fullDetails.nominee.bankAccount = fullDetails.nominee.bankAccount || fullDetails.nominee.nomineeBankAccountNumber || '--';
                    fullDetails.nominee.relationship = fullDetails.nominee.relationship || fullDetails.nominee.nomineeRelationship || '--';
                }
                details = fullDetails;
            } catch (e) { }
        }

        // set detailed claim in signal for template
        this.detailedClaim.set({ ...claim, fullDetails: details });
        this.selectedClaimId.set(claimId);
        // switch to claim details view
        this.activeView.set('claim-details');
    }

    // process premium payment for selected policy from modal
    payPremium() {
        // get selected policy from signal
        const pol = this.selectedPolicy();
        if (!pol) return;

        // calculate payment amount based on payment mode
        const amount = this.getPeriodicPaymentAmount(pol);
        // ask user to confirm payment
        if (!confirm(`Confirm payment of â‚¹${amount.toFixed(2)} for ${pol.tierId} policy?`)) return;

        // set paying flag
        this.isPaying.set(true);
        // send payment request to backend
        this.policyService.processPayment(pol.id, amount).subscribe({
            next: (res) => {
                // payment success
                this.isPaying.set(false);
                // close modal
                this.showPolicyDetailModal.set(false);

                // send invoice email via n8n
                this.sendInvoiceEmail(pol);

                alert('Payment Successful! Your policy is now ACTIVE. Invoice sent to your email.');

                // refresh policies and show updated details
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

                    // âœ… Send invoice email even on pseudo-error if it means success
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

    // handle when nominee verification component emits verified event
    onNomineeVerified(event: { aadhar: File | null, photo: File, aadharUrl?: string, photoUrl?: string }) {
        // store nominee aadhar file
        this.nomineeAadharFile.set(event.aadhar);
        // store nominee selfie photo
        this.nomineePhotoFile.set(event.photo);
        // store aadhar URL from imgkit
        this.nomineeAadharUrl.set(event.aadharUrl || '');
        // store photo URL from imgkit
        this.nomineePhotoUrl.set(event.photoUrl || '');
        // mark nominee as verified
        this.isNomineeVerified.set(true);
    }

    // initialize claim form for a selected policy and reset all fields
    initiateClaim(pol: any) {
        // store selected policy
        this.selectedPolicyForClaim.set(pol);
        // reset claim form with default values
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
        // reset claim step to step 1
        this.claimStep.set(1);
        // clear uploaded claim documents
        this.claimFiles = [];
        // reset document flags
        this.hasFirReport.set(false);
        this.hasHospitalBill.set(false);
        this.hasDeathCertificate.set(false);
        // reset nominee verification
        this.isNomineeVerified.set(false);
        this.nomineeAadharFile.set(null);
        this.nomineePhotoFile.set(null);
        // reset location
        this.selectedLocationCoords.set(null);
        // switch to claim raising view
        this.switchView('raise-claim');
    }

    // calculate number of days hospitalized between admission and discharge
    get hospitalDays(): number {
        if (!this.claimForm.admissionDate || !this.claimForm.dischargeDate) return 0;
        const d1 = new Date(this.claimForm.admissionDate);
        const d2 = new Date(this.claimForm.dischargeDate);
        const diffTime = Math.abs(d2.getTime() - d1.getTime());
        // convert milliseconds to days and round up
        return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    }

    // calculate total medical cost from all expense categories
    calculateTotalMedicalCost() {
        this.claimForm.estimatedMedicalCost = (this.claimForm.hospitalBill || 0) + (this.claimForm.medicines || 0) + (this.claimForm.otherExpenses || 0);
    }

    // suggest claim amount as 90% of estimated medical cost
    get suggestedClaimAmount(): number {
        return this.claimForm.estimatedMedicalCost * 0.9;
    }

    // populate claim amount with the suggested amount
    populateSuggestedAmount() {
        this.claimForm.requestedAmount = this.suggestedClaimAmount;
    }

    // check if all required claim data is complete
    get isClaimDataComplete(): boolean {
        // for death claims, need bill, fir, death cert and nominee verification
        if (this.claimForm.incidentType === 'Death') {
            return this.hasHospitalBill() && this.hasFirReport() && this.hasDeathCertificate() && this.isNomineeVerified();
        }
        // for other claims, need hospital bill and hospital name
        return this.hasHospitalBill() && this.claimForm.hospitalName !== '';
    }

    // handle file upload for claim supporting documents
    onFileChange(event: any, type: 'fir' | 'bill' | 'death' | 'others' = 'others') {
        // check if files selected
        if (event.target.files.length > 0) {
            // convert file list to array
            const files = Array.from(event.target.files) as File[];
            // add to claim files array
            this.claimFiles = [...this.claimFiles, ...files];

            // set flags based on document type
            if (type === 'fir') this.hasFirReport.set(true);
            if (type === 'bill') this.hasHospitalBill.set(true);
            if (type === 'death') this.hasDeathCertificate.set(true);
        }
    }

    // submit insurance claim with all form data and documents to backend
    submitClaim() {
        // get selected policy
        const pol = this.selectedPolicyForClaim();
        if (!pol) return;

        // validate that incident date is after or on policy start date
        if (pol.startDate && this.claimForm.incidentDate) {
            const start = new Date(pol.startDate);
            const incident = new Date(this.claimForm.incidentDate);
            // set hours to 0 for date-only comparison
            start.setHours(0, 0, 0, 0);
            incident.setHours(0, 0, 0, 0);

            // show error if incident before policy start
            if (incident < start) {
                alert(`Cannot raise a claim for an incident before the policy start date (${start.toLocaleDateString()}).`);
                return;
            }
        }

        // set submitting flag
        this.isSubmitting.set(true);
        // create form data object for multipart upload
        const formData = new FormData();
        // add policy ID
        formData.append('policyApplicationId', this.selectedPolicyForClaim()!.id);
        // add incident details
        formData.append('incidentDate', this.claimForm.incidentDate);
        formData.append('incidentTime', this.claimForm.incidentTime || '');
        formData.append('incidentType', this.claimForm.incidentType);
        formData.append('accidentCause', this.claimForm.accidentCause);
        formData.append('policeCaseFiled', this.claimForm.policeCaseFiled.toString());
        formData.append('firNumber', this.claimForm.firNumber || '');
        formData.append('incidentLocation', this.claimForm.incidentLocation);

        // add location coordinates if selected
        const coords = this.selectedLocationCoords();
        if (coords) {
            formData.append('latitude', coords.lat.toString());
            formData.append('longitude', coords.lng.toString());
        }

        // add claim description
        formData.append('description', this.claimForm.description);
        // add hospital information
        formData.append('hospitalName', this.claimForm.hospitalName);
        formData.append('hospitalizationRequired', this.claimForm.hospitalizationRequired.toString());
        formData.append('admissionDate', this.claimForm.admissionDate || '');
        formData.append('dischargeDate', this.claimForm.dischargeDate || '');
        // add injury details
        formData.append('injuryType', this.claimForm.injuryType);
        formData.append('bodyPartInjured', this.claimForm.bodyPartInjured);
        // add medical costs
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

        // attach uploaded claim documents
        this.claimFiles.forEach(file => {
            formData.append('documents', file, file.name);
        });

        // for death claims, attach nominee documents
        if (this.claimForm.incidentType === 'Death') {
            if (this.nomineeAadharFile()) {
                formData.append('documents', this.nomineeAadharFile()!, 'Nominee_Aadhar.jpg');
            }
            if (this.nomineePhotoFile()) {
                formData.append('documents', this.nomineePhotoFile()!, 'Nominee_Photo.jpg');
            }
        }

        // send claim to backend
        this.claimService.raiseClaim(formData).subscribe({
            next: () => {
                // success
                this.isSubmitting.set(false);
                // show success modal
                this.showClaimSuccessModal.set(true);
                // refresh claims list
                this.loadMyClaims();
            },
            error: (err) => {
                // error
                this.isSubmitting.set(false);
                alert('Failed to raise claim: ' + (err.error?.message || 'Server error'));
            }
        });
    }

    // close claim success modal and switch to claims view
    closeClaimSuccessModal() {
        this.showClaimSuccessModal.set(false);
        this.switchView('my-claims');
    }

    // handle when user selects an incident location from google places
    onLocationSelected(data: any) {
        // check if data is string or object with coordinates
        if (typeof data === 'string') {
            // string data: just the address
            this.claimForm.incidentLocation = data;
        } else {
            // object data: has address and coordinates
            this.claimForm.incidentLocation = data.address;
            // store latitude and longitude
            this.selectedLocationCoords.set({ lat: data.lat, lng: data.lng });
        }
        console.log('Location updated in dashboard form:', data);
    }

    // get minimum date for claim (policy start date)
    getMinDateForClaim(): string {
        const pol = this.selectedPolicyForClaim();
        if (!pol || !pol.startDate) return '';
        const d = new Date(pol.startDate);
        // convert to YYYY-MM-DD format for input
        return d.toISOString().split('T')[0];
    }

    // handle location selection for policy purchase address
    onPolicyLocationSelected(data: any) {
        // check if data is string or object with coordinates
        if (typeof data === 'string') {
            // string data: just the address
            this.applicationForm.location.address = data;
        } else {
            // object data: has full location info with components
            this.applicationForm.location.address = data.address;
            this.applicationForm.location.latitude = data.lat;
            this.applicationForm.location.longitude = data.lng;
            this.selectedPolicyLocationCoords.set({ lat: data.lat, lng: data.lng });

            // extract location components if available
            if (data.components) {
                this.applicationForm.location.state = data.components.state || '';
                this.applicationForm.location.district = data.components.district || '';
                this.applicationForm.location.area = data.components.area || '';
                this.applicationForm.location.pincode = data.components.pincode || '';
            }
        }
        console.log('Policy location updated:', data);
    }

    // handle when user selects a hospital from autocomplete
    onHospitalChanged(data: any) {
        // check if data is string or object
        if (typeof data === 'string') {
            // string: just hospital name
            this.claimForm.hospitalName = data;
            this.selectedHospitalDetails.set(null);
        } else {
            // object: has coordinates and details
            this.claimForm.hospitalName = data.address;
            // store coordinates for map
            this.selectedHospitalCoords.set({ lat: data.lat, lng: data.lng });
            // store hospital details
            this.selectedHospitalDetails.set(data.components || null);
        }
        console.log('Hospital updated with details:', data);
    }

    // create PDF invoice, upload to imagekit, then send via n8n email
    private sendInvoiceEmail(pol: any) {
        // get current user
        const user = this.authService.getUser();
        // create new PDF document
        const doc = new jsPDF();

        // 1. Generate PDF content
        doc.setFontSize(22);
        doc.text('ACCISURE INSURANCE INVOICE', 105, 20, { align: 'center' });
        doc.setFontSize(10);
        doc.text(`Date: ${new Date().toLocaleDateString()}`, 105, 28, { align: 'center' });

        doc.line(20, 35, 190, 35);

        // customer details
        doc.setFontSize(12);
        doc.text('Customer Details:', 20, 45);
        doc.setFontSize(10);
        doc.text(`Name: ${user.name || 'Valued Customer'}`, 20, 52);
        doc.text(`Email: ${user.email}`, 20, 58);

        // policy information
        doc.setFontSize(12);
        doc.text('Policy Information:', 120, 45);
        doc.setFontSize(10);
        doc.text(`Policy ID: ${pol.id}`, 120, 52);
        doc.text(`Plan: ${pol.tierId || pol.policyName}`, 120, 58);
        doc.text(`Category: ${pol.policyCategory || 'Standard'}`, 120, 64);

        // payment details table
        autoTable(doc, {
            startY: 80,
            head: [['Description', 'Amount']],
            body: [
                ['Premium Payment', `INR ${this.getPeriodicPaymentAmount(pol)}`],
                ['Service Tax (GST)', 'Included'],
            ],
            foot: [['Total Paid', `INR ${this.getPeriodicPaymentAmount(pol)}`]],
            theme: 'striped',
            headStyles: { fillColor: [1, 33, 67] }
        });

        // 2. Convert to Base64 and upload
        const pdfBase64 = doc.output('datauristring');

        this.policyService.uploadInvoice(pol.id, pdfBase64, `Invoice_${pol.id}.pdf`).subscribe({
            next: (uploadRes) => {
                // get permanent URL from imagekit
                const permanentUrl = uploadRes.invoiceUrl;

                // 3. Send email via n8n with invoice link
                const payload = {
                    customerEmail: user.email,
                    customerName: user.name || user.email,
                    phoneNumber: user.phone || 'Not Provided',
                    policyId: pol.id,
                    policyName: pol.tierId || pol.policyName || 'Accidental Insurance Policy',
                    amount: this.getPeriodicPaymentAmount(pol),
                    paymentDate: new Date().toLocaleDateString('en-IN', {
                        day: '2-digit', month: 'short', year: 'numeric'
                    }),
                    invoiceLink: permanentUrl
                };
                // send email payload to n8n webhook
                this.http.post(n8nWebhooks.sendInvoice, payload)
                    .subscribe({
                        next: (res) => console.log('Invoice email sent with working link:', res),
                        error: (err) => console.error('Invoice email failed:', err)
                    });
            },
            error: (uploadErr) => {
                // error uploading PDF
                console.error('Invoice PDF upload failed. Sending email with fallback.', uploadErr);
                // still attempt to send email with fallback invoice link
                this.http.post('https://sanjay29n8n.app.n8n.cloud/webhook/send-invoice', {
                    customerEmail: user.email,
                    customerName: user.name || user.email,
                    phoneNumber: user.phone || 'Not Provided',
                    policyId: pol.id,
                    policyName: pol.tierId || pol.policyName || 'Accidental Insurance Policy',
                    amount: this.getPeriodicPaymentAmount(pol),
                    paymentDate: new Date().toLocaleDateString('en-IN', {
                        day: '2-digit', month: 'short', year: 'numeric'
                    }),
                    invoiceLink: `https://ik.imagekit.io/nextbyteind/invoices/Invoice_${pol.id}.pdf`
                }).subscribe();
            }
        });
    }

    // logout current user and clear session
    logout() {
        this.authService.logout();
    }

    // expose this component instance to child partial components
    get self(): this { return this; }

    // open AI chat helper for a specific policy tier
    openChatHelper(tier: any) {
        if (!this.selectedCategory) return;

        // build policy data object for chat context
        const policyData = {
            policyId: tier.tierId,
            policyName: tier.tierName,
            category: this.selectedCategory.categoryName,
            coverageAmount: tier.baseCoverageAmount,
            premium: tier.basePremiumAmount,
            benefits: tier.benefits
        };

        // set current policy for chat
        this.currentChatPolicy.set(policyData);
        // clear previous messages
        this.chatMessages.set([]);
        // open chat window
        this.isChatOpen.set(true);
        // set loading state
        this.isChatLoading.set(true);

        // send initial greeting to trigger AI response
        const initialText = "Hi"; // trigger first response from AI
        this.sendChatMessage(initialText, true);
    }

    // send chat message to AI policy assistant
    sendChatMessage(messageText?: string, isInitial: boolean = false) {
        // get message text from parameter or input signal
        const text = messageText || this.chatUserMessage();
        // return if no text and not initial greeting
        if (!text && !isInitial) return;

        // add user message to chat if not initial
        if (!isInitial) {
            this.chatMessages.update((msgs: any[]) => [...msgs, { role: 'user', content: text }]);
            // clear input field
            this.chatUserMessage.set('');
        }

        // set loading state
        this.isChatLoading.set(true);

        // get current user data
        const freshUser = this.authService.getUser();

        // build payload with customer and policy context
        const payload = {
            customer: {
                id: freshUser.id,
                name: freshUser.name,
                email: freshUser.email,
                phone: freshUser.phone
            },
            policy: this.currentChatPolicy(),
            message: text,
            question: text // fallback for n8n AI nodes
        };

        console.log('[ChatHelper] Sending payload to n8n:', JSON.stringify(payload));

        // send chat message to backend
        this.policyService.sendChatQuestion(payload).subscribe({
            next: (res) => {
                // turn off loading
                this.isChatLoading.set(false);

                // get AI response from backend
                let aiReply = res.reply || res.answer ||
                    "I'm sorry, I couldn't get a response. Please try again.";

                // clean markdown symbols from response
                aiReply = aiReply
                    .replace(/[*_`#>-]/g, '')     // remove symbols
                    .replace(/\n{2,}/g, '\n')     // remove extra line breaks
                    .trim();

                // add AI response to messages
                this.chatMessages.update((msgs: any[]) => [
                    ...msgs,
                    { role: 'bot', content: aiReply }
                ]);
            },
            error: (err) => {
                // error getting response
                this.isChatLoading.set(false);
                // show error message in chat
                this.chatMessages.update((msgs: any[]) => [...msgs, { role: 'bot', content: "Error: Could not reach the policy assistant." }]);
            }
        });
    }

    // close chat window
    closeChat() {
        this.isChatOpen.set(false);
    }

    // handle aadhar file upload for KYC
    onAadharFileChange(event: any) {
        if (event.target.files.length > 0) {
            // get uploaded file
            const file = event.target.files[0];
            // store in signal
            this.aadharFile.set(file);
            // clear previous errors
            this.kycError.set(null);
            this.kycSuccessMsg.set(null);
            // create file reader
            const reader = new FileReader();
            reader.onload = async () => {
                // show preview
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

                        let hasName = false;
                        let hasDob = false;
                        let hasAadhar = false;

                        // Extract Name heuristic
                        const dobIndex = lines.findIndex((l: string) => /DOB|Year of Birth/i.test(l));
                        if (dobIndex > 0) {
                            let possibleName = lines[dobIndex - 1].trim();
                            possibleName = possibleName.replace(/^[^a-zA-Z]+/, '').trim();
                            if (possibleName.length > 3) {
                                parsedDetails.push(`Name: ${possibleName}`);
                                hasName = true;
                            }
                        }

                        // Extract DOB
                        const dobMatch = fullText.match(/DOB[:\.\s]*(\d{2}\/\d{2}\/\d{4})/i) || fullText.match(/Year of Birth.*(\d{4})/i);
                        if (dobMatch) {
                            parsedDetails.push(`Date of Birth: ${dobMatch[1]}`);
                            hasDob = true;
                        }

                        // Extract 12-digit Aadhar pattern
                        const aadharMatch = fullText.match(/\d{4}[\s-]+\d{4}[\s-]+\d{4}/) || fullText.match(/\d{12}/);
                        if (aadharMatch) {
                            parsedDetails.push(`Aadhar Number: ${aadharMatch[0].trim()}`);
                            hasAadhar = true;
                        }

                        if (hasName && hasDob && hasAadhar) {
                            this.aadharText.set(`Key Details Extracted Successfully!\n\n${parsedDetails.join('\n')}\n\nNote: These details are for verification convenience.`);
                        } else {
                            // some fields missing
                            const missingFields = [];
                            if (!hasName) missingFields.push("Name");
                            if (!hasDob) missingFields.push("Date of Birth");
                            if (!hasAadhar) missingFields.push("Aadhar Number");

                            // show which fields are missing
                            this.aadharText.set(`Missing required fields: ${missingFields.join(', ')}\n\nPlease ensure the uploaded image contains these details.`);
                            this.kycError.set("Please upload correct file");
                        }
                    } else {
                        // no text found
                        this.aadharText.set("Could not automatically extract readable text. Please ensure the image is clear.");
                        this.kycError.set("Please upload correct file");
                    }
                } catch (e) {
                    // Vision API error
                    console.error("KYC Vision API failed:", e);
                    this.aadharText.set("Extraction temporarily unavailable. Verification will proceed via face recognition.");
                }
            };
            // read file as data URL for preview
            reader.readAsDataURL(file);
        }
    }

    // activate device camera for selfie capture
    async activateCamera() {
        this.isCameraActive.set(true);
        try {
            // request camera access
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            // store stream  in signal
            this.cameraStream.set(stream);
            // attach stream to video element after small delay
            setTimeout(() => {
                const video = document.getElementById('camera-video') as HTMLVideoElement;
                if (video) video.srcObject = stream;
            }, 100);
        } catch (err) {
            // camera access denied
            console.error('Error accessing camera:', err);
            alert('Could not access camera.');
        }
    }

    // capture photo from camera as selfie
    takePicture() {
        // get video element
        const video = document.getElementById('camera-video') as HTMLVideoElement;
        // create canvas to draw frame
        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        // draw current video frame to canvas
        canvas.getContext('2d')?.drawImage(video, 0, 0);

        // convert canvas to blob and create file
        canvas.toBlob((blob) => {
            if (blob) {
                // create File from blob
                const file = new File([blob], 'selfie.jpg', { type: 'image/jpeg' });
                // store file
                this.selfieFile.set(file);
                // store preview URL
                this.selfiePreview.set(URL.createObjectURL(blob));
                // stop camera
                this.stopCamera();
            }
        }, 'image/jpeg');
    }

    // stop camera and release media stream
    stopCamera() {
        if (this.cameraStream()) {
            // stop all camera tracks
            this.cameraStream()?.getTracks().forEach(track => track.stop());
            // clear stream
            this.cameraStream.set(null);
        }
        // set camera inactive
        this.isCameraActive.set(false);
    }

    // verify KYC by comparing selfie with aadhar document
    async verifyKyc() {
        // check both documents uploaded
        if (!this.aadharFile() || !this.selfieFile()) return;

        // set verifying flag
        this.isKycVerifying.set(true);
        // clear previous errors
        this.kycError.set(null);
        this.kycSuccessMsg.set(null);

        try {
            // create form data with both images
            const formData = new FormData();
            // selfie for face match
            formData.append('image_file1', this.selfieFile()!);
            // aadhar/PAN for document
            formData.append('image_file2', this.aadharFile()!);

            // send to n8n for verification
            const response = await fetch(n8nWebhooks.kycVerification, {
                method: 'POST',
                body: formData
            });

            // get verification result
            const result = await response.json();
            console.log('n8n KYC Response:', result);

            // check if we got confidence score
            if (result.confidence !== undefined) {
                // check if confidence is high enough (80%)
                if (result.confidence > 80) {
                    // success
                    this.kycSuccessMsg.set('Face Verified Successfully! Same Person. Similarity Score: ' + result.confidence);
                    // mark as verified
                    this.isKycVerified.set(true);
                    // store in local storage
                    if (this.user.id) localStorage.setItem('isKycVerified_' + this.user.id, 'true');

                    // update backend KYC status
                    if (this.user.id) {
                        this.authService.completeKyc(this.user.id).subscribe({
                            next: () => console.log('Backend KYC Status updated successfully'),
                            error: (err) => console.error('Failed to sync KYC with backend', err)
                        });
                    }

                    // redirect to dashboard after 3 seconds
                    setTimeout(() => {
                        this.switchView('dashboard');
                    }, 3000);
                } else {
                    // confidence too low
                    this.kycError.set('Face Not Matching. Similarity Score: ' + result.confidence);
                }
            } else {
                // no confidence in response
                this.kycError.set('Error: ' + (result.message || 'Unexpected response from verification service.'));
            }
        } catch (err: any) {
            // network or other error
            console.error(err);
            this.kycError.set('Failed to connect to verification service.');
        } finally {
            // turn off verifying flag
            this.isKycVerifying.set(false);
        }
    }

    // voice agent integration functions

    // toggle voice mode on/off
    toggleVoiceMode() {
        // toggle the voice mode state
        const newState = !this.isVoiceMode();
        this.isVoiceMode.set(newState);
        // if turning on, send greeting
        if (newState) {
            // send greeting to start conversation
            this.sendGreeting();
        }
    }

    // send initial greeting when voice mode starts
    sendGreeting() {
        const greetingText = "Hi Sanjay! I'm AcciSure. How can I help you today?";

        // add greeting to chat messages
        this.chatMessages.update(msgs => [
            ...msgs,
            { role: 'agent', content: greetingText }
        ]);

        // set processing flag so mic is paused during greeting
        this.isVoiceProcessing.set(true);
        console.log('[VoiceAgent] Greeting started, mic is paused while speaking...');

        // check if browser supports text-to-speech
        if ('speechSynthesis' in window) {
            // speak greeting text
            this.speakResponse(greetingText, () => {
                console.log('[VoiceAgent] Greeting finished. Ready for user input.');
                // clear processing flag after greeting
                this.isVoiceProcessing.set(false);
            });
        } else {
            // browser doesn't support TTS
            console.warn('[VoiceAgent] SpeechSynthesis not supported by browser. Starting mic immediately.');
            this.isVoiceProcessing.set(false);
        }
    }

    // speak text response using browser text-to-speech
    private speakResponse(text: string, onEnd: () => void) {
        // check if TTS available
        if (!('speechSynthesis' in window)) {
            onEnd();
            return;
        }

        // cancel any existing speech
        window.speechSynthesis.cancel();

        // clean text: remove markdown and special chars
        const cleanText = text
            .replace(/[*_#`]/g, '')           // remove markdown
            .replace(/[-]{2,}/g, ' ')         // replace dashes
            .replace(/\s+/g, ' ')             // clean spaces
            .trim();

        // create speech utterance
        const utterance = new SpeechSynthesisUtterance(cleanText);

        // function to select best voice
        const setVoice = () => {
            const voices = window.speechSynthesis.getVoices();

            // select voice in order of preference: natural > google/microsoft > english > any
            let selectedVoice = voices.find(v => (v.name.toLowerCase().includes('natural') || v.name.toLowerCase().includes('enhanced')) && v.lang.startsWith('en'))
                || voices.find(v => (v.name.includes('Google') || v.name.includes('Microsoft')) && v.lang.startsWith('en'))
                || voices.find(v => v.lang.startsWith('en'))
                || voices[0];

            // set the voice
            if (selectedVoice) {
                utterance.voice = selectedVoice;
                console.log(`[VoiceAgent] Using Intelligent Voice: ${selectedVoice.name}`);
            }
        };

        // voices often load asynchronously
        if (window.speechSynthesis.getVoices().length === 0) {
            // wait for voices to load
            window.speechSynthesis.onvoiceschanged = () => {
                setVoice();
                // speak once voices loaded
                window.speechSynthesis.speak(utterance);
                // one-time use
                window.speechSynthesis.onvoiceschanged = null;
            };
        } else {
            // voices already loaded
            setVoice();
            window.speechSynthesis.speak(utterance);
        }

        // set voice characteristics for natural sound
        utterance.rate = 1.05;    // slightly faster sounds confident
        utterance.pitch = 1.0;    // normal pitch
        utterance.volume = 1.0;   // full volume

        // handle when speech ends
        utterance.onend = () => onEnd();
        // handle speech errors
        utterance.onerror = (e) => {
            console.error('[VoiceAgent] TTS Error:', e);
            onEnd();
        };
    }

    // handle audio captured from voice agent
    async handleAudioCaptured(event: any) {
        // get audio blob from event
        const audioBlob = event as Blob;
        console.log(`[VoiceAgent] â”€â”€ STEP 1: Audio captured. Size=${audioBlob?.size} bytes, Type=${audioBlob?.type}`);

        // check if audio is valid
        if (!audioBlob || audioBlob.size === 0) {
            console.warn('[VoiceAgent] â”€â”€ Captured blob is empty, skipping.');
            return;
        }

        // check if user authenticated
        const currentUser = this.authService.getCurrentUser();
        if (!currentUser) {
            console.error('[VoiceAgent] â”€â”€ No authenticated user, aborting.');
            return;
        }

        // set processing to pause listening
        this.isVoiceProcessing.set(true);

        // get current policy from chat
        const policy = this.currentChatPolicy();
        const policyId = policy?.policyId || 'General';

        // build customer and policy context for backend
        const freshUser = this.authService.getUser();
        const customerContext = JSON.stringify({
            id: freshUser?.id || currentUser?.id,
            name: freshUser?.name || currentUser?.name,
            email: freshUser?.email || currentUser?.email,
            phone: freshUser?.phone || currentUser?.phone
        });
        const policyContext = policy ? JSON.stringify(policy) : '{}';

        // create form data with audio and context
        const formData = new FormData();
        formData.append('audioFile', audioBlob, 'audio.webm');
        formData.append('policyId', policyId);
        formData.append('customerId', freshUser?.id || '');
        formData.append('policyContext', policyContext);      // Full policy object â†’ n8n
        formData.append('customerContext', customerContext);  // Full customer object â†’ n8n

        console.log(`[VoiceAgent] â”€â”€ STEP 2: Sending to backend â†’ policy:`, policy, '| customer:', freshUser);
        console.log('[VoiceAgent] â”€â”€ POST ' + environment.voiceAgentApiUrl);

        try {
            // send to voice API backend
            const response = await this.http.post<any>(environment.voiceAgentApiUrl, formData).toPromise();
            console.log('[VoiceAgent] â”€â”€ STEP 3: Backend response received:', response);

            if (response) {
                // extract response data
                const transcript = response.transcript || response.Transcript || '';
                const aiResponse = response.aiResponse || response.AiResponse || "I'm sorry, I'm having trouble thinking right now.";
                const audioBase64 = response.audioBase64 || response.AudioBase64;

                console.log(`[VoiceAgent] â”€â”€ STEP 4: Transcript from Deepgram: "${transcript}"`);
                console.log(`[VoiceAgent] â”€â”€ STEP 5: AI Response from Groq: "${aiResponse}"`);
                console.log(`[VoiceAgent] â”€â”€ STEP 6: ElevenLabs audio present: ${!!audioBase64}, length: ${audioBase64?.length ?? 0}`);

                // add user and AI messages to chat
                this.chatMessages.update(msgs => [
                    ...msgs,
                    { role: 'user', content: transcript || '(no transcript)' },
                    { role: 'agent', content: aiResponse }
                ]);

                // check if we have audio response from ElevenLabs
                if (audioBase64) {
                    console.log('[VoiceAgent] â”€â”€ STEP 7: Playing ElevenLabs audio response...');
                    // play audio response
                    this.playAudioBase64(audioBase64);
                } else {
                    // no audio, use text-to-speech fallback
                    console.warn('[VoiceAgent] â”€â”€ STEP 7: No audio in response, using SpeechSynthesis fallback...');
                    this.speakResponse(aiResponse, () => {
                        console.log('[VoiceAgent] â”€â”€ Fallback TTS finished, resuming listening...');
                        // clear processing flag after speech
                        this.isVoiceProcessing.set(false);
                    });
                    return; // wait for speech to finish
                }
            } else {
                // empty response
                console.warn('[VoiceAgent] â”€â”€ STEP 3: Empty response from backend.');
                this.isVoiceProcessing.set(false);
            }
        } catch (err: any) {
            // error in processing
            console.error('[VoiceAgent] â”€â”€ ERROR in pipeline:', err);
            console.error('[VoiceAgent] â”€â”€ Status:', err?.status, 'URL:', err?.url);
            this.isVoiceProcessing.set(false);
        }
    }

    // play audio response from ElevenLabs or other source
    private playAudioBase64(base64String: string) {
        try {
            // decode base64 to binary
            const binaryString = window.atob(base64String);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            // convert binary string to bytes
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            // create audio blob from bytes
            const blob = new Blob([bytes], { type: 'audio/mpeg' });
            // create object URL
            const url = URL.createObjectURL(blob);
            // create audio element
            const audio = new Audio(url);
            // store reference for cancellation
            this.currentAudio = audio;

            // keep processing flag true while AI speaks
            // prevents mic from recording over AI response
            this.isVoiceProcessing.set(true);

            // handle when audio finishes
            audio.onended = () => {
                // clean up
                URL.revokeObjectURL(url);
                this.currentAudio = null;
                console.log('[VoiceAgent] â”€â”€ AI audio playback finished, resuming mic listening...');
                // allow listening to resume
                this.isVoiceProcessing.set(false);
            };

            // handle playback error
            audio.onerror = (e) => {
                console.error('[VoiceAgent] â”€â”€ Audio playback error:', e);
                URL.revokeObjectURL(url);
                this.isVoiceProcessing.set(false);
            };

            // play audio
            audio.play().catch(e => {
                console.error('[VoiceAgent] â”€â”€ audio.play() failed:', e);
                this.isVoiceProcessing.set(false);
            });

        } catch (err) {
            console.error("Audio playback failed", err);
        }
    }

    // stop voice mode and clean up resources
    cancelVoiceMode() {
        // stop any ElevenLabs audio playing
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio.src = '';
            this.currentAudio = null;
        }
        // stop any browser TTS
        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel();
        }
        // turn off voice mode
        this.isVoiceMode.set(false);
        // clear processing flag
        this.isVoiceProcessing.set(false);
    }

    // --- Onboarding Tour Methods ---
    // initialize and start the driver.js tour
    startTour() {
        this.showOnboardingModal.set(false);
        this.initOnboardingTour();
    }

    // skip the tour and mark as completed
    skipTour() {
        this.showOnboardingModal.set(false);
        localStorage.setItem('tourCompleted_' + this.user.id, 'true');
    }

    // restart the tour from the header button
    restartTour() {
        this.initOnboardingTour();
    }

    // define the tour steps and configuration
    private initOnboardingTour() {
        this.tourDriver = driver({
            animate: true,
            allowClose: true,
            stagePadding: 10,
            overlayColor: '#0f172a',
            overlayOpacity: 0.85,
            steps: [
                {
                    element: '#tour-kyc-proceed',
                    popover: {
                        title: 'BEGIN YOUR JOURNEY',
                        description: 'Start by verifying your identity to unlock all premium insurance features.',
                        side: 'bottom',
                        popoverClass: 'tour-popover-attention'
                    }
                },
                {
                    element: '#tour-aadhar-upload',
                    popover: {
                        title: 'UPLOAD DOCUMENTS',
                        description: 'Securely upload your Aadhar or PAN card image for instant processing.',
                        side: 'top',
                        popoverClass: 'tour-popover-attention'
                    }
                },
                {
                    element: '#tour-activate-camera',
                    popover: {
                        title: 'IDENTITY VERIFICATION',
                        description: 'Our AI will match your live selfie with your ID for maximum security.',
                        side: 'top',
                        popoverClass: 'tour-popover-attention'
                    }
                },
                {
                    element: '#tour-verify-kyc',
                    popover: {
                        title: 'COMPLETE PROCESS',
                        description: 'Click here to initiate the final AI-driven verification matching.',
                        side: 'top',
                        popoverClass: 'tour-popover-attention'
                    }
                },
                {
                    element: '#tour-sidebar-dashboard',
                    popover: {
                        title: 'BACK TO DASHBOARD',
                        description: 'Return here anytime to view your protection summary and analytics.',
                        side: 'right',
                        popoverClass: 'tour-popover-attention',
                        onNextClick: () => {
                            this.switchView('dashboard');
                            this.tourDriver?.moveNext();
                        }
                    }
                },
                {
                    element: '#tour-sidebar-policies',
                    popover: {
                        title: 'FIND YOUR PLAN',
                        description: 'Explore our tailored insurance plans and start your coverage today.',
                        side: 'right',
                        popoverClass: 'tour-popover-attention'
                    }
                },
                {
                    element: '#tour-ai-voice',
                    popover: {
                        title: 'END-TO-END GUIDANCE',
                        description: 'For more guidance, click here where our agent will guide you end-to-end in terms of analysis.',
                        side: 'left',
                        popoverClass: 'tour-popover-attention'
                    }
                }
            ],
            onDestroyed: () => {
                // save that user has interacted with/completed the tour
                localStorage.setItem('tourCompleted_' + this.user.id, 'true');
            }
        });

        this.tourDriver.drive();
    }
}