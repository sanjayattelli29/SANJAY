# Insurance Platform - Complete API Documentation




## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Controllers Summary](#controllers-summary)
4. [API Endpoints by Controller](#api-endpoints-by-controller)
   - [AuthController](#1-authcontroller)
   - [AdminController](#2-admincontroller)
   - [AgentController](#3-agentcontroller)
   - [PolicyController](#4-policycontroller)
   - [ClaimController](#5-claimcontroller)
   - [ChatController](#6-chatcontroller)
   - [NotificationsController](#7-notificationscontroller)
   - [PaymentController](#8-paymentcontroller)
   - [ReportController](#9-reportcontroller)
   - [DashboardController](#10-dashboardcontroller)
5. [Common Data Models](#common-data-models)
6. [Error Handling](#error-handling)
7. [Status Codes](#status-codes)

---

## Overview

The Insurance Platform API provides a comprehensive RESTful interface for managing insurance policies, claims, payments, and user interactions. The API supports four user roles:

- **Customer**: Apply for policies, file claims, make payments
- **Agent**: Review policy applications, manage customer relationships
- **Claim Officer**: Review and approve/reject insurance claims
- **Admin**: System administration, user management, assignments

**Total Controllers:** 10  
**Total Endpoints:** 48+

---

## Authentication

### Authentication Method
- **Type:** JWT Bearer Token
- **Header:** `Authorization: Bearer <token>`

### Obtaining a Token
Use the `/api/auth/login` endpoint to obtain a JWT token.

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "status": "Success",
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com",
  "roles": ["Customer"],
  "userId": "abc123-def456-ghi789"
}
```

### Authorization Roles
- `Customer` - End users who buy insurance
- `Agent` - Insurance agents who process applications
- `ClaimOfficer` - Officers who process claims
- `Admin` - System administrators

---

## Controllers Summary

| # | Controller | Base Route | Auth Required | Endpoints | Purpose |
|---|------------|------------|---------------|-----------|---------|
| 1 | AuthController | `/api/auth` | No (on login/register) | 2 | User authentication and registration |
| 2 | AdminController | `/api/admin` | Yes (Admin only) | 11 | System administration and management |
| 3 | AgentController | `/api/agent` | Yes (Agent only) | 5 | Agent operations and analytics |
| 4 | PolicyController | `/api/policy` | Yes (except config) | 4 | Policy management and applications |
| 5 | ClaimController | `/api/claim` | Yes (role-based) | 8 | Insurance claim operations |
| 6 | ChatController | `/api/chat` | Yes | 4 | Real-time chat between customers & agents |
| 7 | NotificationsController | `/api/notifications` | Yes | 4 | User notification management |
| 8 | PaymentController | `/api/payment` | Yes | 1 | Process policy payments |
| 9 | ReportController | `/api/report` | Yes (Admin/Agent/Officer) | 1 | Financial and payment reports |
| 10 | DashboardController | `/api/dashboard` | Yes (role-based) | 4 | Dashboard access verification |

**Total Unique Endpoints:** 44

---

## API Endpoints by Controller

---

## 1. AuthController

**Base Route:** `/api/auth`  
**Authorization:** Not required (for login/register)

### Purpose
Handles user authentication, registration, and token generation.

---

### 1.1 Register Customer

**Endpoint:** `POST /api/auth/register`  
**Authorization:** None  
**Purpose:** Register a new customer account

#### Request Body
```json
{
  "fullName": "Priya Sharma",
  "email": "priya.sharma@gmail.com",
  "password": "SecurePass123!",
  "phoneNumber": "+919876543210",
  "aadhaarNumber": "123456789012"
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| fullName | string | Yes | Customer's full name |
| email | string | Yes | Valid email address |
| password | string | Yes | Strong password (min 8 chars) |
| phoneNumber | string | Yes | Phone number with country code |
| aadhaarNumber | string | No | 12-digit Aadhaar number |

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Registration successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "priya.sharma@gmail.com",
  "roles": ["Customer"],
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### Error Response (400 Bad Request)
```json
{
  "status": "Error",
  "message": "Email already exists"
}
```

---

### 1.2 Login

**Endpoint:** `POST /api/auth/login`  
**Authorization:** None  
**Purpose:** Authenticate user and receive JWT token

#### Request Body
```json
{
  "email": "priya.sharma@gmail.com",
  "password": "SecurePass123!"
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | User's email address |
| password | string | Yes | User's password |

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "priya.sharma@gmail.com",
  "roles": ["Customer"],
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### Error Response (401 Unauthorized)
```json
{
  "status": "Error",
  "message": "Invalid credentials"
}
```

---

## 2. AdminController

**Base Route:** `/api/admin`  
**Authorization:** Required (Admin role only)

### Purpose
Provides administrative functions for managing users, agents, claim officers, and system-wide operations.

---

### 2.1 Create Agent

**Endpoint:** `POST /api/admin/create-agent`  
**Authorization:** Admin  
**Purpose:** Create a new insurance agent account

#### Request Body
```json
{
  "fullName": "Rajesh Kumar",
  "email": "rajesh.kumar@insuranceplatform.com",
  "phoneNumber": "+919876543211",
  "bankAccountNumber": "1234567890123456",
  "password": "Agent@123"
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| fullName | string | Yes | Agent's full name |
| email | string | Yes | Agent's email address |
| phoneNumber | string | Yes | Contact number |
| bankAccountNumber | string | Yes | 16-digit bank account for commissions |
| password | string | Yes | Initial password for agent |

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Agent created successfully",
  "userId": "agent-550e8400-e29b-41d4",
  "email": "rajesh.kumar@insuranceplatform.com"
}
```

---

### 2.2 Create Claim Officer

**Endpoint:** `POST /api/admin/create-claim-officer`  
**Authorization:** Admin  
**Purpose:** Create a new claim officer account

#### Request Body
```json
{
  "fullName": "Amit Patel",
  "email": "amit.patel@insuranceplatform.com",
  "phoneNumber": "+919876543213",
  "bankAccountNumber": "9876543210123456",
  "password": "Officer@123"
}
```

#### Request Parameters
Same as Create Agent

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Claim officer created successfully",
  "userId": "officer-550e8400-e29b-41d4",
  "email": "amit.patel@insuranceplatform.com"
}
```

---

### 2.3 Get All Agents

**Endpoint:** `GET /api/admin/agents`  
**Authorization:** Admin  
**Purpose:** Retrieve list of all agents in the system

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "agent-550e8400-e29b-41d4",
    "fullName": "Rajesh Kumar",
    "email": "rajesh.kumar@insuranceplatform.com",
    "phoneNumber": "+919876543211",
    "createdAt": "2026-03-01T10:30:00Z"
  },
  {
    "id": "agent-660e8400-e29b-41d4",
    "fullName": "Sneha Desai",
    "email": "sneha.desai@insuranceplatform.com",
    "phoneNumber": "+919876543220",
    "createdAt": "2026-03-02T14:20:00Z"
  }
]
```

---

### 2.4 Get All Claim Officers

**Endpoint:** `GET /api/admin/claim-officers`  
**Authorization:** Admin  
**Purpose:** Retrieve list of all claim officers

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "officer-770e8400-e29b-41d4",
    "fullName": "Amit Patel",
    "email": "amit.patel@insuranceplatform.com",
    "phoneNumber": "+919876543213",
    "createdAt": "2026-03-01T09:00:00Z"
  }
]
```

---

### 2.5 Delete User

**Endpoint:** `DELETE /api/admin/delete-user/{id}`  
**Authorization:** Admin  
**Purpose:** Remove a user from the system

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| id | string | User ID to delete |

#### Example Request
```http
DELETE /api/admin/delete-user/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer <admin_token>
```

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "User deleted successfully"
}
```

---

### 2.6 Get All Policy Requests

**Endpoint:** `GET /api/admin/policy-requests`  
**Authorization:** Admin  
**Purpose:** View all policy applications in the system

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "policy-abc123",
    "userId": "customer-xyz789",
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "status": "Pending",
    "calculatedPremium": 15000.00,
    "totalCoverageAmount": 500000.00,
    "submissionDate": "2026-03-03T08:30:00Z",
    "assignedAgentId": null,
    "user": {
      "email": "priya.sharma@gmail.com",
      "fullName": "Priya Sharma"
    }
  }
]
```

---

### 2.7 Get Agents with Workload

**Endpoint:** `GET /api/admin/agents-with-load`  
**Authorization:** Admin  
**Purpose:** View agents and their current workload for assignment decisions

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "agentId": "agent-550e8400-e29b-41d4",
    "agentEmail": "rajesh.kumar@insuranceplatform.com",
    "assignedPolicyCount": 5
  },
  {
    "agentId": "agent-660e8400-e29b-41d4",
    "agentEmail": "sneha.desai@insuranceplatform.com",
    "assignedPolicyCount": 3
  }
]
```

---

### 2.8 Assign Agent to Policy

**Endpoint:** `POST /api/admin/assign-agent`  
**Authorization:** Admin  
**Purpose:** Assign a pending policy application to an agent

#### Request Body
```json
{
  "applicationId": "policy-abc123",
  "agentId": "agent-550e8400-e29b-41d4"
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| applicationId | string | Yes | Policy application ID |
| agentId | string | Yes | Agent ID to assign |

#### Success Response (200 OK)
```json
{
  "message": "Agent assigned successfully"
}
```

#### Error Response (400 Bad Request)
```json
{
  "message": "Assignment failed"
}
```

---

### 2.9 Get All Users

**Endpoint:** `GET /api/admin/all-users`  
**Authorization:** Admin  
**Purpose:** Retrieve complete list of all users in the system

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "user-001",
    "fullName": "Priya Sharma",
    "email": "priya.sharma@gmail.com",
    "roles": ["Customer"],
    "createdAt": "2026-02-28T10:00:00Z"
  },
  {
    "id": "user-002",
    "fullName": "Rajesh Kumar",
    "email": "rajesh.kumar@insuranceplatform.com",
    "roles": ["Agent"],
    "createdAt": "2026-03-01T10:30:00Z"
  }
]
```

---

### 2.10 Get All Claims (Master View)

**Endpoint:** `GET /api/admin/all-claims`  
**Authorization:** Admin  
**Purpose:** View all insurance claims in the system

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "claim-xyz789",
    "policyApplicationId": "policy-abc123",
    "userId": "customer-xyz789",
    "status": "Pending",
    "requestedAmount": 50000.00,
    "approvedAmount": 0.00,
    "incidentType": "Sickness",
    "incidentDate": "2026-02-25T00:00:00Z",
    "submissionDate": "2026-03-03T09:15:00Z",
    "assignedClaimOfficerId": null
  }
]
```

---

### 2.11 Get Admin Statistics

**Endpoint:** `GET /api/admin/admin-stats`  
**Authorization:** Admin  
**Purpose:** Get dashboard statistics for admin overview

#### Request Parameters
None

#### Success Response (200 OK)
```json
{
  "totalCustomers": 150,
  "totalAgents": 12,
  "totalClaimOfficers": 5,
  "totalPolicies": 200,
  "activePolicies": 180,
  "pendingPolicies": 15,
  "totalClaims": 45,
  "pendingClaims": 8,
  "approvedClaims": 32,
  "rejectedClaims": 5,
  "totalClaimAmount": 2500000.00,
  "approvedClaimAmount": 2000000.00
}
```

---

## 3. AgentController

**Base Route:** `/api/agent`  
**Authorization:** Required (Agent role only)

### Purpose
Manages agent-specific operations including policy reviews, customer management, and performance analytics.

---

### 3.1 Get My Assigned Requests

**Endpoint:** `GET /api/agent/my-requests`  
**Authorization:** Agent  
**Purpose:** View policy applications assigned to the logged-in agent

#### Request Parameters
None (Agent ID extracted from JWT token)

#### Success Response (200 OK)
```json
[
  {
    "id": "policy-abc123",
    "userId": "customer-xyz789",
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "status": "Assigned",
    "calculatedPremium": 15000.00,
    "totalCoverageAmount": 500000.00,
    "submissionDate": "2026-03-03T08:30:00Z",
    "user": {
      "fullName": "Priya Sharma",
      "email": "priya.sharma@gmail.com",
      "phoneNumber": "+919876543210"
    }
  }
]
```

---

### 3.2 Review Policy Application

**Endpoint:** `POST /api/agent/review-request`  
**Authorization:** Agent  
**Purpose:** Approve or reject a policy application

#### Request Body
```json
{
  "applicationId": "policy-abc123",
  "status": "Approved"
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| applicationId | string | Yes | Policy application ID |
| status | string | Yes | "Approved" or "Rejected" |

#### Success Response (200 OK)
```json
{
  "message": "Application Approved successfully"
}
```

#### Error Response (400 Bad Request)
```json
{
  "message": "Review failed"
}
```

---

### 3.3 Get My Customers

**Endpoint:** `GET /api/agent/my-customers`  
**Authorization:** Agent  
**Purpose:** View list of customers who bought policies through this agent

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "policy-abc123",
    "userId": "customer-xyz789",
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "status": "Active",
    "startDate": "2026-03-03T00:00:00Z",
    "expiryDate": "2027-03-03T00:00:00Z",
    "user": {
      "fullName": "Priya Sharma",
      "email": "priya.sharma@gmail.com",
      "phoneNumber": "+919876543210"
    }
  }
]
```

---

### 3.4 Get Commission Statistics

**Endpoint:** `GET /api/agent/commission-stats`  
**Authorization:** Agent  
**Purpose:** View total commission earned and active policies

#### Request Parameters
None

#### Success Response (200 OK)
```json
{
  "totalCommission": 75000.00,
  "activePolicies": [
    {
      "id": "policy-abc123",
      "policyCategory": "Health Insurance - Individual",
      "calculatedPremium": 15000.00,
      "commission": 1500.00
    }
  ]
}
```

---

### 3.5 Get Analytics Dashboard

**Endpoint:** `GET /api/agent/analytics`  
**Authorization:** Agent  
**Purpose:** Get comprehensive performance analytics and charts data

#### Request Parameters
None

#### Success Response (200 OK)
```json
{
  "totalCoverageProvided": 5000000.00,
  "activePolicyCount": 15,
  "uniqueCustomerCount": 12,
  "bestPerformingCategory": "Health Insurance - Individual",
  "bestPerformingTier": "GOLD",
  "totalPremiumCollected": 225000.00,
  "totalCommissionEarned": 22500.00,
  "commissionPerformance": [
    { "month": "January", "value": 5000.00 },
    { "month": "February", "value": 7500.00 },
    { "month": "March", "value": 10000.00 }
  ],
  "portfolioMix": [
    { "category": "Health Insurance - Individual", "count": 8 },
    { "category": "Health Insurance - Family", "count": 7 }
  ],
  "tierBreakdown": [
    { "tier": "GOLD", "count": 10 },
    { "tier": "SILVER", "count": 5 }
  ]
}
```

---

## 4. PolicyController

**Base Route:** `/api/policy`  
**Authorization:** Required (except for configuration endpoint)

### Purpose
Manages insurance policy operations including configuration, premium calculation, applications, and policy viewing.

---

### 4.1 Get Policy Configuration

**Endpoint:** `GET /api/policy/configuration`  
**Authorization:** None (Public endpoint)  
**Purpose:** Retrieve available policy categories, tiers, and pricing rules

#### Request Parameters
None

#### Success Response (200 OK)
```json
{
  "policyCategories": [
    {
      "categoryId": "individual",
      "categoryName": "Health Insurance - Individual",
      "maxMembersAllowed": 1,
      "tiers": [
        {
          "tierId": "GOLD",
          "tierName": "Gold Plan",
          "baseCoverageAmount": 500000.00,
          "basePremiumAmount": 15000.00,
          "validityInYears": 1,
          "benefits": [
            "Cashless hospitalization",
            "Pre and post hospitalization",
            "Daycare procedures"
          ]
        },
        {
          "tierId": "SILVER",
          "tierName": "Silver Plan",
          "baseCoverageAmount": 300000.00,
          "basePremiumAmount": 10000.00,
          "validityInYears": 1,
          "benefits": [
            "Cashless hospitalization",
            "Pre and post hospitalization"
          ]
        }
      ]
    },
    {
      "categoryId": "family",
      "categoryName": "Health Insurance - Family",
      "maxMembersAllowed": 6,
      "tiers": [
        {
          "tierId": "GOLD",
          "tierName": "Gold Family Plan",
          "baseCoverageAmount": 1000000.00,
          "basePremiumAmount": 30000.00,
          "validityInYears": 1,
          "benefits": [
            "Family floater coverage",
            "Cashless hospitalization",
            "Maternity coverage"
          ]
        }
      ]
    }
  ],
  "riskFactors": {
    "ageMultipliers": [
      { "minAge": 18, "maxAge": 30, "multiplier": 1.0 },
      { "minAge": 31, "maxAge": 45, "multiplier": 1.2 },
      { "minAge": 46, "maxAge": 60, "multiplier": 1.5 }
    ],
    "professionMultipliers": [
      { "profession": "IT Professional", "multiplier": 1.0 },
      { "profession": "Construction Worker", "multiplier": 1.3 }
    ],
    "smokingMultiplier": {
      "nonSmoker": 1.0,
      "occasional": 1.1,
      "regular": 1.3
    }
  }
}
```

---

### 4.2 Calculate Premium

**Endpoint:** `POST /api/policy/calculate-premium`  
**Authorization:** Required  
**Purpose:** Calculate insurance premium based on applicant details

#### Request Body (Individual)
```json
{
  "policyCategory": "Health Insurance - Individual",
  "tierId": "GOLD",
  "applicant": {
    "fullName": "Priya Sharma",
    "age": 32,
    "profession": "IT Professional",
    "alcoholHabit": "NonDrinker",
    "smokingHabit": "NonSmoker",
    "travelKmPerMonth": 500
  },
  "paymentMode": "Annual",
  "nominee": {
    "nomineeName": "Rahul Sharma",
    "nomineeEmail": "rahul.sharma@gmail.com",
    "nomineePhone": "+919876543299",
    "nomineeBankAccountNumber": "9988776655443322"
  }
}
```

#### Request Body (Family)
```json
{
  "policyCategory": "Health Insurance - Family",
  "tierId": "GOLD",
  "primaryApplicant": {
    "fullName": "Rajesh Kumar",
    "age": 40,
    "profession": "Business Owner",
    "alcoholHabit": "Occasional",
    "smokingHabit": "NonSmoker",
    "travelKmPerMonth": 1000
  },
  "familyMembers": [
    { "fullName": "Priya Kumar", "relation": "Spouse" },
    { "fullName": "Aarav Kumar", "relation": "Son" }
  ],
  "paymentMode": "Annual",
  "nominee": {
    "nomineeName": "Priya Kumar",
    "nomineeEmail": "priya.k@gmail.com",
    "nomineePhone": "+919876543288",
    "nomineeBankAccountNumber": "1122334455667788"
  }
}
```

#### Success Response (200 OK)
```json
{
  "premium": 18000.00
}
```

---

### 4.3 Apply for Policy

**Endpoint:** `POST /api/policy/apply`  
**Authorization:** Customer  
**Purpose:** Submit a new policy application

#### Request Body
Same as Calculate Premium (see 4.2)

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Policy application submitted successfully",
  "applicationId": "policy-abc123",
  "calculatedPremium": 18000.00,
  "applicationStatus": "Pending"
}
```

---

### 4.4 Get My Policies

**Endpoint:** `GET /api/policy/my-policies`  
**Authorization:** Customer  
**Purpose:** View all policies owned by the logged-in customer

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "policy-abc123",
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "status": "Active",
    "calculatedPremium": 18000.00,
    "totalCoverageAmount": 500000.00,
    "remainingCoverageAmount": 500000.00,
    "startDate": "2026-03-03T00:00:00Z",
    "expiryDate": "2027-03-03T00:00:00Z",
    "paymentMode": "Annual",
    "nextPaymentDate": "2027-03-03T00:00:00Z",
    "submissionDate": "2026-03-03T08:30:00Z"
  }
]
```

---

## 5. ClaimController

**Base Route:** `/api/claim`  
**Authorization:** Required (role-based access)

### Purpose
Handles insurance claim operations for customers, agents, claim officers, and admin.

---

### 5.1 Raise Claim (Customer)

**Endpoint:** `POST /api/claim/raise`  
**Authorization:** Customer  
**Purpose:** File a new insurance claim

#### Request Type
`multipart/form-data` (supports file upload)

#### Form Fields
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| policyApplicationId | string | Yes | Policy ID for which claim is filed |
| incidentType | string | Yes | Type: "Sickness", "Accident", etc. |
| incidentLocation | string | Yes | Location where incident occurred |
| incidentDate | datetime | Yes | Date of incident |
| description | string | Yes | Detailed description |
| hospitalName | string | Yes | Hospital name |
| hospitalizationRequired | boolean | Yes | Whether hospitalized |
| requestedAmount | decimal | Yes | Claim amount requested |
| affectedMemberName | string | No | For family policies |
| affectedMemberRelation | string | No | Relation to primary |
| documents | file[] | No | Medical bills, reports (PDF/JPG) |

#### Example Request
```http
POST /api/claim/raise
Authorization: Bearer <customer_token>
Content-Type: multipart/form-data

policyApplicationId=policy-abc123
incidentType=Sickness
incidentLocation=Mumbai, Maharashtra
incidentDate=2026-02-25
description=Hospitalization due to dengue fever
hospitalName=Apollo Hospital
hospitalizationRequired=true
requestedAmount=50000.00
documents=medical_bill.pdf
documents=discharge_summary.pdf
```

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "message": "Claim raised successfully",
  "claimId": "claim-xyz789"
}
```

---

### 5.2 Get My Claims (Customer)

**Endpoint:** `GET /api/claim/my-claims`  
**Authorization:** Customer  
**Purpose:** View all claims filed by the customer

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "claim-xyz789",
    "policyApplicationId": "policy-abc123",
    "status": "Pending",
    "requestedAmount": 50000.00,
    "approvedAmount": 0.00,
    "incidentType": "Sickness",
    "incidentLocation": "Mumbai, Maharashtra",
    "incidentDate": "2026-02-25T00:00:00Z",
    "submissionDate": "2026-03-03T09:15:00Z",
    "hospitalName": "Apollo Hospital",
    "hospitalizationRequired": true,
    "documents": [
      {
        "id": "doc-001",
        "fileName": "medical_bill.pdf",
        "fileUrl": "https://ik.imagekit.io/demo/medical_bill.pdf",
        "fileSize": 524288,
        "uploadedAt": "2026-03-03T09:15:00Z"
      }
    ]
  }
]
```

---

### 5.3 Get Pending Claims (Admin)

**Endpoint:** `GET /api/claim/admin/pending`  
**Authorization:** Admin  
**Purpose:** View all unassigned pending claims

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "claim-xyz789",
    "policyApplicationId": "policy-abc123",
    "userId": "customer-xyz789",
    "requestedAmount": 50000.00,
    "incidentType": "Sickness",
    "submissionDate": "2026-03-03T09:15:00Z",
    "user": {
      "fullName": "Priya Sharma",
      "email": "priya.sharma@gmail.com"
    }
  }
]
```

---

### 5.4 Get Claim Officers with Workload (Admin)

**Endpoint:** `GET /api/claim/admin/officers`  
**Authorization:** Admin  
**Purpose:** View claim officers and their current workload

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "claimOfficerId": "officer-770e8400",
    "email": "amit.patel@insuranceplatform.com",
    "assignedClaimsCount": 3
  },
  {
    "claimOfficerId": "officer-880e8400",
    "email": "sneha.singh@insuranceplatform.com",
    "assignedClaimsCount": 5
  }
]
```

---

### 5.5 Assign Claim Officer (Admin)

**Endpoint:** `POST /api/claim/admin/assign`  
**Authorization:** Admin  
**Purpose:** Assign a pending claim to a claim officer

#### Request Body
```json
{
  "claimId": "claim-xyz789",
  "officerId": "officer-770e8400"
}
```

#### Success Response (200 OK)
```json
{
  "message": "Officer assigned successfully."
}
```

---

### 5.6 Get My Assigned Claims (Claim Officer)

**Endpoint:** `GET /api/claim/officer/my-requests`  
**Authorization:** ClaimOfficer  
**Purpose:** View claims assigned to the logged-in officer

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "claim-xyz789",
    "policyApplicationId": "policy-abc123",
    "userId": "customer-xyz789",
    "requestedAmount": 50000.00,
    "approvedAmount": 0.00,
    "status": "Assigned",
    "incidentType": "Sickness",
    "description": "Hospitalization due to dengue fever",
    "hospitalName": "Apollo Hospital",
    "incidentDate": "2026-02-25T00:00:00Z",
    "submissionDate": "2026-03-03T09:15:00Z",
    "user": {
      "fullName": "Priya Sharma",
      "email": "priya.sharma@gmail.com"
    },
    "documents": [...]
  }
]
```

---

### 5.7 Review Claim (Claim Officer)

**Endpoint:** `POST /api/claim/officer/review`  
**Authorization:** ClaimOfficer  
**Purpose:** Approve or reject a claim

#### Request Body
```json
{
  "claimId": "claim-xyz789",
  "status": "Approved",
  "remarks": "Claim approved after reviewing all documents",
  "approvedAmount": 45000.00
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| claimId | string | Yes | Claim ID to review |
| status | string | Yes | "Approved" or "Rejected" |
| remarks | string | Yes | Officer's comments |
| approvedAmount | decimal | Yes | Amount approved (can be less than requested) |

#### Success Response (200 OK)
```json
{
  "message": "Claim Approved successfully."
}
```

---

### 5.8 Get Agent's Customer Claims (Agent)

**Endpoint:** `GET /api/claim/agent/customer-claims`  
**Authorization:** Agent  
**Purpose:** View claims from customers whose policies the agent manages

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "id": "claim-xyz789",
    "policyApplicationId": "policy-abc123",
    "status": "Approved",
    "requestedAmount": 50000.00,
    "approvedAmount": 45000.00,
    "incidentType": "Sickness",
    "user": {
      "fullName": "Priya Sharma",
      "email": "priya.sharma@gmail.com"
    }
  }
]
```

---

## 6. ChatController

**Base Route:** `/api/chat`  
**Authorization:** Required

### Purpose
Manages real-time chat functionality between customers and agents.

---

### 6.1 Get Chat List

**Endpoint:** `GET /api/chat/list`  
**Authorization:** Required  
**Purpose:** Get all active chat sessions for the logged-in user

#### Request Parameters
None (User ID and role extracted from JWT)

#### Success Response (200 OK) - Customer View
```json
[
  {
    "id": "chat-001",
    "policyId": "policy-abc123",
    "policyName": "Health Insurance - Individual",
    "category": "Health",
    "agentEmail": "rajesh.kumar@insuranceplatform.com",
    "lastMessage": "Your policy has been approved!",
    "lastMessageTime": "2026-03-03T14:30:00Z",
    "unreadCount": 2
  }
]
```

#### Success Response (200 OK) - Agent View
```json
[
  {
    "id": "chat-001",
    "policyId": "policy-abc123",
    "policyName": "Health Insurance - Individual",
    "category": "Health",
    "customerEmail": "priya.sharma@gmail.com",
    "lastMessage": "Thank you for your help!",
    "lastMessageTime": "2026-03-03T14:35:00Z",
    "unreadCount": 1
  }
]
```

---

### 6.2 Get Chat History

**Endpoint:** `GET /api/chat/{policyId}`  
**Authorization:** Required  
**Purpose:** Retrieve message history for a specific policy

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| policyId | string | Policy ID for the chat |

#### Example Request
```http
GET /api/chat/policy-abc123
Authorization: Bearer <token>
```

#### Success Response (200 OK)
```json
{
  "policyId": "policy-abc123",
  "customerEmail": "priya.sharma@gmail.com",
  "agentEmail": "rajesh.kumar@insuranceplatform.com",
  "messages": [
    {
      "senderRole": "Customer",
      "message": "Hello, I have a query about my policy coverage",
      "timestamp": "2026-03-03T14:00:00Z"
    },
    {
      "senderRole": "Agent",
      "message": "Hello! I'd be happy to help. What would you like to know?",
      "timestamp": "2026-03-03T14:02:00Z"
    },
    {
      "senderRole": "Customer",
      "message": "Does my policy cover pre-existing conditions?",
      "timestamp": "2026-03-03T14:05:00Z"
    },
    {
      "senderRole": "Agent",
      "message": "Your policy covers pre-existing conditions after a waiting period of 2 years.",
      "timestamp": "2026-03-03T14:07:00Z"
    }
  ]
}
```

---

### 6.3 Initialize Chat

**Endpoint:** `POST /api/chat/init`  
**Authorization:** Required  
**Purpose:** Create or retrieve existing chat session

#### Request Body
```json
{
  "policyId": "policy-abc123",
  "customerId": "customer-xyz789",
  "agentId": "agent-550e8400"
}
```

#### Success Response (200 OK)
```json
{
  "id": "chat-001",
  "policyId": "policy-abc123",
  "customerId": "customer-xyz789",
  "agentId": "agent-550e8400",
  "createdAt": "2026-03-03T14:00:00Z"
}
```

---

### 6.4 Mark Messages as Read

**Endpoint:** `POST /api/chat/{policyId}/read`  
**Authorization:** Required  
**Purpose:** Mark all messages in a chat as read

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| policyId | string | Policy ID for the chat |

#### Example Request
```http
POST /api/chat/policy-abc123/read
Authorization: Bearer <token>
```

#### Success Response (200 OK)
```json
{}
```

---

## 7. NotificationsController

**Base Route:** `/api/notifications`  
**Authorization:** Required

### Purpose
Manages user notifications including real-time alerts and system notifications.

---

### 7.1 Get Notifications

**Endpoint:** `GET /api/notifications`  
**Authorization:** Required  
**Purpose:** Retrieve all notifications for the logged-in user

#### Query Parameters
| Parameter | Type | Optional | Description |
|-----------|------|----------|-------------|
| role | string | Yes | Filter by role: "Customer", "Agent", "Admin", "ClaimOfficer" |

#### Example Request
```http
GET /api/notifications?role=Customer
Authorization: Bearer <customer_token>
```

#### Success Response (200 OK)
```json
[
  {
    "id": "notif-001",
    "userId": "customer-xyz789",
    "title": "Policy Approved",
    "message": "Your health insurance policy has been approved!",
    "notificationType": "CUST:Policy",
    "createdAt": "2026-03-03T10:30:00Z",
    "isRead": false
  },
  {
    "id": "notif-002",
    "userId": "customer-xyz789",
    "title": "Claim Update",
    "message": "Your claim #CLM001 has been approved for ₹45,000",
    "notificationType": "CUST:Claim",
    "createdAt": "2026-03-03T11:15:00Z",
    "isRead": true
  },
  {
    "id": "notif-003",
    "userId": "customer-xyz789",
    "title": "Payment Required",
    "message": "Please complete your payment of ₹18,000.00 for GOLD. ID: POL...",
    "notificationType": "CUST:Policy:policy-abc123",
    "createdAt": "2026-03-03T08:30:00Z",
    "isRead": false
  }
]
```

**Note:** The API returns both stored notifications and dynamically generated notifications based on pending actions.

---

### 7.2 Get Unread Count

**Endpoint:** `GET /api/notifications/unread-count`  
**Authorization:** Required  
**Purpose:** Get count of unread notifications

#### Request Parameters
None

#### Success Response (200 OK)
```json
5
```

---

### 7.3 Mark Notification as Read

**Endpoint:** `POST /api/notifications/{id}/read`  
**Authorization:** Required  
**Purpose:** Mark a specific notification as read

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Notification ID |

#### Example Request
```http
POST /api/notifications/550e8400-e29b-41d4-a716-446655440000/read
Authorization: Bearer <token>
```

#### Success Response (200 OK)
```json
{
  "message": "Notification marked as read"
}
```

---

### 7.4 Mark All as Read

**Endpoint:** `POST /api/notifications/read-all`  
**Authorization:** Required  
**Purpose:** Mark all notifications as read for the user

#### Request Parameters
None

#### Success Response (200 OK)
```json
{
  "message": "All notifications marked as read"
}
```

---

## 8. PaymentController

**Base Route:** `/api/payment`  
**Authorization:** Required

### Purpose
Handles policy payment processing and transaction management.

---

### 8.1 Process Payment

**Endpoint:** `POST /api/payment/process`  
**Authorization:** Required (Customer)  
**Purpose:** Process payment for an approved policy application

#### Request Body
```json
{
  "applicationId": "policy-abc123",
  "amount": 18000.00
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| applicationId | string | Yes | Policy application ID |
| amount | decimal | Yes | Payment amount (must match premium) |

#### Success Response (200 OK)
```json
{
  "status": "Success",
  "transactionId": "TXN-ABC12345",
  "message": "Payment successful and policy activated."
}
```

#### Error Response (400 Bad Request)
```json
{
  "message": "Payment processing failed. Ensure policy is in AwaitingPayment status."
}
```

**Notes:**
- Policy must be in `AwaitingPayment` status (approved by agent)
- Amount must be greater than zero
- Transaction ID is auto-generated
- Policy status changes to `Active` after successful payment

---

## 9. ReportController

**Base Route:** `/api/report`  
**Authorization:** Required (Admin, Agent, ClaimOfficer)

### Purpose
Provides financial reports and payment tracking across the platform.

---

### 9.1 Get Unified Payments Report

**Endpoint:** `GET /api/report/unified-payments`  
**Authorization:** Admin, Agent, ClaimOfficer  
**Purpose:** View comprehensive payment report for all policies

#### Request Parameters
None

#### Success Response (200 OK)
```json
[
  {
    "policyId": "policy-abc123",
    "customerName": "Priya Sharma",
    "customerEmail": "priya.sharma@gmail.com",
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "premium": 18000.00,
    "paidAmount": 18000.00,
    "paymentDate": "2026-03-03T12:00:00Z",
    "transactionId": "TXN-ABC12345",
    "paymentMode": "Annual",
    "nextPaymentDate": "2027-03-03T00:00:00Z",
    "policyStatus": "Active"
  },
  {
    "policyId": "policy-def456",
    "customerName": "Rahul Verma",
    "customerEmail": "rahul.verma@gmail.com",
    "policyCategory": "Health Insurance - Family",
    "tierId": "SILVER",
    "premium": 25000.00,
    "paidAmount": 25000.00,
    "paymentDate": "2026-03-02T10:30:00Z",
    "transactionId": "TXN-DEF67890",
    "paymentMode": "Annual",
    "nextPaymentDate": "2027-03-02T00:00:00Z",
    "policyStatus": "Active"
  }
]
```

---

## 10. DashboardController

**Base Route:** `/api/dashboard`  
**Authorization:** Required (role-based)

### Purpose
Simple endpoints to verify user authentication and role-based access for dashboard views.

---

### 10.1 Customer Dashboard

**Endpoint:** `GET /api/dashboard/customer`  
**Authorization:** Customer  
**Purpose:** Verify customer dashboard access

#### Success Response (200 OK)
```json
{
  "message": "Welcome to the Customer Dashboard! Access granted."
}
```

---

### 10.2 Agent Dashboard

**Endpoint:** `GET /api/dashboard/agent`  
**Authorization:** Agent  
**Purpose:** Verify agent dashboard access

#### Success Response (200 OK)
```json
{
  "message": "Welcome to the Agent Dashboard! Access granted."
}
```

---

### 10.3 Claim Officer Dashboard

**Endpoint:** `GET /api/dashboard/claim-officer`  
**Authorization:** ClaimOfficer  
**Purpose:** Verify claim officer dashboard access

#### Success Response (200 OK)
```json
{
  "message": "Welcome to the Claim Officer Dashboard! Access granted."
}
```

---

### 10.4 Admin Dashboard

**Endpoint:** `GET /api/dashboard/admin`  
**Authorization:** Admin  
**Purpose:** Verify admin dashboard access

#### Success Response (200 OK)
```json
{
  "message": "Welcome to the Admin Dashboard! Access granted."
}
```

---

## Common Data Models

### RegisterCustomerDto
```typescript
{
  fullName: string,        // Required, max 100 chars
  email: string,           // Required, valid email format
  password: string,        // Required, min 8 chars
  phoneNumber: string,     // Required
  aadhaarNumber?: string   // Optional, 12 digits
}
```

### LoginDto
```typescript
{
  email: string,           // Required
  password: string         // Required
}
```

### CreateAgentDto / CreateClaimOfficerDto
```typescript
{
  fullName: string,              // Required
  email: string,                 // Required
  phoneNumber: string,           // Required
  bankAccountNumber: string,     // Required, 16 digits
  password: string               // Required
}
```

### PolicyApplicationRequest
```typescript
{
  policyCategory: string,        // "Health Insurance - Individual" or "Health Insurance - Family"
  tierId: string,                // "GOLD", "SILVER", etc.
  applicant?: ApplicantDetails,  // For Individual policies
  primaryApplicant?: ApplicantDetails, // For Family policies
  familyMembers?: FamilyMemberDetails[], // For Family policies
  paymentMode: string,           // "Annual", "Semi-Annual", "Quarterly", "Monthly"
  nominee: NomineeDetails
}
```

### ApplicantDetails
```typescript
{
  fullName: string,
  age: number,                   // 18-60
  profession: string,            // "IT Professional", "Business Owner", etc.
  alcoholHabit: string,          // "NonDrinker", "Occasional", "Regular"
  smokingHabit: string,          // "NonSmoker", "Occasional", "Regular"
  travelKmPerMonth: number       // 0-5000+
}
```

### NomineeDetails
```typescript
{
  nomineeName: string,
  nomineeEmail: string,
  nomineePhone: string,
  nomineeBankAccountNumber: string  // 16 digits
}
```

### RaiseClaimRequest
```typescript
{
  policyApplicationId: string,
  incidentType: string,           // "Sickness", "Accident", "Surgery", etc.
  incidentLocation: string,
  incidentDate: Date,
  description: string,
  hospitalName: string,
  hospitalizationRequired: boolean,
  requestedAmount: number,
  affectedMemberName?: string,    // For family policies
  affectedMemberRelation?: string, // "Spouse", "Son", "Daughter", etc.
  documents?: File[]              // Medical bills, reports (multipart/form-data)
}
```

---

## Error Handling

### Standard Error Response Format
```json
{
  "status": "Error",
  "message": "Detailed error message",
  "errors": [
    {
      "field": "email",
      "message": "Email is already registered"
    }
  ]
}
```

### Common Error Scenarios

#### 401 Unauthorized
```json
{
  "status": "Error",
  "message": "Invalid or expired token"
}
```

#### 403 Forbidden
```json
{
  "status": "Error",
  "message": "You do not have permission to access this resource"
}
```

#### 404 Not Found
```json
{
  "status": "Error",
  "message": "Resource not found"
}
```

#### 400 Bad Request
```json
{
  "status": "Error",
  "message": "Validation failed",
  "errors": [
    {
      "field": "age",
      "message": "Age must be between 18 and 60"
    }
  ]
}
```

---

## Status Codes

| Code | Status | Description |
|------|--------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid request parameters or validation error |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | Authenticated but not authorized for this resource |
| 404 | Not Found | Resource not found |
| 500 | Internal Server Error | Server error occurred |

---

## Complete Endpoint Summary

### Authentication (2 endpoints)
- `POST /api/auth/register` - Register new customer
- `POST /api/auth/login` - User login

### Admin Operations (11 endpoints)
- `POST /api/admin/create-agent` - Create agent
- `POST /api/admin/create-claim-officer` - Create claim officer
- `GET /api/admin/agents` - List agents
- `GET /api/admin/claim-officers` - List claim officers
- `DELETE /api/admin/delete-user/{id}` - Delete user
- `GET /api/admin/policy-requests` - View all policies
- `GET /api/admin/agents-with-load` - Agent workload
- `POST /api/admin/assign-agent` - Assign policy to agent
- `GET /api/admin/all-users` - List all users
- `GET /api/admin/all-claims` - View all claims
- `GET /api/admin/admin-stats` - Dashboard statistics

### Agent Operations (5 endpoints)
- `GET /api/agent/my-requests` - Get assigned policies
- `POST /api/agent/review-request` - Approve/reject policy
- `GET /api/agent/my-customers` - List customers
- `GET /api/agent/commission-stats` - Commission earnings
- `GET /api/agent/analytics` - Performance analytics

### Policy Management (4 endpoints)
- `GET /api/policy/configuration` - Get policy plans (Public)
- `POST /api/policy/calculate-premium` - Calculate premium
- `POST /api/policy/apply` - Apply for policy
- `GET /api/policy/my-policies` - View my policies

### Claim Management (8 endpoints)
- `POST /api/claim/raise` - File new claim (Customer)
- `GET /api/claim/my-claims` - View my claims (Customer)
- `GET /api/claim/admin/pending` - Pending claims (Admin)
- `GET /api/claim/admin/officers` - Officer workload (Admin)
- `POST /api/claim/admin/assign` - Assign officer (Admin)
- `GET /api/claim/officer/my-requests` - Assigned claims (Officer)
- `POST /api/claim/officer/review` - Review claim (Officer)
- `GET /api/claim/agent/customer-claims` - Customer claims (Agent)

### Chat (4 endpoints)
- `GET /api/chat/list` - Get chat list
- `GET /api/chat/{policyId}` - Get chat history
- `POST /api/chat/init` - Initialize chat
- `POST /api/chat/{policyId}/read` - Mark as read

### Notifications (4 endpoints)
- `GET /api/notifications` - Get notifications
- `GET /api/notifications/unread-count` - Unread count
- `POST /api/notifications/{id}/read` - Mark one as read
- `POST /api/notifications/read-all` - Mark all as read

### Payments (1 endpoint)
- `POST /api/payment/process` - Process payment

### Reports (1 endpoint)
- `GET /api/report/unified-payments` - Payment report

### Dashboard (4 endpoints)
- `GET /api/dashboard/customer` - Customer access
- `GET /api/dashboard/agent` - Agent access
- `GET /api/dashboard/claim-officer` - Officer access
- `GET /api/dashboard/admin` - Admin access

---

## Integration Examples

### Complete User Journey Example

#### 1. Customer Registration
```bash
curl -X POST https://api.insuranceplatform.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Priya Sharma",
    "email": "priya.sharma@gmail.com",
    "password": "SecurePass123!",
    "phoneNumber": "+919876543210",
    "aadhaarNumber": "123456789012"
  }'
```

#### 2. Customer Login
```bash
curl -X POST https://api.insuranceplatform.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "priya.sharma@gmail.com",
    "password": "SecurePass123!"
  }'
```

#### 3. Get Policy Configuration
```bash
curl -X GET https://api.insuranceplatform.com/api/policy/configuration
```

#### 4. Calculate Premium
```bash
curl -X POST https://api.insuranceplatform.com/api/policy/calculate-premium \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "policyCategory": "Health Insurance - Individual",
    "tierId": "GOLD",
    "applicant": {
      "fullName": "Priya Sharma",
      "age": 32,
      "profession": "IT Professional",
      "alcoholHabit": "NonDrinker",
      "smokingHabit": "NonSmoker",
      "travelKmPerMonth": 500
    },
    "paymentMode": "Annual",
    "nominee": {
      "nomineeName": "Rahul Sharma",
      "nomineeEmail": "rahul@gmail.com",
      "nomineePhone": "+919876543299",
      "nomineeBankAccountNumber": "9988776655443322"
    }
  }'
```

#### 5. Apply for Policy
```bash
curl -X POST https://api.insuranceplatform.com/api/policy/apply \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{ ... same as calculate premium ... }'
```

#### 6. Admin Assigns Agent
```bash
curl -X POST https://api.insuranceplatform.com/api/admin/assign-agent \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "policy-abc123",
    "agentId": "agent-550e8400"
  }'
```

#### 7. Agent Reviews Application
```bash
curl -X POST https://api.insuranceplatform.com/api/agent/review-request \
  -H "Authorization: Bearer <agent_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "policy-abc123",
    "status": "Approved"
  }'
```

#### 8. Customer Makes Payment
```bash
curl -X POST https://api.insuranceplatform.com/api/payment/process \
  -H "Authorization: Bearer <customer_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "policy-abc123",
    "amount": 18000.00
  }'
```

#### 9. Customer Raises Claim
```bash
curl -X POST https://api.insuranceplatform.com/api/claim/raise \
  -H "Authorization: Bearer <customer_token>" \
  -F "policyApplicationId=policy-abc123" \
  -F "incidentType=Sickness" \
  -F "incidentLocation=Mumbai, Maharashtra" \
  -F "incidentDate=2026-02-25" \
  -F "description=Hospitalization due to dengue fever" \
  -F "hospitalName=Apollo Hospital" \
  -F "hospitalizationRequired=true" \
  -F "requestedAmount=50000.00" \
  -F "documents=@medical_bill.pdf"
```

---

## Additional Notes

### Rate Limiting
- Rate limiting is applied per user/IP
- Limits: 100 requests per minute per user
- Exceeded limits return `429 Too Many Requests`

### File Upload Specifications
- **Supported Formats:** PDF, JPG, JPEG, PNG
- **Max File Size:** 5 MB per file
- **Max Files:** 10 files per claim
- **Endpoint:** `/api/claim/raise` (multipart/form-data)

### Date/Time Format
- All datetime fields use ISO 8601 format
- Example: `2026-03-03T08:30:00Z`
- Timezone: UTC

### Currency
- All monetary values are in INR (Indian Rupees)
- Format: Decimal with 2 decimal places
- Example: `18000.00`

---
