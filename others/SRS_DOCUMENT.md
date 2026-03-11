# End-to-End Software Requirements Specification (SRS)
## AcciSure Enterprise Insurance Platform (v1.0.0)

---

## Table of Contents
1. [Introduction](#1-introduction)
2. [Operating Framework & Ecosystem](#2-operating-framework--ecosystem)
3. [Business USPs and Paradigm Innovations](#3-business-usps-and-paradigm-innovations)
4. [Roles & Audience Descriptions](#4-roles--audience-descriptions)
5. [System Features & Functional Requirements (FRs)](#5-system-features--functional-requirements-frs)
6. [Data Schema & Architectural Dictionaries](#6-data-schema--architectural-dictionaries)
7. [External System Interfaces & Automations](#7-external-system-interfaces--automations)
8. [Non-Functional Requirements (NFRs)](#8-non-functional-requirements-nfrs)

---

## 1. Introduction
The AcciSure platform represents the digitization of a fully capable internal insurance firm alongside an interactive customer web portal. It unifies operations across policy creation, application purchasing, multi-channel customer support (text, video, chatbot voice), and intelligent claim auditing—powered natively by internal artificial intelligence integrations.

### Scope & Mission
To drastically lower the barrier to entry for insurance consumption via a smart interface, while exponentially decreasing the load on claim officers through orchestrated load-balancing and AI summarizations.

---

## 2. Operating Framework & Ecosystem
- **Frontend Stack**: Angular 21, Tailwind CSS UI Framework.
- **Backend Stack**: C# .NET API layers running over standard Kestrel Servers.
- **Relational Persistence**: Microsoft SQL Server / Formatted by EF Core DB Context mappings.
- **Real-time Pipeline**: WebRTC data pipelines and Microsoft SignalR Core Websockets.
- **Document Integrity**: ImageKit Content Delivery Networks (CDN) + Native document generation.

---

## 3. Business USPs and Paradigm Innovations
This platform operates not merely on simple CRUD operations, but advanced workflow innovations extending to voice, video, and algorithm integrations:

### 3.1. AI Systems Implementation
- **AI Policy Chatbot via N8N**: Customers are guided contextually. The AI dynamically sources policy datasets.
- **AI Claims Intelligence (N8N)**: Analyzes claim documentation returning contextual Risk Scores and payout validity.
- **AI Voice Call Assistant (Upcoming)**: Uses Browser Speech API native to the application.

### 3.2. Automated Communications
- **WhatsApp-Style Live Chat (SignalR)**: Zero polling interaction for customer inquiries.
- **Actionable Email Notification Automations**: Uses MailKit/SMTP to silently dispatch account credentials upon employee onboarding.

### 3.3. Officer Experience Enhancements
- **Live Staff Workload Assignments**: Algorithmic disbursement of incoming claims against officer utilization markers.
- **Coverage Balance Deduction**: Self-maintaining DB ledgers dynamically updating deductibles post-approval.

### 3.4. Interface Advancements
- **In-Platform Video Calling (WebRTC)**: Allows high-quality tele-health and consultation without client download requirements.
- **ImageKit Storage Networks**: For ultra-fast and cached document transfers globally.
- **Dual Verification (OTP+CAPTCHA)**: Rigorous endpoint defense against spam environments.

---

## 4. Roles & Audience Descriptions
The system heavily validates internal access boundaries:
1. **Admin**: Highest level of operation rights. Provisions personnel, configures `PolicyConfigurations`, reads massive system statistics.
2. **Customer**: General users consuming services, modifying their applications, tracking ledgers, chatting with AI.
3. **Agent**: Internal sales brokers capable of bulk assigning configurations and opening lines of manual peer communication.
4. **Claim Officer**: Workload-distributed adjudicators examining the outputs of the AI system for final human sign-off.

---

## 5. System Features & Functional Requirements (FRs)

### 5.1. Standard CRUD Operations
Operations required directly mimicking database structures.
- **FR_CRUD_01**: The system must allow Admins to purely Create, Read, Update, and Delete static insurance rule sets inside Database DB1.
- **FR_CRUD_02**: Customers must be able to view and manage secondary profiles (adding internal relations like `FamilyMember` records against policies).

### 5.2. Core Workflow Interactions
- **FR_WF_01**: Customers must access a unified payment route (Gateway abstractions) to secure an active policy footprint.
- **FR_WF_02**: The Claims phase allows Customers to attach up to 5MB digitized document strings mapping to URLs along with geographic locations strings and timestamps.

### 5.3. Communication Requirements
- **FR_COM_01**: The Frontend application must establish, authenticate, and recover gracefully failed WebSockets routing through the .NET backend Hubs.
- **FR_COM_02**: Video calling requests require localized capability to hook hardware media streams from desktop cameras routing directly against peer session SDP identifiers.

---

## 6. Data Schema & Architectural Dictionaries
The system runs via SQL Server mapped context bindings (`AcciSureDB1`). The primary relational tracks operate identically:
- `AspNetUsers`: Overridden base logic maintaining 2FA columns, normalized Emails, and security stamps.
- `PolicyConfigurations`: Stores base multipliers.
- `PolicyApplications`: The associative map dictating customer ownership of rules.
- `InsuranceClaims`: Main auditing table holding JSON states for `AI_RiskScore`, references to User navigation IDs, dates, payload logs, and current State logic variables.

---

## 7. External System Interfaces & Automations
- **WebHooks**: Internal routines broadcast HTTP operations to N8N Orchestration interfaces for Chatbot query mapping.
- **CDN**: REST-like signatures generated securely passing files to ImageKit API bounds.
- **Telemetry**: Exporting metric sets logic mapping visual aggregations back to Frontend `chart.js` rendering contexts.

---

## 8. Non-Functional Requirements (NFRs)
Specific technical guidelines mandatory across endpoints:
1. **Performance**: Admin dashboard aggregates executed on 50,000+ policies must be resolved through indexed keys avoiding table scans to generate response buffers inside 200ms constraints.
2. **Availability**: Frontend stateless logic deployed via Vercel maintaining an up time exceeding 99.9%. Backend layers containerized within fault-tolerant networks.
3. **Security (Dual-Gate)**: Passwords hashes are irreversible. Endpoints must be signed via JWT formats parsing Claims identity. OTP logic prevents unauthorized generation of system roles protecting spam. CAPTCHAs prevent brute automation of public endpoints. WebRTC streams adhere to strict DTLS encryption boundaries traversing the platform organically.

---
*Authorized specification. Appended via AI automation.*
