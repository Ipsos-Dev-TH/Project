/*
    003_auth_offline_cache.sql
    Offline credential cache for the AuthenAPI login fallback.
    Needed only if AuthenOfflineWindowHours > 0 in Web.config (default 0 = off).
    Idempotent: safe to run more than once.
*/
USE [Project];
GO

IF OBJECT_ID('dbo.AuthOfflineCache', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthOfflineCache (
        UserKey               nvarchar(256) NOT NULL CONSTRAINT PK_AuthOfflineCache PRIMARY KEY, -- UPN lower-case
        PasswordHash          nvarchar(200) NOT NULL,  -- HMAC(pepper) + PBKDF2-SHA256 600,000 rounds
        ProfileJson           nvarchar(max) NOT NULL,
        LastOnlineVerifiedUtc datetime2     NOT NULL,  -- always SYSUTCDATETIME(), never GETDATE()
        OfflineFailures       int           NOT NULL CONSTRAINT DF_AuthOfflineCache_Failures DEFAULT (0)
    );
END
GO

IF OBJECT_ID('dbo.AuthOfflineAlias', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthOfflineAlias (
        LoginName nvarchar(256) NOT NULL CONSTRAINT PK_AuthOfflineAlias PRIMARY KEY, -- name as typed (trim + lower)
        UserKey   nvarchar(256) NOT NULL
            CONSTRAINT FK_AuthOfflineAlias_Cache REFERENCES dbo.AuthOfflineCache (UserKey) ON DELETE CASCADE
    );
    CREATE INDEX IX_AuthOfflineAlias_UserKey ON dbo.AuthOfflineAlias (UserKey);
END
GO

/*
    Hashes are secrets: only the application's service account may read these tables.
    Replace [app_service_login] with the SQL login used in the connection string, then uncomment.

-- DENY SELECT ON dbo.AuthOfflineCache TO public;
-- DENY SELECT ON dbo.AuthOfflineAlias TO public;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.AuthOfflineCache TO [app_service_login];
-- GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.AuthOfflineAlias TO [app_service_login];
*/

/*
    Daily SQL Agent job (the app also purges on every successful online login):

    DELETE FROM dbo.AuthOfflineCache WHERE LastOnlineVerifiedUtc < DATEADD(hour, -24, SYSUTCDATETIME());

    Offboarding (HR/IT): remove a leaver immediately:

    DELETE FROM dbo.AuthOfflineCache WHERE UserKey = N'firstname.lastname@ipsos.com';
*/
GO
