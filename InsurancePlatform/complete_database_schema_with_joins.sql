-- ================================================================================
-- INSURANCE PLATFORM - COMPLETE DATABASE CREATION & RELATIONSHIPS
-- ================================================================================
-- Generated: March 3, 2026
-- Database: SQL Server
-- Purpose: End-to-end database creation with CREATE, RELATIONSHIPS, and JOIN examples
-- ================================================================================

USE master;
GO

-- ================================================================================
-- STEP 1: CREATE DATABASE
-- ================================================================================

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'InsurancePlatform')
BEGIN
    ALTER DATABASE InsurancePlatform SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE InsurancePlatform;
END
GO

CREATE DATABASE InsurancePlatform;
GO

USE InsurancePlatform;
GO

PRINT 'Database created successfully';
GO


-- ================================================================================
-- STEP 2: CREATE TABLES (IN ORDER OF DEPENDENCIES)
-- ================================================================================

-- ============================================================
-- 2.1: AspNetRoles (No Dependencies)
-- ============================================================
PRINT 'Creating AspNetRoles table...';
GO

CREATE TABLE [dbo].[AspNetRoles] (
    [Id]                    NVARCHAR(450)       NOT NULL,
    [Name]                  NVARCHAR(256)       NULL,
    [NormalizedName]        NVARCHAR(256)       NULL,
    [ConcurrencyStamp]      NVARCHAR(MAX)       NULL,
    
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] 
    ON [dbo].[AspNetRoles] ([NormalizedName] ASC) 
    WHERE [NormalizedName] IS NOT NULL;
GO

PRINT 'AspNetRoles created successfully';
GO


-- ============================================================
-- 2.2: AspNetUsers (No Dependencies)
-- ============================================================
PRINT 'Creating AspNetUsers table...';
GO

CREATE TABLE [dbo].[AspNetUsers] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [FullName]                  NVARCHAR(100)       NOT NULL,
    [AadhaarNumber]             NVARCHAR(12)        NULL,
    [BankAccountNumber]         NVARCHAR(16)        NULL,
    [CreatedAt]                 DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [InitialPassword]           NVARCHAR(MAX)       NULL,
    [UserName]                  NVARCHAR(256)       NULL,
    [NormalizedUserName]        NVARCHAR(256)       NULL,
    [Email]                     NVARCHAR(256)       NULL,
    [NormalizedEmail]           NVARCHAR(256)       NULL,
    [EmailConfirmed]            BIT                 NOT NULL DEFAULT 0,
    [PasswordHash]              NVARCHAR(MAX)       NULL,
    [SecurityStamp]             NVARCHAR(MAX)       NULL,
    [ConcurrencyStamp]          NVARCHAR(MAX)       NULL,
    [PhoneNumber]               NVARCHAR(MAX)       NULL,
    [PhoneNumberConfirmed]      BIT                 NOT NULL DEFAULT 0,
    [TwoFactorEnabled]          BIT                 NOT NULL DEFAULT 0,
    [LockoutEnd]                DATETIMEOFFSET      NULL,
    [LockoutEnabled]            BIT                 NOT NULL DEFAULT 0,
    [AccessFailedCount]         INT                 NOT NULL DEFAULT 0,
    
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [EmailIndex] 
    ON [dbo].[AspNetUsers] ([NormalizedEmail] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] 
    ON [dbo].[AspNetUsers] ([NormalizedUserName] ASC) 
    WHERE [NormalizedUserName] IS NOT NULL;
GO

PRINT 'AspNetUsers created successfully';
GO


-- ============================================================
-- 2.3: AspNetUserRoles (Depends on: AspNetUsers, AspNetRoles)
-- ============================================================
PRINT 'Creating AspNetUserRoles table...';
GO

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [RoleId]                    NVARCHAR(450)       NOT NULL,
    
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] 
        FOREIGN KEY ([RoleId]) 
        REFERENCES [dbo].[AspNetRoles] ([Id]) 
        ON DELETE CASCADE,
        
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] 
    ON [dbo].[AspNetUserRoles] ([RoleId] ASC);
GO

PRINT 'AspNetUserRoles created with foreign keys';
GO


-- ============================================================
-- 2.4: AspNetUserClaims (Depends on: AspNetUsers)
-- ============================================================
PRINT 'Creating AspNetUserClaims table...';
GO

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id]                        INT                 NOT NULL IDENTITY(1,1),
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [ClaimType]                 NVARCHAR(MAX)       NULL,
    [ClaimValue]                NVARCHAR(MAX)       NULL,
    
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] 
    ON [dbo].[AspNetUserClaims] ([UserId] ASC);
GO

PRINT 'AspNetUserClaims created with foreign keys';
GO


-- ============================================================
-- 2.5: AspNetUserLogins (Depends on: AspNetUsers)
-- ============================================================
PRINT 'Creating AspNetUserLogins table...';
GO

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider]             NVARCHAR(450)       NOT NULL,
    [ProviderKey]               NVARCHAR(450)       NOT NULL,
    [ProviderDisplayName]       NVARCHAR(MAX)       NULL,
    [UserId]                    NVARCHAR(450)       NOT NULL,
    
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
    
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] 
    ON [dbo].[AspNetUserLogins] ([UserId] ASC);
GO

PRINT 'AspNetUserLogins created with foreign keys';
GO


-- ============================================================
-- 2.6: AspNetUserTokens (Depends on: AspNetUsers)
-- ============================================================
PRINT 'Creating AspNetUserTokens table...';
GO

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [LoginProvider]             NVARCHAR(450)       NOT NULL,
    [Name]                      NVARCHAR(450)       NOT NULL,
    [Value]                     NVARCHAR(MAX)       NULL,
    
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
    
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

PRINT 'AspNetUserTokens created with foreign keys';
GO


-- ============================================================
-- 2.7: AspNetRoleClaims (Depends on: AspNetRoles)
-- ============================================================
PRINT 'Creating AspNetRoleClaims table...';
GO

CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id]                        INT                 NOT NULL IDENTITY(1,1),
    [RoleId]                    NVARCHAR(450)       NOT NULL,
    [ClaimType]                 NVARCHAR(MAX)       NULL,
    [ClaimValue]                NVARCHAR(MAX)       NULL,
    
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] 
        FOREIGN KEY ([RoleId]) 
        REFERENCES [dbo].[AspNetRoles] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] 
    ON [dbo].[AspNetRoleClaims] ([RoleId] ASC);
GO

PRINT 'AspNetRoleClaims created with foreign keys';
GO


-- ============================================================
-- 2.8: Notifications (Depends on: AspNetUsers)
-- ============================================================
PRINT 'Creating Notifications table...';
GO

CREATE TABLE [dbo].[Notifications] (
    [Id]                        UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [Title]                     NVARCHAR(200)       NOT NULL,
    [Message]                   NVARCHAR(MAX)       NOT NULL,
    [CreatedAt]                 DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [IsRead]                    BIT                 NOT NULL DEFAULT 0,
    [NotificationType]          NVARCHAR(50)        NOT NULL DEFAULT 'General',
    
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_Notifications_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] 
    ON [dbo].[Notifications] ([UserId] ASC);
GO

PRINT 'Notifications created with foreign keys';
GO


-- ============================================================
-- 2.9: PolicyApplications (Depends on: AspNetUsers)
-- ============================================================
PRINT 'Creating PolicyApplications table...';
GO

CREATE TABLE [dbo].[PolicyApplications] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [PolicyCategory]            NVARCHAR(MAX)       NOT NULL,
    [TierId]                    NVARCHAR(MAX)       NOT NULL,
    [CalculatedPremium]         DECIMAL(18,2)       NOT NULL,
    [SubmissionDate]            DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [Status]                    NVARCHAR(MAX)       NOT NULL DEFAULT 'Pending',
    [ApplicationDataJson]       NVARCHAR(MAX)       NOT NULL,
    [AssignedAgentId]           NVARCHAR(450)       NULL,
    [ApprovedByAgentId]         NVARCHAR(MAX)       NULL,
    [ApprovedAt]                DATETIME2           NULL,
    [TotalCoverageAmount]       DECIMAL(18,2)       NOT NULL,
    [RemainingCoverageAmount]   DECIMAL(18,2)       NOT NULL,
    [TotalApprovedClaimsAmount] DECIMAL(18,2)       NOT NULL DEFAULT 0,
    [PaymentMode]               NVARCHAR(MAX)       NULL,
    [NextPaymentDate]           DATETIME2           NULL,
    [StartDate]                 DATETIME2           NULL,
    [ExpiryDate]                DATETIME2           NULL,
    [PaidAmount]                DECIMAL(18,2)       NULL,
    [PaymentDate]               DATETIME2           NULL,
    [TransactionId]             NVARCHAR(MAX)       NULL,
    
    CONSTRAINT [PK_PolicyApplications] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_PolicyApplications_AspNetUsers_AssignedAgentId] 
        FOREIGN KEY ([AssignedAgentId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE NO ACTION,
        
    CONSTRAINT [FK_PolicyApplications_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_PolicyApplications_AssignedAgentId] 
    ON [dbo].[PolicyApplications] ([AssignedAgentId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_PolicyApplications_UserId] 
    ON [dbo].[PolicyApplications] ([UserId] ASC);
GO

PRINT 'PolicyApplications created with foreign keys';
GO


-- ============================================================
-- 2.10: InsuranceClaims (Depends on: AspNetUsers, PolicyApplications)
-- ============================================================
PRINT 'Creating InsuranceClaims table...';
GO

CREATE TABLE [dbo].[InsuranceClaims] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [PolicyApplicationId]       NVARCHAR(450)       NOT NULL,
    [UserId]                    NVARCHAR(450)       NOT NULL,
    [RequestedAmount]           DECIMAL(18,2)       NOT NULL,
    [ApprovedAmount]            DECIMAL(18,2)       NOT NULL DEFAULT 0,
    [Description]               NVARCHAR(MAX)       NOT NULL,
    [IncidentDate]              DATETIME2           NOT NULL,
    [SubmissionDate]            DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [IncidentType]              NVARCHAR(MAX)       NOT NULL,
    [IncidentLocation]          NVARCHAR(MAX)       NOT NULL,
    [HospitalName]              NVARCHAR(MAX)       NOT NULL,
    [HospitalizationRequired]   BIT                 NOT NULL,
    [AffectedMemberName]        NVARCHAR(MAX)       NULL,
    [AffectedMemberRelation]    NVARCHAR(MAX)       NULL,
    [Status]                    NVARCHAR(MAX)       NOT NULL DEFAULT 'Pending',
    [Remarks]                   NVARCHAR(MAX)       NULL,
    [AssignedClaimOfficerId]    NVARCHAR(450)       NULL,
    [ApprovedByOfficerId]       NVARCHAR(MAX)       NULL,
    [ProcessedAt]               DATETIME2           NULL,
    
    CONSTRAINT [PK_InsuranceClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_InsuranceClaims_AspNetUsers_AssignedClaimOfficerId] 
        FOREIGN KEY ([AssignedClaimOfficerId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE NO ACTION,
        
    CONSTRAINT [FK_InsuranceClaims_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE CASCADE,
        
    CONSTRAINT [FK_InsuranceClaims_PolicyApplications_PolicyApplicationId] 
        FOREIGN KEY ([PolicyApplicationId]) 
        REFERENCES [dbo].[PolicyApplications] ([Id]) 
        ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX [IX_InsuranceClaims_AssignedClaimOfficerId] 
    ON [dbo].[InsuranceClaims] ([AssignedClaimOfficerId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_InsuranceClaims_PolicyApplicationId] 
    ON [dbo].[InsuranceClaims] ([PolicyApplicationId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_InsuranceClaims_UserId] 
    ON [dbo].[InsuranceClaims] ([UserId] ASC);
GO

PRINT 'InsuranceClaims created with foreign keys';
GO


-- ============================================================
-- 2.11: ClaimDocuments (Depends on: InsuranceClaims)
-- ============================================================
PRINT 'Creating ClaimDocuments table...';
GO

CREATE TABLE [dbo].[ClaimDocuments] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [ClaimId]                   NVARCHAR(450)       NOT NULL,
    [FileId]                    NVARCHAR(MAX)       NOT NULL,
    [FileName]                  NVARCHAR(MAX)       NOT NULL,
    [FileUrl]                   NVARCHAR(MAX)       NOT NULL,
    [FileSize]                  BIGINT              NOT NULL,
    [UploadedAt]                DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [PK_ClaimDocuments] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_ClaimDocuments_InsuranceClaims_ClaimId] 
        FOREIGN KEY ([ClaimId]) 
        REFERENCES [dbo].[InsuranceClaims] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ClaimDocuments_ClaimId] 
    ON [dbo].[ClaimDocuments] ([ClaimId] ASC);
GO

PRINT 'ClaimDocuments created with foreign keys';
GO


-- ============================================================
-- 2.12: Chats (Depends on: PolicyApplications)
-- ============================================================
PRINT 'Creating Chats table...';
GO

CREATE TABLE [dbo].[Chats] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [PolicyId]                  NVARCHAR(450)       NOT NULL,
    [CustomerId]                NVARCHAR(MAX)       NOT NULL,
    [AgentId]                   NVARCHAR(MAX)       NOT NULL,
    [CustomerEmail]             NVARCHAR(MAX)       NULL,
    [AgentEmail]                NVARCHAR(MAX)       NULL,
    [PolicyName]                NVARCHAR(MAX)       NULL,
    [Category]                  NVARCHAR(MAX)       NULL,
    [CoverageAmount]            DECIMAL(18,2)       NOT NULL,
    [DateActivated]             DATETIME2           NOT NULL,
    [LastMessage]               NVARCHAR(MAX)       NULL,
    [LastMessageTime]           DATETIME2           NULL,
    [CreatedAt]                 DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]                 DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [PK_Chats] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_Chats_PolicyApplications_PolicyId] 
        FOREIGN KEY ([PolicyId]) 
        REFERENCES [dbo].[PolicyApplications] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Chats_PolicyId] 
    ON [dbo].[Chats] ([PolicyId] ASC);
GO

PRINT 'Chats created with foreign keys';
GO


-- ============================================================
-- 2.13: ChatMessages (Depends on: Chats)
-- ============================================================
PRINT 'Creating ChatMessages table...';
GO

CREATE TABLE [dbo].[ChatMessages] (
    [Id]                        NVARCHAR(450)       NOT NULL,
    [ChatId]                    NVARCHAR(450)       NOT NULL,
    [SenderId]                  NVARCHAR(MAX)       NOT NULL,
    [SenderRole]                NVARCHAR(MAX)       NOT NULL,
    [Message]                   NVARCHAR(MAX)       NOT NULL,
    [Timestamp]                 DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    [IsRead]                    BIT                 NOT NULL DEFAULT 0,
    
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    CONSTRAINT [FK_ChatMessages_Chats_ChatId] 
        FOREIGN KEY ([ChatId]) 
        REFERENCES [dbo].[Chats] ([Id]) 
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ChatMessages_ChatId] 
    ON [dbo].[ChatMessages] ([ChatId] ASC);
GO

PRINT 'ChatMessages created with foreign keys';
GO

PRINT 'All tables created successfully!';
GO


-- ================================================================================
-- STEP 3: INSERT SAMPLE DATA
-- ================================================================================

PRINT 'Inserting sample data...';
GO

-- ============================================================
-- 3.1: Insert Roles
-- ============================================================
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES 
    (NEWID(), 'Admin', 'ADMIN', NEWID()),
    (NEWID(), 'Agent', 'AGENT', NEWID()),
    (NEWID(), 'Customer', 'CUSTOMER', NEWID()),
    (NEWID(), 'ClaimOfficer', 'CLAIMOFFICER', NEWID());
GO

PRINT 'Roles inserted';
GO


-- ============================================================
-- 3.2: Insert Sample Users
-- ============================================================
DECLARE @AdminId NVARCHAR(450) = NEWID();
DECLARE @AgentId NVARCHAR(450) = NEWID();
DECLARE @CustomerId NVARCHAR(450) = NEWID();
DECLARE @OfficerId NVARCHAR(450) = NEWID();

DECLARE @AdminRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin');
DECLARE @AgentRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Agent');
DECLARE @CustomerRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Customer');
DECLARE @OfficerRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'ClaimOfficer');

-- Insert Admin
INSERT INTO [AspNetUsers] ([Id], [FullName], [Email], [NormalizedEmail], [UserName], [NormalizedUserName], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [CreatedAt])
VALUES 
    (@AdminId, 'System Administrator', 'admin@insuranceplatform.com', 'ADMIN@INSURANCEPLATFORM.COM', 
     'admin@insuranceplatform.com', 'ADMIN@INSURANCEPLATFORM.COM', 1, 
     'AQAAAAEAACcQAAAAEDummyHashForDemo', NEWID(), NEWID(), '+919876543210', GETUTCDATE());

-- Insert Agent
INSERT INTO [AspNetUsers] ([Id], [FullName], [Email], [NormalizedEmail], [UserName], [NormalizedUserName], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [BankAccountNumber], 
    [InitialPassword], [CreatedAt])
VALUES 
    (@AgentId, 'Rajesh Kumar', 'rajesh.kumar@insuranceplatform.com', 'RAJESH.KUMAR@INSURANCEPLATFORM.COM', 
     'rajesh.kumar@insuranceplatform.com', 'RAJESH.KUMAR@INSURANCEPLATFORM.COM', 1, 
     'AQAAAAEAACcQAAAAEDummyHashForDemo', NEWID(), NEWID(), '+919876543211', '1234567890123456', 
     'Agent@123', GETUTCDATE());

-- Insert Customer
INSERT INTO [AspNetUsers] ([Id], [FullName], [Email], [NormalizedEmail], [UserName], [NormalizedUserName], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [AadhaarNumber], [CreatedAt])
VALUES 
    (@CustomerId, 'Priya Sharma', 'priya.sharma@gmail.com', 'PRIYA.SHARMA@GMAIL.COM', 
     'priya.sharma@gmail.com', 'PRIYA.SHARMA@GMAIL.COM', 1, 
     'AQAAAAEAACcQAAAAEDummyHashForDemo', NEWID(), NEWID(), '+919876543212', '123456789012', GETUTCDATE());

-- Insert Claim Officer
INSERT INTO [AspNetUsers] ([Id], [FullName], [Email], [NormalizedEmail], [UserName], [NormalizedUserName], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [BankAccountNumber], 
    [InitialPassword], [CreatedAt])
VALUES 
    (@OfficerId, 'Amit Patel', 'amit.patel@insuranceplatform.com', 'AMIT.PATEL@INSURANCEPLATFORM.COM', 
     'amit.patel@insuranceplatform.com', 'AMIT.PATEL@INSURANCEPLATFORM.COM', 1, 
     'AQAAAAEAACcQAAAAEDummyHashForDemo', NEWID(), NEWID(), '+919876543213', '9876543210123456', 
     'Officer@123', GETUTCDATE());

-- Assign Roles
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES 
    (@AdminId, @AdminRoleId),
    (@AgentId, @AgentRoleId),
    (@CustomerId, @CustomerRoleId),
    (@OfficerId, @OfficerRoleId);

PRINT 'Users and roles assigned';
GO


-- ============================================================
-- 3.3: Insert Sample Policy Application
-- ============================================================
DECLARE @PolicyId NVARCHAR(450) = NEWID();
DECLARE @CustomerId2 NVARCHAR(450) = (SELECT TOP 1 u.Id FROM AspNetUsers u 
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE r.Name = 'Customer');
DECLARE @AgentId2 NVARCHAR(450) = (SELECT TOP 1 u.Id FROM AspNetUsers u 
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE r.Name = 'Agent');

INSERT INTO [PolicyApplications] 
    ([Id], [UserId], [PolicyCategory], [TierId], [CalculatedPremium], [SubmissionDate], [Status], 
     [ApplicationDataJson], [AssignedAgentId], [ApprovedAt], [TotalCoverageAmount], [RemainingCoverageAmount], 
     [PaymentMode], [StartDate], [ExpiryDate], [PaidAmount], [PaymentDate], [TransactionId])
VALUES 
    (@PolicyId, @CustomerId2, 'Health Insurance - Individual', 'GOLD', 15000.00, GETUTCDATE(), 'Approved', 
     '{"applicant":{"name":"Priya Sharma","age":32}}', @AgentId2, GETUTCDATE(), 500000.00, 500000.00, 
     'Annual', GETUTCDATE(), DATEADD(YEAR, 1, GETUTCDATE()), 15000.00, GETUTCDATE(), 'TXN001234567890');

PRINT 'Policy application inserted';
GO


-- ============================================================
-- 3.4: Insert Sample Insurance Claim
-- ============================================================
DECLARE @ClaimId NVARCHAR(450) = NEWID();
DECLARE @PolicyId2 NVARCHAR(450) = (SELECT TOP 1 Id FROM PolicyApplications);
DECLARE @CustomerId3 NVARCHAR(450) = (SELECT TOP 1 UserId FROM PolicyApplications);
DECLARE @OfficerId2 NVARCHAR(450) = (SELECT TOP 1 u.Id FROM AspNetUsers u 
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE r.Name = 'ClaimOfficer');

INSERT INTO [InsuranceClaims]
    ([Id], [PolicyApplicationId], [UserId], [RequestedAmount], [ApprovedAmount], [Description], 
     [IncidentDate], [SubmissionDate], [IncidentType], [IncidentLocation], [HospitalName], 
     [HospitalizationRequired], [Status], [AssignedClaimOfficerId], [Remarks])
VALUES 
    (@ClaimId, @PolicyId2, @CustomerId3, 50000.00, 45000.00, 'Hospitalization due to dengue fever', 
     DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 'Sickness', 'Mumbai, Maharashtra', 'Apollo Hospital', 
     1, 'Approved', @OfficerId2, 'Claim approved with deductible');

PRINT 'Insurance claim inserted';
GO


-- ============================================================
-- 3.5: Insert Sample Claim Document
-- ============================================================
DECLARE @DocumentId NVARCHAR(450) = NEWID();
DECLARE @ClaimId2 NVARCHAR(450) = (SELECT TOP 1 Id FROM InsuranceClaims);

INSERT INTO [ClaimDocuments]
    ([Id], [ClaimId], [FileId], [FileName], [FileUrl], [FileSize], [UploadedAt])
VALUES 
    (@DocumentId, @ClaimId2, 'file_abc123xyz', 'medical_bill.pdf', 
     'https://ik.imagekit.io/demo/medical_bill.pdf', 524288, GETUTCDATE());

PRINT 'Claim document inserted';
GO


-- ============================================================
-- 3.6: Insert Sample Chat and Messages
-- ============================================================
DECLARE @ChatId NVARCHAR(450) = NEWID();
DECLARE @PolicyId3 NVARCHAR(450) = (SELECT TOP 1 Id FROM PolicyApplications);
DECLARE @CustomerId4 NVARCHAR(450) = (SELECT TOP 1 UserId FROM PolicyApplications);
DECLARE @AgentId3 NVARCHAR(450) = (SELECT TOP 1 AssignedAgentId FROM PolicyApplications WHERE AssignedAgentId IS NOT NULL);

INSERT INTO [Chats]
    ([Id], [PolicyId], [CustomerId], [AgentId], [CustomerEmail], [AgentEmail], [PolicyName], 
     [Category], [CoverageAmount], [DateActivated], [LastMessage], [LastMessageTime], [CreatedAt], [UpdatedAt])
VALUES 
    (@ChatId, @PolicyId3, @CustomerId4, @AgentId3, 'priya.sharma@gmail.com', 'rajesh.kumar@insuranceplatform.com', 
     'Health Insurance - Individual', 'Health', 500000.00, GETUTCDATE(), 'Thank you for your help!', 
     GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- Insert Chat Messages
INSERT INTO [ChatMessages] ([Id], [ChatId], [SenderId], [SenderRole], [Message], [Timestamp], [IsRead])
VALUES 
    (NEWID(), @ChatId, @CustomerId4, 'Customer', 'Hello, I have a query about my policy coverage', DATEADD(MINUTE, -30, GETUTCDATE()), 1),
    (NEWID(), @ChatId, @AgentId3, 'Agent', 'Hello! I''d be happy to help. What would you like to know?', DATEADD(MINUTE, -28, GETUTCDATE()), 1),
    (NEWID(), @ChatId, @CustomerId4, 'Customer', 'Does my policy cover pre-existing conditions?', DATEADD(MINUTE, -25, GETUTCDATE()), 1),
    (NEWID(), @ChatId, @AgentId3, 'Agent', 'Your policy covers pre-existing conditions after a waiting period of 2 years.', DATEADD(MINUTE, -23, GETUTCDATE()), 1),
    (NEWID(), @ChatId, @CustomerId4, 'Customer', 'Thank you for your help!', GETUTCDATE(), 1);

PRINT 'Chat and messages inserted';
GO


-- ============================================================
-- 3.7: Insert Sample Notifications
-- ============================================================
DECLARE @CustomerId5 NVARCHAR(450) = (SELECT TOP 1 u.Id FROM AspNetUsers u 
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE r.Name = 'Customer');

INSERT INTO [Notifications] ([Id], [UserId], [Title], [Message], [CreatedAt], [IsRead], [NotificationType])
VALUES 
    (NEWID(), @CustomerId5, 'Policy Approved', 'Your health insurance policy has been approved!', GETUTCDATE(), 0, 'Policy'),
    (NEWID(), @CustomerId5, 'Claim Update', 'Your claim #CLM001 has been approved for ₹45,000', DATEADD(HOUR, -2, GETUTCDATE()), 1, 'Claim'),
    (NEWID(), @CustomerId5, 'Payment Reminder', 'Your next premium payment is due on ' + CONVERT(VARCHAR, DATEADD(MONTH, 1, GETUTCDATE()), 106), GETUTCDATE(), 0, 'Payment');

PRINT 'Notifications inserted';
GO

PRINT 'Sample data insertion completed!';
GO


-- ================================================================================
-- STEP 4: RELATIONSHIP DIAGRAM (FOREIGN KEY SUMMARY)
-- ================================================================================

PRINT '
================================================================================
                    DATABASE RELATIONSHIPS SUMMARY
================================================================================

TABLE RELATIONSHIPS (FOREIGN KEYS):

1. AspNetUserRoles
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]
   -> RoleId REFERENCES AspNetRoles(Id) [CASCADE DELETE]

2. AspNetUserClaims
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]

3. AspNetUserLogins
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]

4. AspNetUserTokens
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]

5. AspNetRoleClaims
   -> RoleId REFERENCES AspNetRoles(Id) [CASCADE DELETE]

6. Notifications
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]

7. PolicyApplications
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]
   -> AssignedAgentId REFERENCES AspNetUsers(Id) [NO ACTION]

8. InsuranceClaims
   -> PolicyApplicationId REFERENCES PolicyApplications(Id) [NO ACTION]
   -> UserId REFERENCES AspNetUsers(Id) [CASCADE DELETE]
   -> AssignedClaimOfficerId REFERENCES AspNetUsers(Id) [NO ACTION]

9. ClaimDocuments
   -> ClaimId REFERENCES InsuranceClaims(Id) [CASCADE DELETE]

10. Chats
    -> PolicyId REFERENCES PolicyApplications(Id) [CASCADE DELETE]

11. ChatMessages
    -> ChatId REFERENCES Chats(Id) [CASCADE DELETE]

================================================================================
';
GO


-- ================================================================================
-- STEP 5: COMMON JOIN QUERIES - PRACTICAL EXAMPLES
-- ================================================================================

PRINT 'Demonstrating JOIN queries...';
GO


-- ============================================================
-- QUERY 1: Get all customers with their roles
-- ============================================================
PRINT '
================================================================================
QUERY 1: Get all customers with their roles
================================================================================
';

SELECT 
    u.Id AS UserId,
    u.FullName,
    u.Email,
    u.PhoneNumber,
    u.AadhaarNumber,
    r.Name AS RoleName,
    u.CreatedAt
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Customer';
GO


-- ============================================================
-- QUERY 2: Get all policies with customer and agent details
-- ============================================================
PRINT '
================================================================================
QUERY 2: Get all policies with customer and agent details
================================================================================
';

SELECT 
    p.Id AS PolicyId,
    p.PolicyCategory,
    p.TierId,
    p.Status,
    p.TotalCoverageAmount,
    p.RemainingCoverageAmount,
    p.CalculatedPremium,
    p.StartDate,
    p.ExpiryDate,
    -- Customer Details
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    customer.PhoneNumber AS CustomerPhone,
    -- Agent Details
    agent.FullName AS AgentName,
    agent.Email AS AgentEmail,
    agent.PhoneNumber AS AgentPhone
FROM PolicyApplications p
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
LEFT JOIN AspNetUsers agent ON p.AssignedAgentId = agent.Id;
GO


-- ============================================================
-- QUERY 3: Get all claims with policy, customer, and officer details  
-- ============================================================
PRINT '
================================================================================
QUERY 3: Get all claims with policy, customer, and officer details
================================================================================
';

SELECT 
    c.Id AS ClaimId,
    c.Status AS ClaimStatus,
    c.RequestedAmount,
    c.ApprovedAmount,
    c.IncidentType,
    c.IncidentDate,
    c.SubmissionDate,
    c.HospitalName,
    -- Customer Details
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    customer.PhoneNumber AS CustomerPhone,
    -- Policy Details
    p.PolicyCategory,
    p.TierId,
    p.TotalCoverageAmount,
    -- Claim Officer Details
    officer.FullName AS OfficerName,
    officer.Email AS OfficerEmail,
    c.Remarks AS OfficerRemarks
FROM InsuranceClaims c
INNER JOIN AspNetUsers customer ON c.UserId = customer.Id
INNER JOIN PolicyApplications p ON c.PolicyApplicationId = p.Id
LEFT JOIN AspNetUsers officer ON c.AssignedClaimOfficerId = officer.Id;
GO


-- ============================================================
-- QUERY 4: Get claim with all uploaded documents
-- ============================================================
PRINT '
================================================================================
QUERY 4: Get claim with all uploaded documents
================================================================================
';

SELECT 
    c.Id AS ClaimId,
    c.Status AS ClaimStatus,
    c.RequestedAmount,
    customer.FullName AS CustomerName,
    -- Document Details
    d.FileName,
    d.FileUrl,
    d.FileSize,
    d.UploadedAt
FROM InsuranceClaims c
INNER JOIN AspNetUsers customer ON c.UserId = customer.Id
LEFT JOIN ClaimDocuments d ON c.Id = d.ClaimId;
GO


-- ============================================================
-- QUERY 5: Get chat conversations with messages
-- ============================================================
PRINT '
================================================================================
QUERY 5: Get chat conversations with messages
================================================================================
';

SELECT 
    ch.Id AS ChatId,
    ch.PolicyName,
    ch.Category,
    customer.FullName AS CustomerName,
    agent.FullName AS AgentName,
    -- Message Details
    m.Message,
    m.SenderRole,
    m.Timestamp,
    m.IsRead
FROM Chats ch
INNER JOIN PolicyApplications p ON ch.PolicyId = p.Id
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
LEFT JOIN AspNetUsers agent ON ch.AgentId = agent.Id
LEFT JOIN ChatMessages m ON ch.Id = m.ChatId
ORDER BY m.Timestamp ASC;
GO


-- ============================================================
-- QUERY 6: Get complete policy history (policy -> claims -> documents)
-- ============================================================
PRINT '
================================================================================
QUERY 6: Get complete policy history with claims and documents
================================================================================
';

SELECT 
    p.Id AS PolicyId,
    p.PolicyCategory,
    p.Status AS PolicyStatus,
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    -- Claim Details
    c.Id AS ClaimId,
    c.Status AS ClaimStatus,
    c.RequestedAmount,
    c.ApprovedAmount,
    c.IncidentType,
    c.SubmissionDate AS ClaimSubmissionDate,
    -- Document Details
    d.FileName,
    d.FileUrl,
    d.UploadedAt AS DocumentUploadedAt
FROM PolicyApplications p
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
LEFT JOIN InsuranceClaims c ON p.Id = c.PolicyApplicationId
LEFT JOIN ClaimDocuments d ON c.Id = d.ClaimId;
GO


-- ============================================================
-- QUERY 7: Get agent workload (assigned policies and claims)
-- ============================================================
PRINT '
================================================================================
QUERY 7: Get agent workload summary
================================================================================
';

SELECT 
    agent.FullName AS AgentName,
    agent.Email AS AgentEmail,
    COUNT(DISTINCT p.Id) AS TotalAssignedPolicies,
    COUNT(DISTINCT CASE WHEN p.Status = 'Pending' THEN p.Id END) AS PendingPolicies,
    COUNT(DISTINCT CASE WHEN p.Status = 'Approved' THEN p.Id END) AS ApprovedPolicies
FROM AspNetUsers agent
INNER JOIN AspNetUserRoles ur ON agent.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN PolicyApplications p ON agent.Id = p.AssignedAgentId
WHERE r.Name = 'Agent'
GROUP BY agent.FullName, agent.Email;
GO


-- ============================================================
-- QUERY 8: Get claim officer workload
-- ============================================================
PRINT '
================================================================================
QUERY 8: Get claim officer workload summary
================================================================================
';

SELECT 
    officer.FullName AS OfficerName,
    officer.Email AS OfficerEmail,
    COUNT(DISTINCT c.Id) AS TotalAssignedClaims,
    COUNT(DISTINCT CASE WHEN c.Status = 'Pending' THEN c.Id END) AS PendingClaims,
    COUNT(DISTINCT CASE WHEN c.Status = 'Approved' THEN c.Id END) AS ApprovedClaims,
    COUNT(DISTINCT CASE WHEN c.Status = 'Rejected' THEN c.Id END) AS RejectedClaims,
    SUM(c.ApprovedAmount) AS TotalApprovedAmount
FROM AspNetUsers officer
INNER JOIN AspNetUserRoles ur ON officer.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN InsuranceClaims c ON officer.Id = c.AssignedClaimOfficerId
WHERE r.Name = 'ClaimOfficer'
GROUP BY officer.FullName, officer.Email;
GO


-- ============================================================
-- QUERY 9: Get customer dashboard summary
-- ============================================================
PRINT '
================================================================================
QUERY 9: Get customer dashboard summary
================================================================================
';

SELECT 
    customer.FullName AS CustomerName,
    customer.Email,
    -- Policy Stats
    COUNT(DISTINCT p.Id) AS TotalPolicies,
    SUM(p.TotalCoverageAmount) AS TotalCoverage,
    SUM(p.RemainingCoverageAmount) AS RemainingCoverage,
    -- Claim Stats
    COUNT(DISTINCT c.Id) AS TotalClaims,
    SUM(c.RequestedAmount) AS TotalClaimRequested,
    SUM(c.ApprovedAmount) AS TotalClaimApproved,
    -- Notification Stats
    COUNT(DISTINCT n.Id) AS TotalNotifications,
    COUNT(DISTINCT CASE WHEN n.IsRead = 0 THEN n.Id END) AS UnreadNotifications
FROM AspNetUsers customer
INNER JOIN AspNetUserRoles ur ON customer.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN PolicyApplications p ON customer.Id = p.UserId
LEFT JOIN InsuranceClaims c ON customer.Id = c.UserId
LEFT JOIN Notifications n ON customer.Id = n.UserId
WHERE r.Name = 'Customer'
GROUP BY customer.FullName, customer.Email;
GO


-- ============================================================
-- QUERY 10: Get unread notifications for a user
-- ============================================================
PRINT '
================================================================================
QUERY 10: Get unread notifications for users
================================================================================
';

SELECT 
    u.FullName AS UserName,
    u.Email,
    n.Title AS NotificationTitle,
    n.Message AS NotificationMessage,
    n.NotificationType,
    n.CreatedAt,
    n.IsRead
FROM Notifications n
INNER JOIN AspNetUsers u ON n.UserId = u.Id
WHERE n.IsRead = 0
ORDER BY n.CreatedAt DESC;
GO


-- ============================================================
-- QUERY 11: Get chat history with unread message count
-- ============================================================
PRINT '
================================================================================
QUERY 11: Get chat history with unread message count
================================================================================
';

SELECT 
    ch.Id AS ChatId,
    ch.PolicyName,
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    agent.FullName AS AgentName,
    agent.Email AS AgentEmail,
    ch.LastMessage,
    ch.LastMessageTime,
    COUNT(CASE WHEN m.IsRead = 0 THEN 1 END) AS UnreadMessageCount,
    COUNT(m.Id) AS TotalMessages
FROM Chats ch
INNER JOIN PolicyApplications p ON ch.PolicyId = p.Id
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
LEFT JOIN AspNetUsers agent ON ch.AgentId = agent.Id
LEFT JOIN ChatMessages m ON ch.Id = m.ChatId
GROUP BY ch.Id, ch.PolicyName, customer.FullName, customer.Email, 
         agent.FullName, agent.Email, ch.LastMessage, ch.LastMessageTime;
GO


-- ============================================================
-- QUERY 12: Complex JOIN - Get complete insurance flow
-- ============================================================
PRINT '
================================================================================
QUERY 12: Complete insurance flow (User -> Policy -> Claim -> Documents -> Chat)
================================================================================
';

SELECT 
    -- User Info
    u.FullName AS CustomerName,
    u.Email AS CustomerEmail,
    u.PhoneNumber AS CustomerPhone,
    -- Policy Info
    p.Id AS PolicyId,
    p.PolicyCategory,
    p.TierId,
    p.Status AS PolicyStatus,
    p.TotalCoverageAmount,
    p.RemainingCoverageAmount,
    -- Agent Info
    agent.FullName AS AssignedAgentName,
    -- Claim Info
    c.Id AS ClaimId,
    c.Status AS ClaimStatus,
    c.RequestedAmount,
    c.ApprovedAmount,
    c.IncidentType,
    -- Officer Info
    officer.FullName AS ClaimOfficerName,
    -- Document Info
    d.FileName AS ClaimDocument,
    -- Chat Info
    ch.Id AS ChatId,
    ch.LastMessage AS LastChatMessage
FROM AspNetUsers u
LEFT JOIN PolicyApplications p ON u.Id = p.UserId
LEFT JOIN AspNetUsers agent ON p.AssignedAgentId = agent.Id
LEFT JOIN InsuranceClaims c ON p.Id = c.PolicyApplicationId
LEFT JOIN AspNetUsers officer ON c.AssignedClaimOfficerId = officer.Id
LEFT JOIN ClaimDocuments d ON c.Id = d.ClaimId
LEFT JOIN Chats ch ON p.Id = ch.PolicyId
WHERE EXISTS (
    SELECT 1 FROM AspNetUserRoles ur 
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id 
    WHERE ur.UserId = u.Id AND r.Name = 'Customer'
);
GO


-- ============================================================
-- QUERY 13: Admin dashboard statistics
-- ============================================================
PRINT '
================================================================================
QUERY 13: Admin dashboard statistics (aggregated data)
================================================================================
';

SELECT 
    -- User Stats
    (SELECT COUNT(*) FROM AspNetUsers u 
     INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
     INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
     WHERE r.Name = 'Customer') AS TotalCustomers,
    
    (SELECT COUNT(*) FROM AspNetUsers u 
     INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
     INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
     WHERE r.Name = 'Agent') AS TotalAgents,
    
    (SELECT COUNT(*) FROM AspNetUsers u 
     INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
     INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
     WHERE r.Name = 'ClaimOfficer') AS TotalClaimOfficers,
    
    -- Policy Stats
    (SELECT COUNT(*) FROM PolicyApplications) AS TotalPolicies,
    (SELECT COUNT(*) FROM PolicyApplications WHERE Status = 'Pending') AS PendingPolicies,
    (SELECT COUNT(*) FROM PolicyApplications WHERE Status = 'Approved') AS ApprovedPolicies,
    (SELECT COUNT(*) FROM PolicyApplications WHERE Status = 'Rejected') AS RejectedPolicies,
    (SELECT SUM(TotalCoverageAmount) FROM PolicyApplications WHERE Status = 'Approved') AS TotalCoverageAmount,
    
    -- Claim Stats
    (SELECT COUNT(*) FROM InsuranceClaims) AS TotalClaims,
    (SELECT COUNT(*) FROM InsuranceClaims WHERE Status = 'Pending') AS PendingClaims,
    (SELECT COUNT(*) FROM InsuranceClaims WHERE Status = 'Approved') AS ApprovedClaims,
    (SELECT COUNT(*) FROM InsuranceClaims WHERE Status = 'Rejected') AS RejectedClaims,
    (SELECT SUM(RequestedAmount) FROM InsuranceClaims) AS TotalClaimRequested,
    (SELECT SUM(ApprovedAmount) FROM InsuranceClaims) AS TotalClaimApproved,
    
    -- Communication Stats
    (SELECT COUNT(*) FROM Chats) AS TotalChats,
    (SELECT COUNT(*) FROM ChatMessages) AS TotalChatMessages,
    (SELECT COUNT(*) FROM Notifications) AS TotalNotifications;
GO


-- ================================================================================
-- STEP 6: ADVANCED QUERIES - BUSINESS LOGIC
-- ================================================================================

PRINT 'Demonstrating advanced business queries...';
GO


-- ============================================================
-- QUERY 14: Find policies about to expire (next 30 days)
-- ============================================================
PRINT '
================================================================================
QUERY 14: Find policies expiring in next 30 days
================================================================================
';

SELECT 
    p.Id AS PolicyId,
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    customer.PhoneNumber AS CustomerPhone,
    p.PolicyCategory,
    p.ExpiryDate,
    DATEDIFF(DAY, GETDATE(), p.ExpiryDate) AS DaysUntilExpiry,
    p.CalculatedPremium AS RenewalAmount
FROM PolicyApplications p
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
WHERE p.Status = 'Approved'
  AND p.ExpiryDate IS NOT NULL
  AND p.ExpiryDate BETWEEN GETDATE() AND DATEADD(DAY, 30, GETDATE())
ORDER BY p.ExpiryDate ASC;
GO


-- ============================================================
-- QUERY 15: Find customers with high claim utilization
-- ============================================================
PRINT '
================================================================================
QUERY 15: Find customers with high claim utilization (>50% of coverage)
================================================================================
';

SELECT 
    customer.FullName AS CustomerName,
    customer.Email,
    p.PolicyCategory,
    p.TotalCoverageAmount,
    p.RemainingCoverageAmount,
    p.TotalApprovedClaimsAmount,
    CAST((p.TotalApprovedClaimsAmount * 100.0 / NULLIF(p.TotalCoverageAmount, 0)) AS DECIMAL(5,2)) AS UtilizationPercentage
FROM PolicyApplications p
INNER JOIN AspNetUsers customer ON p.UserId = customer.Id
WHERE p.Status = 'Approved'
  AND p.TotalCoverageAmount > 0
  AND (p.TotalApprovedClaimsAmount * 100.0 / p.TotalCoverageAmount) > 50
ORDER BY UtilizationPercentage DESC;
GO


-- ============================================================
-- QUERY 16: Find claims pending for more than 7 days
-- ============================================================
PRINT '
================================================================================
QUERY 16: Find claims pending for more than 7 days
================================================================================
';

SELECT 
    c.Id AS ClaimId,
    customer.FullName AS CustomerName,
    customer.Email AS CustomerEmail,
    c.RequestedAmount,
    c.IncidentType,
    c.SubmissionDate,
    DATEDIFF(DAY, c.SubmissionDate, GETDATE()) AS DaysPending,
    officer.FullName AS AssignedOfficer
FROM InsuranceClaims c
INNER JOIN AspNetUsers customer ON c.UserId = customer.Id
LEFT JOIN AspNetUsers officer ON c.AssignedClaimOfficerId = officer.Id
WHERE c.Status = 'Pending'
  AND DATEDIFF(DAY, c.SubmissionDate, GETDATE()) > 7
ORDER BY DaysPending DESC;
GO


-- ============================================================
-- QUERY 17: Monthly revenue report
-- ============================================================
PRINT '
================================================================================
QUERY 17: Monthly revenue report (last 6 months)
================================================================================
';

SELECT 
    YEAR(p.PaymentDate) AS Year,
    MONTH(p.PaymentDate) AS Month,
    DATENAME(MONTH, p.PaymentDate) AS MonthName,
    COUNT(p.Id) AS TotalPolicies,
    SUM(p.PaidAmount) AS TotalRevenue,
    AVG(p.PaidAmount) AS AveragePremium
FROM PolicyApplications p
WHERE p.PaymentDate IS NOT NULL
  AND p.PaidAmount IS NOT NULL
  AND p.PaymentDate >= DATEADD(MONTH, -6, GETDATE())
GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate), DATENAME(MONTH, p.PaymentDate)
ORDER BY Year DESC, Month DESC;
GO


-- ============================================================
-- QUERY 18: Top 5 customers by total coverage
-- ============================================================
PRINT '
================================================================================
QUERY 18: Top 5 customers by total coverage amount
================================================================================
';

SELECT TOP 5
    customer.FullName AS CustomerName,
    customer.Email,
    customer.PhoneNumber,
    COUNT(p.Id) AS TotalPolicies,
    SUM(p.TotalCoverageAmount) AS TotalCoverageAmount,
    SUM(p.RemainingCoverageAmount) AS RemainingCoverageAmount,
    SUM(p.TotalApprovedClaimsAmount) AS TotalClaimsApproved
FROM AspNetUsers customer
INNER JOIN PolicyApplications p ON customer.Id = p.UserId
WHERE p.Status = 'Approved'
GROUP BY customer.FullName, customer.Email, customer.PhoneNumber
ORDER BY SUM(p.TotalCoverageAmount) DESC;
GO


-- ================================================================================
-- STEP 7: CLEANUP AND UTILITY QUERIES
-- ================================================================================

PRINT '
================================================================================
                    DATABASE CREATION COMPLETED SUCCESSFULLY!
================================================================================

Summary:
- 13 tables created with proper relationships
- Sample data inserted for testing
- 18+ practical JOIN queries demonstrated
- Foreign keys and indexes configured

Tables Created:
1. AspNetRoles
2. AspNetUsers
3. AspNetUserRoles
4. AspNetUserClaims
5. AspNetUserLogins
6. AspNetUserTokens
7. AspNetRoleClaims
8. Notifications
9. PolicyApplications
10. InsuranceClaims
11. ClaimDocuments
12. Chats
13. ChatMessages

Key Relationships:
- Users can have multiple Policies
- Policies can have multiple Claims
- Claims can have multiple Documents
- Policies can have multiple Chats
- Chats have multiple Messages
- Users receive Notifications

Next Steps:
1. Review the JOIN queries above
2. Test with your own data
3. Modify queries as per requirements
4. Add indexes for performance optimization

================================================================================
';
GO

PRINT 'Script execution completed successfully!';
GO


-- ================================================================================
-- APPENDIX: USEFUL UTILITY QUERIES
-- ================================================================================

-- Count all tables
-- SELECT COUNT(*) AS TotalTables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- List all tables
-- SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;

-- List all foreign keys
-- SELECT 
--     fk.name AS ForeignKeyName,
--     OBJECT_NAME(fk.parent_object_id) AS TableName,
--     COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
--     OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
--     COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
-- FROM sys.foreign_keys fk
-- INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
-- ORDER BY TableName, ForeignKeyName;

-- Get table sizes
-- SELECT 
--     t.NAME AS TableName,
--     p.rows AS RowCounts,
--     SUM(a.total_pages) * 8 AS TotalSpaceKB, 
--     SUM(a.used_pages) * 8 AS UsedSpaceKB
-- FROM sys.tables t
-- INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
-- INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
-- INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
-- WHERE t.is_ms_shipped = 0
-- GROUP BY t.Name, p.Rows
-- ORDER BY TotalSpaceKB DESC;

-- ================================================================================
-- END OF SCRIPT
-- ================================================================================
