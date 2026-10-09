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
        // Query to check if an active project exists by Symphony Number (dash-less key)
        public string Select_Project_CheckExist = @"SELECT ID FROM [Project].[dbo].[Project]
            WHERE Symphony_Key = @SymphonyKey AND Is_Deleted = 0 AND ID <> @ExcludeId";

        // Search template - {0} is a whitelisted WHERE condition built by GetSearchQuery
        private const string Select_Project_Search = @"SELECT TOP 1000 [Project].*,
            Project_Type.Project_Type, Project_SubType.SubType,
            ClientName.ClientName, MarketSector.SectorName
            FROM [Project].[dbo].[Project]
            LEFT JOIN Project_Type ON Project_Type.ID = [Project].Project_Type_ID
            LEFT JOIN Project_SubType ON Project_SubType.ID = [Project].Project_SubType_ID
            LEFT JOIN ClientName ON ClientName.ID = [Project].ClientName_ID
            LEFT JOIN MarketSector ON MarketSector.ID = [Project].MarketSector_ID
            WHERE [Project].Is_Deleted = 0 AND ({0})
            ORDER BY [Project].Created_DateTime DESC";

        // Query to get an active project by ID
        public string Select_Project_byIDSelected = @"SELECT [Project].*,
            Project_Type.Project_Type, Project_SubType.SubType,
            ClientName.ClientName, MarketSector.SectorName
            FROM [Project].[dbo].[Project]
            LEFT JOIN ClientName ON ClientName.ID = [Project].ClientName_ID
            LEFT JOIN MarketSector ON MarketSector.ID = [Project].MarketSector_ID
            LEFT JOIN Project_Type ON Project_Type.ID = [Project].Project_Type_ID
            LEFT JOIN Project_SubType ON Project_SubType.ID = [Project].Project_SubType_ID
            WHERE [Project].ID = @IDSelected AND [Project].Is_Deleted = 0";

        // Query to get all active project types
        public string Select_Project_Type_all = "SELECT * FROM [Project].[dbo].[Project_Type] WHERE [Status] = 1 ORDER BY Project_Type ASC";

        // Query to get project subtypes by type ID
        public string Select_Project_SubType_byddl1 = "SELECT * FROM [Project].[dbo].[Project_SubType] WHERE [Status] = 1 AND Project_Type_ID = @ProjectTypeId ORDER BY SubType ASC";

        // Query to get all active product types
        public string Select_Product_Type_all = "SELECT * FROM [Project].[dbo].[Product_Type] WHERE [Status] = 1";

        // Lookup dropdowns
        public string Select_ClientDB_active = "SELECT * FROM [Project].[dbo].[ClientDB] WHERE [Status] = 1 ORDER BY Name ASC";
        public string Select_ClientName_active = "SELECT * FROM [Project].[dbo].[ClientName] WHERE [Status] = 1 ORDER BY ClientName ASC";
        public string Select_MarketSector_active = "SELECT * FROM [Project].[dbo].[MarketSector] WHERE [Status] = 1 ORDER BY SectorName ASC";

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
            Project_Owner = @Project_Owner,
            Modified_By = @Modified_By,
            Modified_DateTime = @Modified_DateTime
            WHERE ID = @IDSelected AND Is_Deleted = 0";

        // Insert new project - returns the new ID
        public string Insert_Project_new = @"INSERT INTO [dbo].[Project] (
            [Symphony_Number], [Project_Number], [Project_Name], [Project_Type_ID],
            [Project_SubType_ID], [Product_Type_ID], [Project_StartDate], [Project_EndDate],
            [ClientDBID], [Status], [Creator], [Created_DateTime],
            ClientName_ID, MarketSector_ID, Used_iField, Project_Owner
        )
        OUTPUT INSERTED.ID
        VALUES (
            @Symphony_Number, @Project_Number, @Project_Name, @Project_Type_ID,
            @Project_SubType_ID, @Product_Type_ID, @Project_StartDate, @Project_EndDate,
            @ClientDBID, @Status, @Creator, @Created_DateTime,
            @ClientName_ID, @MarketSector_ID, @Used_iField, @Project_Owner
        )";

        // Soft delete
        public string SoftDelete_Project_byID = @"UPDATE [dbo].[Project] SET
            Is_Deleted = 1,
            Modified_By = @Modified_By,
            Modified_DateTime = @Modified_DateTime
            WHERE ID = @IDSelected AND Is_Deleted = 0";

        // Per-project history (new format + legacy formats)
        public string Select_Project_History = @"SELECT TOP 500
            CONVERT(varchar(19), Action_DateTime, 120) AS Action_Time,
            Action_By, Task, Data
            FROM [dbo].[Logs]
            WHERE Data LIKE @NewPattern OR Data LIKE @LegacyPattern OR Data = @LegacyInsert
            ORDER BY Action_DateTime DESC";

        // Log row written inside a caller-supplied transaction
        public string Insert_Log = @"INSERT INTO [dbo].[Logs] ([Action_on_Table],[Task],[Data],[Action_By],[Action_DateTime])
            VALUES (@Action_on_Table, @Task, @Data, @Action_By, @Action_DateTime)";

        // Upload history
        public string Insert_Upload_History = @"INSERT INTO [dbo].[Upload_History]
            ([File_Name], Uploaded_By, Uploaded_DateTime, Is_Preview, Total_Rows,
             Inserted_Count, Updated_Count, Skipped_Count, Error_Count, Error_Detail)
            VALUES (@File_Name, @Uploaded_By, @Uploaded_DateTime, @Is_Preview, @Total_Rows,
             @Inserted_Count, @Updated_Count, @Skipped_Count, @Error_Count, @Error_Detail)";

        public string Select_Upload_History_Recent = @"SELECT TOP 10
            CONVERT(varchar(16), Uploaded_DateTime, 120) AS Uploaded_Time,
            [File_Name], Uploaded_By,
            CASE WHEN Is_Preview = 1 THEN 'Preview' ELSE 'Upload' END AS Run_Type,
            Total_Rows, Inserted_Count, Updated_Count, Skipped_Count, Error_Count
            FROM [dbo].[Upload_History]
            ORDER BY Uploaded_DateTime DESC, ID DESC";

        // Whitelist: search option -> fully qualified column (avoids ambiguous [Status]/[ID] in the join)
        private static readonly Dictionary<string, string> SearchColumnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Symphony_Number", "[Project].[Symphony_Number]" },
            { "Project_Number", "[Project].[Project_Number]" },
            { "Project_Name", "[Project].[Project_Name]" },
            { "Project_Type", "[Project_Type].[Project_Type]" },
            { "SubType", "[Project_SubType].[SubType]" },
            { "ClientName", "[ClientName].[ClientName]" },
            { "SectorName", "[MarketSector].[SectorName]" },
            { "Project_Owner", "[Project].[Project_Owner]" },
            { "Status", "[Project].[Status]" }
        };

        /// <summary>
        /// Validates if a column name is allowed for search operations
        /// </summary>
        public static bool IsValidSearchColumn(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) && SearchColumnMap.ContainsKey(columnName);
        }

        /// <summary>
        /// Gets the search query for a whitelisted option.
        /// Parameters: @Pattern (escaped LIKE pattern) and @KeyPattern (escaped, dash-less pattern for Symphony Number).
        /// </summary>
        public string GetSearchQuery(string columnName)
        {
            if (!IsValidSearchColumn(columnName))
            {
                throw new ArgumentException("Invalid column name for search", nameof(columnName));
            }

            string column = SearchColumnMap[columnName];
            string condition;
            if (string.Equals(columnName, "Symphony_Number", StringComparison.OrdinalIgnoreCase))
            {
                condition = column + " LIKE @Pattern OR [Project].[Symphony_Key] LIKE @KeyPattern";
            }
            else
            {
                condition = column + " LIKE @Pattern";
            }
            return string.Format(Select_Project_Search, condition);
        }

        /// <summary>
        /// Escapes LIKE wildcards ([, %, _) using bracket escaping.
        /// </summary>
        public static string EscapeLike(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        /// <summary>
        /// Builds a "contains" LIKE pattern with wildcards escaped.
        /// </summary>
        public static string ContainsPattern(string value)
        {
            return "%" + EscapeLike(value) + "%";
        }
    }
}
