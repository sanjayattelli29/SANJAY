# Backend Insurance Platform - Ultimate Developer Guide & Technical Documentation

## Table of Contents
1. [Overview & Platform Design](#1-overview--platform-design)
2. [Unique Selling Propositions (Innovations)](#2-unique-selling-propositions-innovations)
3. [Architecture: Clean / Onion Methodology](#3-architecture-clean--onion-methodology)
4. [Deep Dive: API Controllers Layer](#4-deep-dive-api-controllers-layer)
5. [Deep Dive: Infrastructure Services & Implementations](#5-deep-dive-infrastructure-services--implementations)
6. [Application Layer: DTOs & Orchestration](#6-application-layer-dtos--orchestration)
7. [Domain Layer: Database Schema & Entities](#7-domain-layer-database-schema--entities)
8. [Database Configuration & Entity Framework](#8-database-configuration--entity-framework)
9. [Notification & Real-Time Hubs](#9-notification--real-time-hubs)
10. [Testing & Maintenance](#10-testing--maintenance)

---

## 1. Overview & Platform Design
The **Insurance Platform Backend** serves as the central brain of the AcciSure ecosystem. It is an enterprise-scale RESTful API and WebSocket system developed in **.NET Core SDK (C#)**. Harnessing **Entity Framework Core**, it maintains a rich relational structure over SQL Server databases while exposing safe, authenticated endpoints for Angular Web clients. It utilizes Clean Architecture mapped heavily around strict interface boundaries to encapsulate business logic seamlessly.

---

## 2. Unique Selling Propositions (Innovations)
Traditional insurance platform backends merely execute "Simple CRUD operations". The AcciSure backend incorporates revolutionary workflows seamlessly integrated into its micro-service-like service layers:

### 2.1. AI Systems Integration (N8N, LLMs)
- The backend features orchestration hooks that call N8N automation scripts. When a user submits documents, the `ClaimService` queries external AI endpoints to receive an **AI Claims Summary** returning a risk profile and automated payout ranges without requiring immediate human parsing. This executes in a non-blocking background thread.

### 2.2. Automated Staff Credential Delivery
- When Admins create new Agents via the API, the system automatically triggers a background **Send Email Automation** task using SMTP networks (SendGrid/MailKit) delivering secure, temporary onboarding credentials to the designated staff immediately.

### 2.3. Live Workload Assignments
- Instead of arbitrary task distribution, the backend dynamically calculates `ClaimOfficerWorkloadDto` in real-time. Claims form queues based on active open counts per officer, ensuring faster resolution times via the `AdminService` queries mapped through LINQ structures.

### 2.4. SignalR WhatsApp-Style Chat & Notifications
- Fully implemented native WebSockets provide bidirectional streaming. The `ChatHub` supports continuous, real-time message streams, mimicking modern text applications seamlessly across user sessions bridging Customers with Agents, bypassing entirely synchronous polling bottlenecks.

### 2.5. Coverage Balance Auto-Deductions
- Post-payout generation (when a claim validates), the backend automatically recalculates complex business entities, applying real-time auto-deductions directly mapped against the active `PolicyApplication` coverage balances executing directly across atomic EF Core database transactions.

---

## 3. Architecture: Clean / Onion Methodology
The Application relies heavily on strict boundaries separating logic:
- **`API/`**: Contains only mapping logic (`[HttpGet]`, `[HttpPost]`) and `Authentication` rules. Needs nothing of SQL. Pure presentation layer.
- **`Application/`**: Contains the Interfaces (`IClaimService`, `IAuthService`) and DTO schemas mapping the flow of data out of the Core.
- **`Domain/`**: Pure Object-oriented C# records modeling databases (`ApplicationUser`, `InsuranceClaim`, `PolicyConfiguration`). Zero database logic resides here, pure entity definitions.
- **`Infrastructure/`**: Dependency injection classes that talk purely to the database context or cloud APIs. Implements the application interfaces connecting to SQL Server and ImageKit CDN.

---

## 4. Deep Dive: API Controllers Layer
The `.NET` Web API exposes logic via specialized controllers mapping specifically to entities or system requirements. Each is heavily decorated with `[Authorize]` tags verifying JWTs.

### 4.1. `AuthController.cs` (`/api/auth`)
- Responsible for provisioning initial access tokens and creating non-internal staff system users.
- **`POST /Register`**: Ingests `RegisterCustomerDto`, runs password strength algorithmic tests, uses `.NET Identity` `UserManager` to construct hashes. Applies OTP and CAPTCHA validation flows to lock down route abuse.
- **`POST /Login`**: Parses `LoginDto`. Extracts Claims based on user Roles (e.g. `Customer`, `Admin`), signs a JSON Web Token with a Symmetric Security Key, and dictates the Bearer token lifetime.

### 4.2. `AdminController.cs` (`/api/admin`)
- Restricted securely to `[Authorize(Roles="Admin")]`.
- **`GET /DashboardStats`**: Summarizes system-wide parameters spanning active financial revenue, total logged-in users, outstanding pending claims, and overall policy uptake. Exposes optimized aggregates avoiding N+1 EF Core query pitfalls.
- **`POST /CreateAgent`** & **`POST /CreateClaimOfficer`**: System access routing allowing Admins to spin up internal user profiles. Triggers immediate automated onboarding email dispatches via injected notification services.
- **`GET /Workloads`**: Generates real-time maps of Claim Officers and their associated pending claim pipelines enabling visual chart generation for management.

### 4.3. `ClaimController.cs` (`/api/claim`)
- Core business layer dealing with the most heavily orchestrated functionality.
- **`POST /RaiseClaim`**: Accepts a `RaiseClaimRequest`. Handles multipart boundaries mapping attached documents (PDFs, Images) routing them cleanly out to CDN upload wrappers. Validates geo-coordinates and incident date parameters ensuring claims occur within active policy windows.
- **`GET /MyClaims`**: Returns claims relative to the caller. Automatically filters records matching the embedded JWT `ClaimType.NameIdentifier`.
- **`PUT /ReviewClaim`**: (`[Authorize(Roles="ClaimOfficer,Admin")]`): Transitions state logic (e.g. `Pending` -> `Review` -> `Approved`). Fires real-time WebSocket notifications down to the frontend user if their claim resolves successfully.
- **`POST /EvaluateAI`**: Manually pokes the backend orchestration endpoint to re-analyze a specific claim context against the N8N LLM inference network.

### 4.4. `PolicyController.cs` (`/api/policy`)
- Manages the entire lifecycle of purchasable configuration products.
- **`GET /GetAll`**: Provides unauthenticated endpoints yielding the standardized platform catalogs including dynamically generated base premium tiers.
- **`POST /Apply`**: Maps a `PolicyApplication` connecting the user to a configuration. Handles nested entities parsing JSON arrays to map `FamilyMembers` and `NomineeDetails` to the root policy application avoiding orphan records.
- **`PUT /Config`**: Admin endpoint tweaking parameters like Max Coverage constraints or modifying base premium multiplier integers over time.

### 4.5. `ChatController.cs` & `NotificationsController.cs`
- Restful endpoints supplementing the SignalR websocket connections.
- **`GET /History/{userId}`**: Provides paginated scroll logic allowing the frontend chat panels to retrieve long histories of communication seamlessly without dumping multi-megabyte payloads in a single request.
- **`PUT /MarkRead`**: Changes notification flags inside SQL indicating a message payload was actively parsed by the end user adjusting Unread graphical UI badges incrementally.

### 4.6. `AgentController.cs` & `DashboardController.cs`
- Endpoints catering specifically to the Agent interface workflows mapping Clients inside CRM tables.
- **`GET /MyPortfolio`**: Loads a specific list of associated users mapping directly via Foreign Key linkages under the specific logged-in Agent ID.
- **`DashboardStats`**: Yields localized charts specific purely to the Agent's ongoing commissions and resolved policy acquisitions.

### 4.7. `PaymentController.cs` & `ReportController.cs`
- Payment abstractions routing requests towards third-party Stripe or Razorpay backend setups converting frontend intent tokens into valid database policy activation events.
- Reporting APIs abstract `jspdf` functionality formatting long-form CSVs or backend generated static reports over audited datasets securely exported exclusively for executive Admin review.

---

## 5. Deep Dive: Infrastructure Services & Implementations
The Application layer sets the boundaries (`IClaimService`) but the Infrastructure namespace (`c:\Sanjay\InsurancePlatform\Infrastructure\Services`) executes the concrete logic.

### 5.1. `ClaimService.cs` Implementation
This is the longest and most complex service in the ecosystem (over 16,000 bytes of logic).
- It injects `ApplicationDbContext`, `INotificationService`, `IFileStorageService`, and `HttpClient`.
- Handles complex validation logic confirming policy boundaries natively cross-reference date-times against database UTC flags.
- Orchestrates multi-step transaction loops: 
  1. Opens Entity transaction block.
  2. Creates physical DB row.
  3. Relies on `ImageKitFileStorageService` generating CDN Links mapping them immediately to `ClaimDocument` schemas avoiding broken references.
  4. Triggers `HttpClient` POST towards the N8N webhook URI containing the raw JSON schema payload.
  5. Awaits N8N AI response returning `AI_SummaryText` and `RiskScore`.
  6. Mutates the claim object adding AI generated parameters, committing `SaveChanges()`.
  7. If any step fails, triggers `.Rollback()` securing database integrity seamlessly.

### 5.2. `PolicyService.cs` Implementation
- Manages immense EF Core navigation loads mapping deep relational trees (`Policy -> ApplicationUser -> Relatives`).
- Ensures Coverage Balance calculations apply strict algorithms dictating premium prices evaluating customer variables seamlessly during the application instantiation logic block avoiding invalid or undercharged policies entering the live database schema setup.
- Interacts strictly with raw Entity states applying changes dynamically while exposing purely sanitized DTO objects to the outer API layers.

### 5.3. `AuthService.cs` Implementation
- Abstracts complicated ASP.NET Identity interactions dealing securely with cryptographic hashing configurations entirely bypassing plaintext password vectors.
- Injects JWT logic, constructs claims topologies, creates Role bindings validating Admins vs Users securely.
- Initiates Background Task logic hooking SMTP handlers securely piping new registration codes to outbound external user email inboxes upon Administrative trigger actions.

### 5.4. `ImageKitFileStorageService.cs` Implementation
- Third-party wrapper converting base IFormFile inputs out of the standard web boundaries pushing payloads sequentially up to the cloud CDN APIs.
- Returns pure strings back to the core logic resolving URL endpoints guaranteeing images and PDFs exist permanently independently from the platform host environment. 

### 5.5. `ChatService.cs` & `NotificationService.cs` Implementation
- Bridges logic spanning REST contexts and live SignalR Hub bindings. 
- The `NotificationService` accesses the `IHubContext<NotificationHub>` dynamically pushing arbitrary text strings ("Your claim was just reviewed!") towards specific user connections mapped locally on the Kestrel memory tree providing real-time seamless feedback bypassing delays.

---

## 6. Application Layer: DTOs & Orchestration
DTOs (Data Transfer Objects) serve to sanitize information. We never output raw DB Entities containing sensitive Identity hashed values.
- **`RaiseClaimRequest.cs`**: Abstracts the inputs of an incident holding date configurations and URL file arrays securely isolated.
- **`ClaimOfficerWorkloadDto.cs`**: Specific schema mapping purely the names of officers and quantitative numeric bounds parsing graphical representation needs accurately directly matching frontend `Chart.js` expected syntax array variables.
- **`UnifiedPaymentDto.cs`**: Isolates internal token architectures enabling smooth interactions against internal gateways securely processing API secrets server-side protecting platform integrity from frontend network visibility completely.

---

## 7. Domain Layer: Database Schema & Entities
These are mapping representations of SQL structures driving the whole `.NET` logic loop securely bounded without specific persistence ignorance logic present cleanly conforming Entity mapping variables.

- **`ApplicationUser`**: Extends standard `.NET Identity` models handling User data tracking portfolios.
- **`PolicyConfiguration`**: Defining static policy metadata describing constraints exactly mapped out.
- **`PolicyApplication`**: Cross-references linking policies to User portfolios capturing exactly dynamic balance tracking metrics securely deducting totals post-claim.
- **`InsuranceClaim`**: Core transaction model. Tracking properties spanning AI risk evaluations down to standard manual payout array arrays mapping.
- **`ChatMessage` & `Notification`**: Simple chronological entity setups mapping directly backwards capturing exact timestamps resolving chat windows incrementally securely bounding User relationships precisely natively.

---

## 8. Database Configuration & Entity Framework
The project accesses SQL Server via EF Core Code-First paradigms natively translating domain records against physical local table architectures seamlessly generating migration logic sequentially over time providing secure regression states precisely tracking application evolution natively exactly capturing updates via scripts like `migration_script.sql`.

- **`ApplicationDbContext.cs`**: Manages explicit bindings resolving cascading deletions preventing dead data references destroying system accuracy metrics enforcing foreign key ties stringently.
- **`DbInitializer.cs`**: Natively loads configuration seeds defining logic parameters cleanly mapping out policy setups automatically preventing zero-state environments breaking frontend UI boundaries natively reading standard internal `policy-config.json` configurations seamlessly applying logic precisely across builds dynamically natively.

---

## 9. Notification & Real-Time Hubs
SignalR resides within the endpoint config inside the `Infrastructure/Hubs` setup resolving long-running operations interactively mapping connection trees dynamically providing fluid conversational interfaces removing archaic HTTP pulling completely providing smooth UI integrations cleanly.

- **`NotificationHub.cs`**: Broadcasts alerts exclusively sending logic dynamically pushing real-time notification states rendering dynamic toast structures within the Angular frontend silently natively triggering component updates cleanly across the SPA framework efficiently handling scaling logic safely wrapping multi-node backend topologies natively maintaining communication flows optimally precisely matching connections efficiently scaling natively securely bounding access accurately natively.

---

## 10. Testing & Maintenance
Controllers and Services are written loosely coupled around constructor injected Interfaces (`IAuthService`). This fundamentally supports Mock implementations validating parameters seamlessly avoiding complex dependencies generating testing models mapping outputs securely enabling pure code validation cleanly without touching SQL Server configurations explicitly reducing pipeline lag mapping completely logically scaling integration workflows seamlessly mapping boundaries perfectly logically tracking behaviors explicitly clearly resolving edge cases optimally naturally.

---
*End of Comprehensive API & Infrastructure Backend Developer Guide*
*Authorized technical specification.*
