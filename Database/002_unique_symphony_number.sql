/*
    002_unique_symphony_number.sql
    Database-level guarantee that an active Symphony Number exists only once.
    Run AFTER 001 and AFTER all duplicates are merged in Admin > Duplicate Projects
    (the index creation fails while duplicates remain; the 001 report lists them).
*/
USE [Project];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM dbo.Project
    WHERE Is_Deleted = 0 AND Symphony_Number IS NOT NULL
    GROUP BY REPLACE(Symphony_Number, '-', '') HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR('Active duplicate Symphony Numbers still exist. Merge them in Admin > Duplicate Projects first.', 16, 1);
    SET NOEXEC ON;
END
GO

IF OBJECT_ID('dbo.vw_Project_ActiveSymphony', 'V') IS NULL
    EXEC('CREATE VIEW dbo.vw_Project_ActiveSymphony WITH SCHEMABINDING AS
          SELECT ID, REPLACE(Symphony_Number, ''-'', '''') AS Symphony_Key
          FROM dbo.Project
          WHERE Is_Deleted = 0 AND Symphony_Number IS NOT NULL');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Project_ActiveSymphony' AND object_id = OBJECT_ID('dbo.vw_Project_ActiveSymphony'))
    CREATE UNIQUE CLUSTERED INDEX UX_Project_ActiveSymphony ON dbo.vw_Project_ActiveSymphony (Symphony_Key);
GO

SET NOEXEC OFF;
GO
