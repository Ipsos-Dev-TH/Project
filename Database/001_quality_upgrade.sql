/*
    001_quality_upgrade.sql
    Run against the [Project] database BEFORE deploying the matching application version.
    Idempotent: safe to run more than once.
*/
USE [Project];
GO

/* ---- Project: soft delete + modification tracking ---- */
IF COL_LENGTH('dbo.Project', 'Is_Deleted') IS NULL
    ALTER TABLE dbo.Project ADD Is_Deleted bit NOT NULL CONSTRAINT DF_Project_Is_Deleted DEFAULT (0);
GO
IF COL_LENGTH('dbo.Project', 'Modified_By') IS NULL
    ALTER TABLE dbo.Project ADD Modified_By nvarchar(255) NULL;
GO
IF COL_LENGTH('dbo.Project', 'Modified_DateTime') IS NULL
    ALTER TABLE dbo.Project ADD Modified_DateTime datetime NULL;
GO

/* Speeds up duplicate checks and search on the normalized Symphony Number */
IF COL_LENGTH('dbo.Project', 'Symphony_Key') IS NULL
    ALTER TABLE dbo.Project ADD Symphony_Key AS REPLACE(Symphony_Number, '-', '') PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Project_Symphony_Key' AND object_id = OBJECT_ID('dbo.Project'))
    CREATE INDEX IX_Project_Symphony_Key ON dbo.Project (Symphony_Key) INCLUDE (Is_Deleted);
GO

/* ---- Lookup rows created by earlier uploads had NULL Status and were hidden from dropdowns ---- */
UPDATE dbo.ClientName      SET [Status] = 1 WHERE [Status] IS NULL;
UPDATE dbo.MarketSector    SET [Status] = 1 WHERE [Status] IS NULL;
UPDATE dbo.Project_Type    SET [Status] = 1 WHERE [Status] IS NULL;
UPDATE dbo.Project_SubType SET [Status] = 1 WHERE [Status] IS NULL;
UPDATE dbo.Product_Type    SET [Status] = 1 WHERE [Status] IS NULL;
GO

/* ---- Users & roles (Admin / Editor / Viewer) ---- */
IF OBJECT_ID('dbo.App_User', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.App_User (
        ID               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_App_User PRIMARY KEY,
        Email            nvarchar(255) NOT NULL CONSTRAINT UQ_App_User_Email UNIQUE,
        [Role]           nvarchar(20)  NOT NULL CONSTRAINT CK_App_User_Role CHECK ([Role] IN ('Admin', 'Editor', 'Viewer')),
        [Status]         bit           NOT NULL CONSTRAINT DF_App_User_Status DEFAULT (1),
        Created_By       nvarchar(255) NULL,
        Created_DateTime datetime      NOT NULL CONSTRAINT DF_App_User_Created DEFAULT (GETDATE()),
        Modified_By      nvarchar(255) NULL,
        Modified_DateTime datetime     NULL
    );
END
GO

/* Seed the first administrator (replace the email, then uncomment). */
-- IF NOT EXISTS (SELECT 1 FROM dbo.App_User WHERE Email = N'firstname.lastname@ipsos.com')
--     INSERT INTO dbo.App_User (Email, [Role], Created_By) VALUES (N'firstname.lastname@ipsos.com', 'Admin', 'migration');
GO

/* ---- Upload history ---- */
IF OBJECT_ID('dbo.Upload_History', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Upload_History (
        ID                int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Upload_History PRIMARY KEY,
        [File_Name]       nvarchar(260) NOT NULL,
        Uploaded_By       nvarchar(255) NOT NULL,
        Uploaded_DateTime datetime      NOT NULL CONSTRAINT DF_Upload_History_Date DEFAULT (GETDATE()),
        Is_Preview        bit           NOT NULL CONSTRAINT DF_Upload_History_Preview DEFAULT (0),
        Total_Rows        int NOT NULL CONSTRAINT DF_Upload_History_Total DEFAULT (0),
        Inserted_Count    int NOT NULL CONSTRAINT DF_Upload_History_Ins DEFAULT (0),
        Updated_Count     int NOT NULL CONSTRAINT DF_Upload_History_Upd DEFAULT (0),
        Skipped_Count     int NOT NULL CONSTRAINT DF_Upload_History_Skip DEFAULT (0),
        Error_Count       int NOT NULL CONSTRAINT DF_Upload_History_Err DEFAULT (0),
        Error_Detail      nvarchar(max) NULL
    );
END
GO

/* ---- Logs: index for the audit viewer and per-project history ---- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Logs_Action_DateTime' AND object_id = OBJECT_ID('dbo.Logs'))
    CREATE INDEX IX_Logs_Action_DateTime ON dbo.Logs (Action_DateTime DESC);
GO

/* ---- Report: current duplicate Symphony Numbers (resolve them in Admin > Duplicate Projects) ---- */
SELECT Symphony_Key, COUNT(*) AS Duplicate_Count, MIN(ID) AS First_ID, MAX(ID) AS Last_ID
FROM dbo.Project
WHERE Is_Deleted = 0
GROUP BY Symphony_Key
HAVING COUNT(*) > 1
ORDER BY Duplicate_Count DESC;
GO
