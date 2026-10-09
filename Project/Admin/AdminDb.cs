using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    /// <summary>
    /// Describes one editable master-data table. Table and column names come only from this whitelist.
    /// </summary>
    internal sealed class MasterTable
    {
        public MasterTable(string key, string title, string nameColumn, string projectFk, bool hasParent)
        {
            Key = key;
            Title = title;
            NameColumn = nameColumn;
            ProjectFk = projectFk;
            HasParent = hasParent;
        }

        /// <summary>Database table name (also used in the ?data= query string).</summary>
        public string Key { get; private set; }

        public string Title { get; private set; }

        /// <summary>Column holding the display name.</summary>
        public string NameColumn { get; private set; }

        /// <summary>Foreign key column in dbo.Project that references this table.</summary>
        public string ProjectFk { get; private set; }

        /// <summary>True for Project_SubType (has a parent Project_Type_ID).</summary>
        public bool HasParent { get; private set; }

        private static readonly Dictionary<string, MasterTable> All =
            new Dictionary<string, MasterTable>(StringComparer.OrdinalIgnoreCase)
            {
                { "Project_Type", new MasterTable("Project_Type", "Project Type", "Project_Type", "Project_Type_ID", false) },
                { "Project_SubType", new MasterTable("Project_SubType", "Project SubType", "SubType", "Project_SubType_ID", true) },
                { "Product_Type", new MasterTable("Product_Type", "Product Type", "Product_Type", "Product_Type_ID", false) },
                { "ClientName", new MasterTable("ClientName", "Client Name", "ClientName", "ClientName_ID", false) },
                { "MarketSector", new MasterTable("MarketSector", "Market Sector", "SectorName", "MarketSector_ID", false) },
                { "ClientDB", new MasterTable("ClientDB", "Client DB", "Name", "ClientDBID", false) }
            };

        public static MasterTable Find(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }
            MasterTable table;
            return All.TryGetValue(key.Trim(), out table) ? table : null;
        }
    }

    /// <summary>
    /// Small data-access helper for the Admin pages. Unlike Core.DatabaseQueryWithParams it does not
    /// swallow exceptions, so callers can show a friendly message and roll back transactions.
    /// </summary>
    internal static class AdminDb
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString; }
        }

        public static SqlConnection Open()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        private static void AddParameters(SqlCommand cmd, SqlParameter[] parameters)
        {
            if (parameters == null)
            {
                return;
            }
            foreach (SqlParameter parameter in parameters)
            {
                if (parameter != null)
                {
                    cmd.Parameters.Add(parameter);
                }
            }
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                AddParameters(cmd, parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable QueryTx(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                AddParameters(cmd, parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var conn = Open())
            using (var cmd = new SqlCommand(sql, conn))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static object ScalarTx(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var conn = Open())
            using (var cmd = new SqlCommand(sql, conn))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static int ExecuteTx(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Writes an audit row inside the caller's transaction.</summary>
        public static void LogTx(SqlConnection conn, SqlTransaction tx, string table, string task, string data, string by)
        {
            ExecuteTx(conn, tx,
                "INSERT INTO [dbo].[Logs] ([Action_on_Table],[Task],[Data],[Action_By],[Action_DateTime]) VALUES (@LogTable, @LogTask, @LogData, @LogBy, @LogDate)",
                P("@LogTable", table),
                P("@LogTask", task),
                P("@LogData", Truncate(data, 4000)),
                P("@LogBy", by ?? string.Empty),
                P("@LogDate", DateTime.Now));
        }

        /// <summary>Builds a LIKE pattern that matches the text literally anywhere in the value.</summary>
        public static string LikeContains(string text)
        {
            string escaped = (text ?? string.Empty)
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
            return "%" + escaped + "%";
        }

        public static string Truncate(string value, int max)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return value.Length <= max ? value : value.Substring(0, max);
        }

        public static string ToText(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        /// <summary>kind: success, info, warning, error. The text is HTML-encoded.</summary>
        public static void ShowMessage(Label label, string text, string kind)
        {
            label.Text = HttpUtility.HtmlEncode(text);
            label.CssClass = "ipsos-alert ipsos-alert-" + kind + " admin-message";
            label.Visible = true;
        }

        public static void ShowError(Label label, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Admin error: " + ex);
            ShowMessage(label, FriendlyError(ex), "error");
        }

        public static string FriendlyError(Exception ex)
        {
            var sqlEx = ex as SqlException;
            if (sqlEx != null)
            {
                switch (sqlEx.Number)
                {
                    case 2601:
                    case 2627:
                        return "บันทึกไม่สำเร็จ: ข้อมูลซ้ำกับรายการที่มีอยู่แล้ว (duplicate value).";
                    case 8152:
                    case 2628:
                        return "บันทึกไม่สำเร็จ: ข้อความยาวเกินกว่าที่ฐานข้อมูลรองรับ (value too long).";
                    case 547:
                        return "บันทึกไม่สำเร็จ: ข้อมูลถูกอ้างอิงหรือขัดกับข้อกำหนดของฐานข้อมูล (constraint violation).";
                    case 207:
                    case 208:
                        return "Database schema is out of date. Please run Database/001_quality_upgrade.sql. (" + sqlEx.Message + ")";
                    case 1205:
                        return "The database was busy (deadlock). Please try again.";
                    case -2:
                        return "The database did not respond in time. Please try again.";
                }
                return "Database error: " + sqlEx.Message;
            }
            return "เกิดข้อผิดพลาด: " + ex.Message;
        }
    }
}
