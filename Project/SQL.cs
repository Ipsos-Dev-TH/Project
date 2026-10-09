using System;
using System.Collections.Generic;

namespace Project
{
    /// <summary>
    /// SQL query templates for Project operations.
    /// Note: Use parameterized queries when executing these - replace placeholders with SqlParameter.
    /// </summary>
    public class SQL
    {
        // Query to check if a project exists by Symphony Number
        public string Select_Project_CheckExist = @"SELECT ID FROM [Project].[dbo].[Project]
            WHERE REPLACE(Symphony_Number, '-', '') = @SymphonyKey AND ID <> @ExcludeId";

        // Query to search projects by column - Note: Column name should be validated against whitelist before use
        public string Select_Project_byColumn = @"SELECT [Project].*,
            Project_Type.Project_Type, Project_SubType.SubType,
            ClientName.ClientName, MarketSector.SectorName
            FROM [Project].[dbo].[Project]
            LEFT JOIN Project_Type ON Project_Type.ID = Project_Type_ID
            LEFT JOIN Project_SubType ON Project_SubType.ID = Project_SubType_ID
            LEFT JOIN ClientName ON ClientName.ID = ClientName_ID
            LEFT JOIN MarketSector ON MarketSector.ID = MarketSector_ID
            WHERE {0} LIKE N'%' + @Value + '%'
            ORDER BY Created_DateTime DESC";

        // Query to get project by ID
        public string Select_Project_byIDSelected = @"SELECT [Project].*,
            Project_Type.Project_Type, Project_SubType.SubType,
            ClientName.ClientName, MarketSector.SectorName
            FROM [Project].[dbo].[Project]
            LEFT JOIN ClientName ON ClientName.ID = ClientName_ID
            LEFT JOIN MarketSector ON MarketSector.ID = MarketSector_ID
            LEFT JOIN Project_Type ON Project_Type.ID = Project_Type_ID
            LEFT JOIN Project_SubType ON Project_SubType.ID = Project_SubType_ID
            WHERE [Project].ID = @IDSelected";

        // Query to get all active project types
        public string Select_Project_Type_all = "SELECT * FROM [Project].[dbo].[Project_Type] WHERE [Status] = 1 ORDER BY Project_Type ASC";

        // Query to get project subtypes by type ID
        public string Select_Project_SubType_byddl1 = "SELECT * FROM [Project].[dbo].[Project_SubType] WHERE [Status] = 1 AND Project_Type_ID = @ProjectTypeId ORDER BY SubType ASC";

        // Query to get all active product types
        public string Select_Product_Type_all = "SELECT * FROM [Project].[dbo].[Product_Type] WHERE [Status] = 1";

        // Update project by ID
        public string Update_Project_byID = @"UPDATE [dbo].[Project] SET
            [Symphony_Number] = @Symphony_Number,
            [Project_Number] = @Project_Number,
            [Project_Name] = @Project_Name,
            [Project_Type_ID] = @Project_Type_ID,
            [Project_SubType_ID] = @Project_SubType_ID,
            [Product_Type_ID] = @Product_Type_ID,
            [Project_StartDate] = @Project_StartDate,
            [Project_EndDate] = @Project_EndDate,
            [ClientDBID] = @ClientDBID,
            ClientName_ID = @ClientName_ID,
            MarketSector_ID = @MarketSector_ID,
            Used_iField = @Used_iField,
            Project_Owner = @Project_Owner
            WHERE ID = @IDSelected";

        // Insert new project
        public string Insert_Project_new = @"INSERT INTO [dbo].[Project] (
            [Symphony_Number], [Project_Number], [Project_Name], [Project_Type_ID],
            [Project_SubType_ID], [Product_Type_ID], [Project_StartDate], [Project_EndDate],
            [ClientDBID], [Status], [Creator], [Created_DateTime],
            ClientName_ID, MarketSector_ID, Used_iField, Project_Owner
        ) VALUES (
            @Symphony_Number, @Project_Number, @Project_Name, @Project_Type_ID,
            @Project_SubType_ID, @Product_Type_ID, @Project_StartDate, @Project_EndDate,
            @ClientDBID, @Status, @Creator, @Created_DateTime,
            @ClientName_ID, @MarketSector_ID, @Used_iField, @Project_Owner
        )";

        // Whitelist of allowed column names for dynamic column queries
        private static readonly HashSet<string> AllowedSearchColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Symphony_Number", "Project_Number", "Project_Name", "Project_Type",
            "SubType", "ClientName", "SectorName", "Project_Owner", "Status"
        };

        /// <summary>
        /// Validates if a column name is allowed for search operations
        /// </summary>
        public static bool IsValidSearchColumn(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) && AllowedSearchColumns.Contains(columnName);
        }

        /// <summary>
        /// Gets the search query with validated column name
        /// </summary>
        public string GetSearchQuery(string columnName)
        {
            if (!IsValidSearchColumn(columnName))
            {
                throw new ArgumentException("Invalid column name for search", nameof(columnName));
            }
            return string.Format(Select_Project_byColumn, $"[{columnName}]");
        }
    }
}