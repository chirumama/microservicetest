-- ============================================================
-- MicroserviceHub — Full Database Setup + Migration Script
-- Run this against your SQL Server instance.
-- Safe to run on a fresh DB or on top of the original schema.
-- ============================================================

CREATE DATABASE IF NOT EXISTS microservice_hub_db;
GO
USE microservice_hub_db;
GO

-- ================================
-- ROLES
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        Id   INT PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );
END
GO

-- DB role IDs must match AuthService.cs:
--   1 = User, 2 = Admin, 3 = SuperAdmin
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 1)
    INSERT INTO Roles (Id, Name) VALUES (1, 'User');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 2)
    INSERT INTO Roles (Id, Name) VALUES (2, 'Admin');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 3)
    INSERT INTO Roles (Id, Name) VALUES (3, 'SuperAdmin');
GO

-- ================================
-- USERS
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        Email        NVARCHAR(255) UNIQUE NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        RoleId       INT NOT NULL,
        IsActive     BIT DEFAULT 1,
        CreatedAt    DATETIME DEFAULT GETUTCDATE(),
        UpdatedAt    DATETIME NULL,
        FOREIGN KEY (RoleId) REFERENCES Roles(Id)
    );
END
GO

-- ================================
-- APPLICATIONS
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Applications')
BEGIN
    CREATE TABLE Applications (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NOT NULL,
        Title       NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500),
        Status      INT DEFAULT 1,
        CreatedAt   DATETIME DEFAULT GETUTCDATE(),
        UpdatedAt   DATETIME NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
END
GO

-- ================================
-- API KEYS  (column names: AppKey, AppSecretHash)
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE ApiKeys (
        Id                   INT IDENTITY(1,1) PRIMARY KEY,
        ApplicationId        INT NOT NULL,
        Environment          NVARCHAR(50) NOT NULL,   -- 'Development' | 'Pre-Production' | 'Production'
        AppKey               NVARCHAR(200) NOT NULL,
        AppSecretHash        NVARCHAR(500) NOT NULL,
        IsActive             BIT DEFAULT 1,
        IsEnvironmentEnabled BIT DEFAULT 1,           -- controls env toggle in UI
        CreatedAt            DATETIME DEFAULT GETUTCDATE(),
        UpdatedAt            DATETIME NULL,
        FOREIGN KEY (ApplicationId) REFERENCES Applications(Id)
    );
END
ELSE
BEGIN
    -- Migration: add IsEnvironmentEnabled if it doesn't exist yet
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'IsEnvironmentEnabled'
    )
    BEGIN
        ALTER TABLE ApiKeys
        ADD IsEnvironmentEnabled BIT NOT NULL DEFAULT 1;
        PRINT 'Added IsEnvironmentEnabled column to ApiKeys';
    END

    -- Migration: rename ApiSecretHash -> AppSecretHash if the old name exists
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'ApiSecretHash'
    )
    AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'AppSecretHash'
    )
    BEGIN
        EXEC sp_rename 'ApiKeys.ApiSecretHash', 'AppSecretHash', 'COLUMN';
        PRINT 'Renamed ApiSecretHash -> AppSecretHash';
    END

    -- Migration: rename ApiKey -> AppKey if old name exists
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'ApiKey'
    )
    AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'AppKey'
    )
    BEGIN
        EXEC sp_rename 'ApiKeys.ApiKey', 'AppKey', 'COLUMN';
        PRINT 'Renamed ApiKey -> AppKey';
    END
END
GO

-- ================================
-- MICROSERVICES
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Microservices')
BEGIN
    CREATE TABLE Microservices (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500),
        IsActive    BIT DEFAULT 1,
        CreatedAt   DATETIME DEFAULT GETUTCDATE()
    );
END
GO

-- ================================
-- APPLICATION MICROSERVICE ACCESS
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApplicationMicroservices')
BEGIN
    CREATE TABLE ApplicationMicroservices (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        ApplicationId  INT NOT NULL,
        MicroserviceId INT NOT NULL,
        IsEnabled      BIT DEFAULT 1,
        CreatedAt      DATETIME DEFAULT GETUTCDATE(),
        UpdatedAt      DATETIME NULL,
        FOREIGN KEY (ApplicationId)  REFERENCES Applications(Id),
        FOREIGN KEY (MicroserviceId) REFERENCES Microservices(Id)
    );
END
GO

-- ================================
-- APPLICATION ENVIRONMENT SETTINGS
-- (optional helper table — main env state lives in ApiKeys.IsEnvironmentEnabled)
-- ================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApplicationEnvironments')
BEGIN
    CREATE TABLE ApplicationEnvironments (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        ApplicationId INT NOT NULL,
        Environment   NVARCHAR(50) NOT NULL,
        IsEnabled     BIT DEFAULT 1,
        CreatedAt     DATETIME DEFAULT GETUTCDATE(),
        UpdatedAt     DATETIME NULL,
        FOREIGN KEY (ApplicationId) REFERENCES Applications(Id)
    );
END
GO

-- ================================
-- SEED MICROSERVICES
-- ================================
IF NOT EXISTS (SELECT 1 FROM Microservices)
BEGIN
    INSERT INTO Microservices (Name, Description) VALUES
    ('Search Service',       'Search across multiple data sources'),
    ('Media Storage',        'Upload and manage files'),
    ('Reporting Service',    'Generate reports'),
    ('Aadhaar Verification', 'Verify Aadhaar'),
    ('PAN Verification',     'Verify PAN');
END
GO

-- ================================
-- SEED USERS
-- Passwords below are BCrypt hashes for '123'
-- (generated with BCrypt.Net.BCrypt.HashPassword("123"))
-- Replace with your own hashes in production.
-- ================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'user@test.com')
    INSERT INTO Users (Email, PasswordHash, RoleId)
    VALUES ('user@test.com',
            '$2a$11$ePMevmMr6GL6zG4E3yCdAuoMHCbguAoJfNxh1N0LpRBtHflNBzLYK',
            1);   -- Role: User

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@test.com')
    INSERT INTO Users (Email, PasswordHash, RoleId)
    VALUES ('admin@test.com',
            '$2a$11$ePMevmMr6GL6zG4E3yCdAuoMHCbguAoJfNxh1N0LpRBtHflNBzLYK',
            2);   -- Role: Admin

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'superadmin@test.com')
    INSERT INTO Users (Email, PasswordHash, RoleId)
    VALUES ('superadmin@test.com',
            '$2a$11$ePMevmMr6GL6zG4E3yCdAuoMHCbguAoJfNxh1N0LpRBtHflNBzLYK',
            3);   -- Role: SuperAdmin
GO

-- All three test accounts use password: 123
PRINT 'Database setup complete.';
PRINT 'Test credentials: user@test.com / admin@test.com / superadmin@test.com — password: 123';
GO
