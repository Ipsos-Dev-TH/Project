using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project
{
    public class SQL
    {
        public string Select_Project_CheckExist = "SELECT * FROM [Project].[dbo].[Project] Where Symphony_Number = '@Symphony_Number'";
        public string Select_Project_byColumn = "SELECT * FROM [Project].[dbo].[Project] inner join Project_Type on Project_Type.ID = Project_Type_ID inner join Project_SubType on Project_SubType.ID = Project_SubType_ID inner join ClientName on ClientName.ID = ClientName_ID inner join MarketSector on MarketSector.ID = MarketSector_ID WHERE @Column LIKE N'%@Value%' order by Created_DateTime DESC";
        public string Select_Project_byIDSelected = "SELECT * FROM [Project].[dbo].[Project] inner join ClientName on ClientName.ID = ClientName_ID inner join MarketSector on MarketSector.ID = MarketSector_ID  inner join Project_Type on Project_Type.ID = Project_Type_ID inner join Project_SubType on Project_SubType.ID = Project_SubType_ID Where [Project].ID = @IDSelected";
        public string Select_Project_Type_all = "SELECT * FROM [Project].[dbo].[Project_Type] Where [Status] = 1  order by Project_Type ASC";
        public string Select_Project_SubType_byddl1 = "SELECT * FROM [Project].[dbo].[Project_SubType] Where [Status] = 1 AND Project_Type_ID = @ddl1 order by SubType ASC";
        public string Select_Product_Type_all = "SELECT * FROM [Project].[dbo].[Product_Type] Where [Status] = 1";


        public string Update_Project_byID = "UPDATE [dbo].[Project] SET [Symphony_Number] = @Symphony_Number,[Project_Number] = @Project_Number,[Project_Name] = @Project_Name,[Project_Type_ID] = @Project_Type_ID,[Project_SubType_ID] = @Project_SubType_ID,[Product_Type_ID] = @Product_Type_ID,[Project_StartDate] = @Project_StartDate,[Project_EndDate] = @Project_EndDate,[Status] = @Status,[ClientDBID] = @ClientDBID, ClientName_ID = @ClientName_ID, MarketSector_ID = @MarketSector_ID, Used_iField = @Used_iField, Project_Owner = @Project_Owner WHERE ID = @IDSelected";
        public string Insert_Project_new = "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Product_Type_ID],[Project_StartDate],[Project_EndDate],[ClientDBID],[Status],[Creator],[Created_DateTime],ClientName_ID,MarketSector_ID,Used_iField,Project_Owner) VALUES (@Symphony_Number,@Project_Number,@Project_Name,@Project_Type_ID,@Project_SubType_ID,@Product_Type_ID,@Project_StartDate,@Project_EndDate,@ClientDBID,@Status,@Creator,@Created_DateTime,@ClientName_ID,@MarketSector_ID,@Used_iField,@Project_Owner)";
    }
}