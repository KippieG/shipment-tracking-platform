using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ShipmentTracking.Infrastructure.Persistence.Migrations;

/// <summary>Initial production schema. Generated-equivalent SQL keeps deployment independent of local tooling.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260820120000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE AspNetRoles (Id nvarchar(450) NOT NULL PRIMARY KEY, Name nvarchar(256) NULL, NormalizedName nvarchar(256) NULL, ConcurrencyStamp nvarchar(max) NULL);
            CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (NormalizedName) WHERE [NormalizedName] IS NOT NULL;
            CREATE TABLE AspNetUsers (Id nvarchar(450) NOT NULL PRIMARY KEY, UserName nvarchar(256) NULL, NormalizedUserName nvarchar(256) NULL, Email nvarchar(256) NULL, NormalizedEmail nvarchar(256) NULL, EmailConfirmed bit NOT NULL, PasswordHash nvarchar(max) NULL, SecurityStamp nvarchar(max) NULL, ConcurrencyStamp nvarchar(max) NULL, PhoneNumber nvarchar(max) NULL, PhoneNumberConfirmed bit NOT NULL, TwoFactorEnabled bit NOT NULL, LockoutEnd datetimeoffset NULL, LockoutEnabled bit NOT NULL, AccessFailedCount int NOT NULL, CreatedAt datetime2 NOT NULL);
            CREATE INDEX EmailIndex ON AspNetUsers (NormalizedEmail);
            CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (NormalizedUserName) WHERE [NormalizedUserName] IS NOT NULL;
            CREATE TABLE AspNetRoleClaims (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, RoleId nvarchar(450) NOT NULL, ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL, CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE);
            CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
            CREATE TABLE AspNetUserClaims (Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, UserId nvarchar(450) NOT NULL, ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL, CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE);
            CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
            CREATE TABLE AspNetUserLogins (LoginProvider nvarchar(450) NOT NULL, ProviderKey nvarchar(450) NOT NULL, ProviderDisplayName nvarchar(max) NULL, UserId nvarchar(450) NOT NULL, CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey), CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE);
            CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
            CREATE TABLE AspNetUserRoles (UserId nvarchar(450) NOT NULL, RoleId nvarchar(450) NOT NULL, CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId), CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE, CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE);
            CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);
            CREATE TABLE AspNetUserTokens (UserId nvarchar(450) NOT NULL, LoginProvider nvarchar(450) NOT NULL, Name nvarchar(450) NOT NULL, Value nvarchar(max) NULL, CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name), CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE);
            CREATE TABLE Shipments (Id uniqueidentifier NOT NULL PRIMARY KEY, TrackingNumber nvarchar(50) NOT NULL, SenderName nvarchar(200) NOT NULL, SenderAddress nvarchar(500) NOT NULL, RecipientName nvarchar(200) NOT NULL, RecipientAddress nvarchar(500) NOT NULL, Description nvarchar(1000) NOT NULL, WeightKg decimal(10,3) NOT NULL, Status nvarchar(50) NOT NULL, IsDeleted bit NOT NULL, CreatedAt datetime2 NOT NULL, UpdatedAt datetime2 NOT NULL, CreatedBy nvarchar(256) NOT NULL);
            CREATE UNIQUE INDEX IX_Shipments_TrackingNumber ON Shipments (TrackingNumber);
            CREATE TABLE ShipmentStatusHistories (Id uniqueidentifier NOT NULL PRIMARY KEY, ShipmentId uniqueidentifier NOT NULL, Status nvarchar(50) NOT NULL, Notes nvarchar(500) NOT NULL, ChangedBy nvarchar(256) NOT NULL, ChangedAt datetime2 NOT NULL, CONSTRAINT FK_History_Shipment FOREIGN KEY (ShipmentId) REFERENCES Shipments(Id) ON DELETE CASCADE);
            CREATE INDEX IX_ShipmentStatusHistories_ShipmentId ON ShipmentStatusHistories (ShipmentId);
            CREATE TABLE Documents (Id uniqueidentifier NOT NULL PRIMARY KEY, ShipmentId uniqueidentifier NOT NULL, FileName nvarchar(255) NOT NULL, ContentType nvarchar(100) NOT NULL, FileSizeBytes bigint NOT NULL, BlobUri nvarchar(1000) NOT NULL, UploadedBy nvarchar(256) NOT NULL, UploadedAt datetime2 NOT NULL, CONSTRAINT FK_Documents_Shipment FOREIGN KEY (ShipmentId) REFERENCES Shipments(Id) ON DELETE NO ACTION);
            CREATE INDEX IX_Documents_ShipmentId ON Documents (ShipmentId);
            CREATE TABLE IdempotencyRecords (Id uniqueidentifier NOT NULL PRIMARY KEY, Scope nvarchar(200) NOT NULL, [Key] nvarchar(128) NOT NULL, RequestHash nvarchar(64) NOT NULL, StatusCode int NOT NULL, ResponseBody nvarchar(max) NOT NULL, CreatedAt datetime2 NOT NULL);
            CREATE UNIQUE INDEX IX_IdempotencyRecords_Scope_Key ON IdempotencyRecords (Scope, [Key]);
            CREATE TABLE OutboxMessages (Id uniqueidentifier NOT NULL PRIMARY KEY, Type nvarchar(200) NOT NULL, Payload nvarchar(max) NOT NULL, OccurredAt datetime2 NOT NULL, ProcessedAt datetime2 NULL, AttemptCount int NOT NULL, LastError nvarchar(2000) NULL);
            CREATE INDEX IX_OutboxMessages_ProcessedAt_OccurredAt ON OutboxMessages (ProcessedAt, OccurredAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE OutboxMessages; DROP TABLE IdempotencyRecords; DROP TABLE Documents; DROP TABLE ShipmentStatusHistories; DROP TABLE Shipments;
        DROP TABLE AspNetUserTokens; DROP TABLE AspNetUserRoles; DROP TABLE AspNetUserLogins; DROP TABLE AspNetUserClaims; DROP TABLE AspNetRoleClaims; DROP TABLE AspNetUsers; DROP TABLE AspNetRoles;
        """);
}
