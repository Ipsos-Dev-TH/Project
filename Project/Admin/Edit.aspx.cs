using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class Edit : System.Web.UI.Page
    {
        private readonly Core _core = new Core();
        private readonly string _conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        // Whitelist of allowed table names for editing
        private static readonly HashSet<string> AllowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Project_Type", "Project_SubType", "Product_Type", "ClientName", "MarketSector"
        };

        // Session-based properties instead of static variables
        private DataTable CurrentDataTable
        {
            get => Session["Admin_Edit_dt"] as DataTable ?? new DataTable();
            set => Session["Admin_Edit_dt"] = value;
        }

        private DataTable DisplayDataTable
        {
            get => Session["Admin_Edit_dtshow"] as DataTable ?? new DataTable();
            set => Session["Admin_Edit_dtshow"] = value;
        }

        private string CurrentData
        {
            get => Session["Admin_Edit_data"] as string ?? string.Empty;
            set => Session["Admin_Edit_data"] = value;
        }

        private string CurrentBU
        {
            get => Session["Admin_Edit_BU"] as string ?? string.Empty;
            set => Session["Admin_Edit_BU"] = value;
        }

        private string CurrentEmail
        {
            get => Session["Email"] as string ?? string.Empty;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentEmail))
                {
                    Response.Redirect("/Home", false);
                    return;
                }

                string data = Request.QueryString["data"];
                string bu = Request.QueryString["BU"];

                // Validate table name against whitelist
                if (!string.IsNullOrEmpty(data) && !AllowedTables.Contains(data))
                {
                    Response.Redirect("/", false);
                    return;
                }

                CurrentData = data ?? string.Empty;
                CurrentBU = bu ?? string.Empty;

                if (!IsPostBack)
                {
                    ReadData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Page_Load: {ex.Message}");
                Response.Redirect("/Home", false);
            }
        }

        public void ReadData()
        {
            string data = CurrentData;
            DataTable dt;

            if (data == "Project_SubType")
            {
                string typeIdStr = Request.QueryString["TypeID"];
                if (!int.TryParse(typeIdStr, out int typeId))
                {
                    Response.Redirect("/", false);
                    return;
                }

                DataTable dtt = _core.DatabaseQueryWithParams(_conn,
                    "SELECT [Project_Type] FROM Project_Type WHERE [Status] = 'True' AND ID = @TypeId",
                    new SqlParameter("@TypeId", typeId));

                dt = _core.DatabaseQueryWithParams(_conn,
                    "SELECT * FROM Project_SubType WHERE [Status] = 'True' AND Project_Type_ID = @TypeId",
                    new SqlParameter("@TypeId", typeId));

                if (dtt.Rows.Count > 0)
                {
                    string name = dtt.Rows[0][0].ToString();
                    Label1.Text = "Editing Data of " + data + " that Project Type is " + name;
                }
            }
            else if (AllowedTables.Contains(data))
            {
                // Using validated table name from whitelist
                string query = $"SELECT * FROM [{data}] WHERE [Status] = 'True' ORDER BY ID DESC";
                dt = _core.DatabaseQuery(_conn, query);
                Label1.Text = "Editing Data of " + data;
            }
            else
            {
                Response.Redirect("/", false);
                return;
            }

            CurrentDataTable = dt;

            // Create display table without sensitive columns
            DataTable dtshow = new DataTable();
            dtshow.Merge(dt);

            string[] columnsToRemove = { "id", "bu", "typeid", "bu_id", "status", "cost_type_id", "project_type_id" };

            for (int i = dtshow.Columns.Count - 1; i >= 0; i--)
            {
                string colName = dtshow.Columns[i].ColumnName.ToLower();
                if (Array.Exists(columnsToRemove, c => c == colName) ||
                    (colName == "bu" && data.ToLower() != "bu"))
                {
                    dtshow.Columns.RemoveAt(i);
                }
            }

            DisplayDataTable = dtshow;
            GridView1.DataSource = dtshow;
            GridView1.DataBind();
        }

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView1.EditIndex = e.NewEditIndex;
            GridView1.DataSource = DisplayDataTable;
            GridView1.DataBind();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            e.Cancel = true;
            GridView1.EditIndex = -1;
            GridView1.DataSource = DisplayDataTable;
            GridView1.DataBind();
        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            try
            {
                string data = CurrentData;
                DataTable dt = CurrentDataTable;
                DataTable dtshow = DisplayDataTable;

                if (!AllowedTables.Contains(data) || e.RowIndex >= dt.Rows.Count)
                {
                    return;
                }

                GridViewRow row = GridView1.Rows[e.RowIndex];
                long recordId = Convert.ToInt64(dt.Rows[e.RowIndex]["ID"]);

                // Build parameterized update query
                var parameters = new List<SqlParameter>();
                var setClauses = new List<string>();
                string dataChanged = $"ID-{recordId}{{";

                for (int i = 0; i < dtshow.Columns.Count; i++)
                {
                    string columnName = dtshow.Columns[i].ColumnName;
                    string newValue = ((TextBox)(row.Cells[2 + i].Controls[0])).Text;
                    string oldValue = dtshow.Rows[e.RowIndex][columnName].ToString();

                    if (oldValue != newValue)
                    {
                        dataChanged += $"{columnName}[{oldValue},{newValue}];";
                    }

                    string paramName = $"@p{i}";
                    setClauses.Add($"[{columnName}] = {paramName}");
                    parameters.Add(new SqlParameter(paramName, newValue));
                }

                parameters.Add(new SqlParameter("@RecordId", recordId));
                string cmd = $"UPDATE [{data}] SET {string.Join(", ", setClauses)} WHERE ID = @RecordId";

                _core.DatabaseInsertWithParams(_conn, cmd, parameters.ToArray());
                dataChanged += "}";
                _core.Logs(_conn, data, "Update", dataChanged, CurrentEmail);

                GridView1.EditIndex = -1;
                ReadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GridView1_RowUpdating: {ex.Message}");
                throw;
            }
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                string data = CurrentData;
                DataTable dt = CurrentDataTable;

                if (!AllowedTables.Contains(data) || e.RowIndex >= dt.Rows.Count)
                {
                    return;
                }

                long recordId = Convert.ToInt64(dt.Rows[e.RowIndex]["ID"]);

                _core.DatabaseInsertWithParams(_conn,
                    $"UPDATE [{data}] SET [Status] = 'False' WHERE ID = @RecordId",
                    new SqlParameter("@RecordId", recordId));

                _core.Logs(_conn, data, "Update", $"ID-{recordId}{{Status[True,False];}}", CurrentEmail);
                ReadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GridView1_RowDeleting: {ex.Message}");
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                string data = CurrentData;

                if (!AllowedTables.Contains(data))
                {
                    return;
                }

                var parameters = new List<SqlParameter>();
                var columnNames = new List<string>();
                var paramPlaceholders = new List<string>();

                // Handle Project_SubType insert
                if (data == "Project_SubType")
                {
                    if (!int.TryParse(Request.QueryString["TypeID"], out int typeId))
                    {
                        return;
                    }

                    columnNames.Add("[SubType]");
                    paramPlaceholders.Add("@SubType");
                    parameters.Add(new SqlParameter("@SubType", ""));

                    columnNames.Add("[Project_Type_ID]");
                    paramPlaceholders.Add("@TypeId");
                    parameters.Add(new SqlParameter("@TypeId", typeId));
                }
                else
                {
                    // For other tables, insert with default empty value for main column
                    string mainColumn = GetMainColumnName(data);
                    if (!string.IsNullOrEmpty(mainColumn))
                    {
                        columnNames.Add($"[{mainColumn}]");
                        paramPlaceholders.Add("@MainValue");
                        parameters.Add(new SqlParameter("@MainValue", ""));
                    }
                }

                if (columnNames.Count > 0)
                {
                    string cmd = $"INSERT INTO [{data}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramPlaceholders)})";
                    _core.DatabaseInsertWithParams(_conn, cmd, parameters.ToArray());
                    _core.Logs(_conn, data, "Insert", "", CurrentEmail);
                }

                ReadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Button1_Click: {ex.Message}");
                throw;
            }
        }

        private string GetMainColumnName(string tableName)
        {
            switch (tableName)
            {
                case "Project_Type": return "Project_Type";
                case "Product_Type": return "Product_Type";
                case "ClientName": return "ClientName";
                case "MarketSector": return "SectorName";
                default: return null;
            }
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView1.PageIndex = e.NewPageIndex;
            GridView1.DataSource = DisplayDataTable;
            GridView1.DataBind();
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin", false);
        }
    }
}