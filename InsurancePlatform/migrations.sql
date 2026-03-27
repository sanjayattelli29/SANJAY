IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [AadhaarNumber] nvarchar(12) NULL,
    [BankAccountNumber] nvarchar(16) NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    [NotificationType] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PolicyApplications] (
    [Id] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [PolicyCategory] nvarchar(max) NOT NULL,
    [TierId] nvarchar(max) NOT NULL,
    [CalculatedPremium] decimal(18,2) NOT NULL,
    [SubmissionDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ApplicationDataJson] nvarchar(max) NOT NULL,
    [AssignedAgentId] nvarchar(450) NULL,
    [ApprovedByAgentId] nvarchar(max) NULL,
    [ApprovedAt] datetime2 NULL,
    [TotalCoverageAmount] decimal(18,2) NOT NULL,
    [RemainingCoverageAmount] decimal(18,2) NOT NULL,
    [TotalApprovedClaimsAmount] decimal(18,2) NOT NULL,
    [PaymentMode] nvarchar(max) NULL,
    [NextPaymentDate] datetime2 NULL,
    [StartDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [PaidAmount] decimal(18,2) NULL,
    [PaymentDate] datetime2 NULL,
    [TransactionId] nvarchar(max) NULL,
    CONSTRAINT [PK_PolicyApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PolicyApplications_AspNetUsers_AssignedAgentId] FOREIGN KEY ([AssignedAgentId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_PolicyApplications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Chats] (
    [Id] nvarchar(450) NOT NULL,
    [PolicyId] nvarchar(450) NOT NULL,
    [CustomerId] nvarchar(max) NOT NULL,
    [AgentId] nvarchar(max) NOT NULL,
    [CustomerEmail] nvarchar(max) NULL,
    [AgentEmail] nvarchar(max) NULL,
    [PolicyName] nvarchar(max) NULL,
    [Category] nvarchar(max) NULL,
    [CoverageAmount] decimal(18,2) NOT NULL,
    [DateActivated] datetime2 NOT NULL,
    [LastMessage] nvarchar(max) NULL,
    [LastMessageTime] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Chats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Chats_PolicyApplications_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [PolicyApplications] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InsuranceClaims] (
    [Id] nvarchar(450) NOT NULL,
    [PolicyApplicationId] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [RequestedAmount] decimal(18,2) NOT NULL,
    [ApprovedAmount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IncidentDate] datetime2 NOT NULL,
    [SubmissionDate] datetime2 NOT NULL,
    [IncidentType] nvarchar(max) NOT NULL,
    [IncidentLocation] nvarchar(max) NOT NULL,
    [HospitalName] nvarchar(max) NOT NULL,
    [HospitalizationRequired] bit NOT NULL,
    [AffectedMemberName] nvarchar(max) NULL,
    [AffectedMemberRelation] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [AssignedClaimOfficerId] nvarchar(450) NULL,
    [ApprovedByOfficerId] nvarchar(max) NULL,
    [ProcessedAt] datetime2 NULL,
    CONSTRAINT [PK_InsuranceClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InsuranceClaims_AspNetUsers_AssignedClaimOfficerId] FOREIGN KEY ([AssignedClaimOfficerId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_InsuranceClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InsuranceClaims_PolicyApplications_PolicyApplicationId] FOREIGN KEY ([PolicyApplicationId]) REFERENCES [PolicyApplications] ([Id])
);
GO

CREATE TABLE [ChatMessages] (
    [Id] nvarchar(450) NOT NULL,
    [ChatId] nvarchar(450) NOT NULL,
    [SenderId] nvarchar(max) NOT NULL,
    [SenderRole] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [Chats] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ClaimDocuments] (
    [Id] nvarchar(450) NOT NULL,
    [ClaimId] nvarchar(450) NOT NULL,
    [FileId] nvarchar(max) NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    [FileSize] bigint NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClaimDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClaimDocuments_InsuranceClaims_ClaimId] FOREIGN KEY ([ClaimId]) REFERENCES [InsuranceClaims] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_ChatMessages_ChatId] ON [ChatMessages] ([ChatId]);
GO

CREATE INDEX [IX_Chats_PolicyId] ON [Chats] ([PolicyId]);
GO

CREATE INDEX [IX_ClaimDocuments_ClaimId] ON [ClaimDocuments] ([ClaimId]);
GO

CREATE INDEX [IX_InsuranceClaims_AssignedClaimOfficerId] ON [InsuranceClaims] ([AssignedClaimOfficerId]);
GO

CREATE INDEX [IX_InsuranceClaims_PolicyApplicationId] ON [InsuranceClaims] ([PolicyApplicationId]);
GO

CREATE INDEX [IX_InsuranceClaims_UserId] ON [InsuranceClaims] ([UserId]);
GO

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO

CREATE INDEX [IX_PolicyApplications_AssignedAgentId] ON [PolicyApplications] ([AssignedAgentId]);
GO

CREATE INDEX [IX_PolicyApplications_UserId] ON [PolicyApplications] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260302151801_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260302152436_first', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303084851_AddUserCreatedDate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUsers] ADD [InitialPassword] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303090528_AddUserInitialPassword', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303144727_initital', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PolicyApplications] ADD [Age] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [PolicyApplications] ADD [AlcoholHabit] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [PolicyApplications] ADD [AnnualIncome] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [PolicyApplications] ADD [Profession] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [PolicyApplications] ADD [SmokingHabit] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [PolicyApplications] ADD [TravelKmPerMonth] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [ApplicationDocuments] (
    [Id] nvarchar(450) NOT NULL,
    [PolicyApplicationId] nvarchar(450) NOT NULL,
    [DocumentType] nvarchar(max) NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    [CloudKey] nvarchar(max) NULL,
    [FileSize] bigint NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ApplicationDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApplicationDocuments_PolicyApplications_PolicyApplicationId] FOREIGN KEY ([PolicyApplicationId]) REFERENCES [PolicyApplications] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [FamilyMembers] (
    [Id] nvarchar(450) NOT NULL,
    [PolicyApplicationId] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Relation] nvarchar(max) NOT NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [ExistingHealthConditions] nvarchar(max) NULL,
    CONSTRAINT [PK_FamilyMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FamilyMembers_PolicyApplications_PolicyApplicationId] FOREIGN KEY ([PolicyApplicationId]) REFERENCES [PolicyApplications] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [NomineeDetails] (
    [Id] nvarchar(450) NOT NULL,
    [PolicyApplicationId] nvarchar(450) NOT NULL,
    [NomineeName] nvarchar(max) NOT NULL,
    [Relationship] nvarchar(max) NOT NULL,
    [NomineePhone] nvarchar(max) NOT NULL,
    [NomineeEmail] nvarchar(max) NOT NULL,
    [BankAccountNumber] nvarchar(max) NOT NULL,
    [IFSC] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_NomineeDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NomineeDetails_PolicyApplications_PolicyApplicationId] FOREIGN KEY ([PolicyApplicationId]) REFERENCES [PolicyApplications] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ApplicationDocuments_PolicyApplicationId] ON [ApplicationDocuments] ([PolicyApplicationId]);
GO

CREATE INDEX [IX_FamilyMembers_PolicyApplicationId] ON [FamilyMembers] ([PolicyApplicationId]);
GO

CREATE UNIQUE INDEX [IX_NomineeDetails_PolicyApplicationId] ON [NomineeDetails] ([PolicyApplicationId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260309165403_newpolicybuying1', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310005700_newpolicylocaion', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PolicyApplications] ADD [VehicleType] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310051536_newpolicyvechiletype', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PolicyApplications] ADD [Address] nvarchar(max) NULL;
GO

ALTER TABLE [PolicyApplications] ADD [District] nvarchar(max) NULL;
GO

ALTER TABLE [PolicyApplications] ADD [Latitude] float NULL;
GO

ALTER TABLE [PolicyApplications] ADD [Longitude] float NULL;
GO

ALTER TABLE [PolicyApplications] ADD [Pincode] nvarchar(max) NULL;
GO

ALTER TABLE [PolicyApplications] ADD [State] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310071835_newpolicylocation', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUsers] ADD [IsKycVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310115043_kyc', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310121654_AddKycVerifiedToUser', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [InsuranceClaims] ADD [AccidentCause] nvarchar(max) NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [AdmissionDate] datetime2 NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [BodyPartInjured] nvarchar(max) NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [DischargeDate] datetime2 NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [EstimatedMedicalCost] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [InsuranceClaims] ADD [FirNumber] nvarchar(max) NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [HospitalBill] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [InsuranceClaims] ADD [IncidentTime] time NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [InjuryType] nvarchar(max) NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [Medicines] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [InsuranceClaims] ADD [OtherExpenses] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [InsuranceClaims] ADD [PoliceCaseFiled] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310172340_claimsenahance', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [InsuranceClaims] ADD [Latitude] float NULL;
GO

ALTER TABLE [InsuranceClaims] ADD [Longitude] float NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310175243_claimsenahancea', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312111250_kyc-nominee', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [NomineeDetails] ADD [AadharCardUrl] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [NomineeDetails] ADD [AadharNumber] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312120816_kyc-nominee1', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312121653_kyc-nominee12', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312122714_kyc-nominee124', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AspNetUsers] ADD [ProfileImageUrl] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260313034914_profile', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260313103103_invoice', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PolicyApplications] ADD [InvoiceUrl] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260313111110_invoice1aa', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260315080330_initial', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PolicyApplications] ADD [AnalysisReportUrl] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323101023_analysis', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [InsuranceClaims] ADD [AnalysisReportUrl] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260325071326_claim', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [FamilyMembers] ADD [AadhaarNumber] nvarchar(12) NULL;
GO

ALTER TABLE [FamilyMembers] ADD [AadharCardUrl] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260326140448_AddFamilyMemberAadhar', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260326155236_family', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327064739_policy', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [PolicyCategories] (
    [CategoryId] nvarchar(450) NOT NULL,
    [CategoryName] nvarchar(max) NOT NULL,
    [MaxMembersAllowed] int NOT NULL,
    [PremiumBasedOn] nvarchar(max) NULL,
    CONSTRAINT [PK_PolicyCategories] PRIMARY KEY ([CategoryId])
);
GO

CREATE TABLE [PolicyTiers] (
    [TierId] nvarchar(450) NOT NULL,
    [TierName] nvarchar(max) NOT NULL,
    [CategoryId] nvarchar(450) NOT NULL,
    [BaseCoverageAmount] decimal(18,2) NOT NULL,
    [BasePremiumAmount] decimal(18,2) NOT NULL,
    [ValidityInYears] int NOT NULL,
    [Benefits] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PolicyTiers] PRIMARY KEY ([TierId]),
    CONSTRAINT [FK_PolicyTiers_PolicyCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [PolicyCategories] ([CategoryId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PolicyTiers_CategoryId] ON [PolicyTiers] ([CategoryId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327071155_policy11', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327082418_MigratePolicyToDb', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327082421_policy114', N'8.0.0');
GO

COMMIT;
GO

