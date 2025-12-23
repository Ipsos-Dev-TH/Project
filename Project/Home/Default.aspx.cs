using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using ExcelDataReader;
using HtmlAgilityPack;

namespace Project
{
    public partial class Default : System.Web.UI.Page
    {
        private readonly SQL _sql = new SQL();
        private readonly Core _core = new Core();
        private readonly string _conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        // Allowed file extensions for upload
        private static readonly HashSet<string> AllowedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx", ".xls", ".html", ".htm"
        };

        private string CurrentEmail => Session["Email"] as string ?? string.Empty;
        protected void Page_Load(object sender, EventArgs e)
        {
            string command = Request.QueryString["Command"];
            string id = Request.QueryString["ID"];

            if (!IsPostBack)
            {
                LoadDropdownData();
                LoadProjectData(command, id);
            }
        }

        private void LoadDropdownData()
        {
            // Project Type dropdown
            DataTable dttype = _core.DatabaseQuery(_conn, _sql.Select_Project_Type_all);
            AddDefaultRow(dttype, "Project_Type", "---Please Choose---");
            BindDropdown(DropDownList1, dttype, "Project_Type", "ID");

            // Product Type dropdown
            DataTable dtproducttype = _core.DatabaseQuery(_conn, _sql.Select_Product_Type_all);
            AddDefaultRow(dtproducttype, "Product_Type", "---Please Choose---");
            BindDropdown(DropDownList16, dtproducttype, "Product_Type", "ID");

            // Client DB dropdown
            DataTable dtclientdb = _core.DatabaseQuery(_conn, "SELECT * FROM ClientDB WHERE Status = 'True'");
            AddDefaultRow(dtclientdb, "Name", "---Please Choose---");
            BindDropdown(DropDownList17, dtclientdb, "Name", "ID");

            // Client Name dropdown
            DataTable dtclientname = _core.DatabaseQuery(_conn, "SELECT * FROM [ClientName] WHERE [Status] = 1 ORDER BY ClientName ASC");
            AddDefaultRow(dtclientname, "ClientName", "---Please Choose---");
            BindDropdown(DropDownList18, dtclientname, "ClientName", "ID");

            // Market Sector dropdown
            DataTable dtmarketsector = _core.DatabaseQuery(_conn, "SELECT * FROM [MarketSector] WHERE [Status] = 1 ORDER BY SectorName ASC");
            AddDefaultRow(dtmarketsector, "SectorName", "---Please Choose---");
            BindDropdown(DropDownList19, dtmarketsector, "SectorName", "ID");
        }

        private void AddDefaultRow(DataTable dt, string textColumn, string defaultText)
        {
            if (dt.Columns.Contains(textColumn) && dt.Columns.Contains("ID"))
            {
                DataRow dr = dt.NewRow();
                dr[textColumn] = defaultText;
                dr["ID"] = 0;
                dt.Rows.InsertAt(dr, 0);
            }
        }

        private void BindDropdown(DropDownList ddl, DataTable dt, string textField, string valueField)
        {
            ddl.DataSource = dt;
            ddl.DataTextField = textField;
            ddl.DataValueField = valueField;
            ddl.DataBind();
        }

        private void LoadProjectData(string command, string id)
        {
            if (command == "View" || command == "Edit")
            {
                if (!int.TryParse(id, out int projectId))
                {
                    return;
                }

                Button7.Enabled = false;
                Button8.Enabled = true;

                DataTable dtData = _core.DatabaseQueryWithParams(_conn,
                    _sql.Select_Project_byIDSelected,
                    new SqlParameter("@IDSelected", projectId));

                if (dtData.Rows.Count == 0) return;

                Session["dtData"] = dtData;
                DataRow row = dtData.Rows[0];

                TextBox1.Text = row["Symphony_Number"]?.ToString() ?? "";
                TextBox2.Text = row["Project_Number"]?.ToString() ?? "";
                TextBox3.Text = row["Project_Name"]?.ToString() ?? "";
                TextBox22.Text = row["Project_Owner"]?.ToString() ?? "";

                // Display Status (read-only)
                string status = row["Status"]?.ToString() ?? "Unknown";
                LblProjectStatus.Text = status;
                LblProjectStatus.CssClass = "status-badge-lg status-" + status.ToLower().Replace(" ", "-");

                // Parse dates safely
                if (row["Project_StartDate"] != DBNull.Value && DateTime.TryParse(row["Project_StartDate"].ToString(), out DateTime stDate))
                {
                    datepicker1.Text = stDate.ToString("yyyy-MM-dd");
                }

                if (row["Project_EndDate"] != DBNull.Value && DateTime.TryParse(row["Project_EndDate"].ToString(), out DateTime edDate))
                {
                    datepicker2.Text = edDate.ToString("yyyy-MM-dd");
                }

                // Set dropdown selections safely
                TrySelectDropdownValue(DropDownList1, row["Project_Type_ID"]?.ToString());
                TrySelectDropdownValue(DropDownList16, row["Product_Type_ID"]?.ToString());
                TrySelectDropdownText(DropDownList18, row["ClientName"]?.ToString());
                TrySelectDropdownText(DropDownList19, row["SectorName"]?.ToString());
                TrySelectDropdownValue(DropDownList17, row["ClientDBID"]?.ToString());

                if (bool.TryParse(row["Used_iField"]?.ToString(), out bool usediField))
                {
                    CheckBox1.Checked = usediField;
                }

                // Load project subtypes
                LoadProjectSubTypes(row["Project_SubType_ID"]?.ToString());

                if (command == "Edit")
                {
                    EnableControl();
                }
            }
        }

        private void TrySelectDropdownValue(DropDownList ddl, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                ddl.ClearSelection();
                var item = ddl.Items.FindByValue(value);
                if (item != null)
                {
                    item.Selected = true;
                }
            }
            catch { }
        }

        private void TrySelectDropdownText(DropDownList ddl, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                ddl.ClearSelection();
                var item = ddl.Items.FindByText(text);
                if (item != null)
                {
                    item.Selected = true;
                }
            }
            catch { }
        }

        private void LoadProjectSubTypes(string selectedSubTypeId)
        {
            if (!int.TryParse(DropDownList1.SelectedValue, out int typeId) || typeId <= 0)
            {
                return;
            }

            DataTable dtsub = _core.DatabaseQueryWithParams(_conn,
                _sql.Select_Project_SubType_byddl1,
                new SqlParameter("@ProjectTypeId", typeId));

            AddDefaultRow(dtsub, "SubType", "---Please Choose---");
            BindDropdown(DropDownList2, dtsub, "SubType", "ID");

            TrySelectDropdownValue(DropDownList2, selectedSubTypeId);
        }

        public void EnableControl()
        {
            TextBox1.Enabled = true;
            //TextBox2.Enabled = true;
            TextBox3.Enabled = true;
            TextBox21.Enabled = true;
            TextBox22.Enabled = true;
            //TextBox23.Enabled = true;
            datepicker1.Enabled = true;
            datepicker2.Enabled = true;
            DropDownList1.Enabled = true;
            DropDownList2.Enabled = true;
            DropDownList16.Enabled = true;
            DropDownList15.Enabled = true;
            DropDownList17.Enabled = true;
            DropDownList18.Enabled = true;
            DropDownList19.Enabled = true;
            Button2.Enabled = true;


        }

        [ScriptMethod]
        [WebMethod]
        public static List<string> GetCompletionList(string prefixText, int count)
        {
            Core core = new Core();
            string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

            if (string.IsNullOrWhiteSpace(prefixText))
            {
                return new List<string>();
            }

            if (count <= 0 || count > 100)
            {
                count = 20;
            }

            DataTable dt = core.DatabaseQueryWithParams(conn,
                "SELECT TOP (@Count) [ClientName] FROM ClientName WHERE Status = 1 AND ClientName LIKE @Pattern ORDER BY ClientName",
                new SqlParameter("@Count", count),
                new SqlParameter("@Pattern", "%" + prefixText + "%"));

            List<string> names = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                if (row[0] != null)
                {
                    names.Add(row[0].ToString());
                }
            }
            return names;
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            string Command = Request.QueryString["Command"];
            string ID = Request.QueryString["ID"];

            string query = "";
            if (TextBox1.Text != string.Empty && DropDownList1.SelectedIndex > 0 && DropDownList2.SelectedIndex > 0 && DropDownList17.SelectedIndex > 0 && DropDownList18.SelectedIndex > 0 && DropDownList19.SelectedIndex > 0 && TextBox22.Text.Length > 6)
            {
                if (Command == "Edit")
                {
                    query = _sql.Update_Project_byID.Replace("@IDSelected", ID);
                }
                else
                {
                    DataTable dtcheck = _core.DatabaseQuery(_conn, _sql.Select_Project_CheckExist.Replace("@Symphony_Number", TextBox1.Text));
                    if (dtcheck.Rows.Count > 0)
                    {
                        ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('เลขโปรเจกต์ซ้ำหรือไม่ถูกต้อง');", true);
                    }
                    else
                    {
                        query = _sql.Insert_Project_new;

                    }
                }
                try
                {
                    using (SqlConnection con = new SqlConnection(_conn))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Connection = con;
                            cmd.Parameters.AddWithValue("@Symphony_Number", TextBox1.Text);
                            cmd.Parameters.AddWithValue("@Project_Number", TextBox2.Text);
                            cmd.Parameters.AddWithValue("@Project_Name", TextBox3.Text);
                            cmd.Parameters.AddWithValue("@Project_Type_ID", DropDownList1.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Project_SubType_ID", DropDownList2.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Product_Type_ID", DropDownList16.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Project_StartDate", string.IsNullOrWhiteSpace(datepicker1.Text) ? DBNull.Value : (object)datepicker1.Text);
                            cmd.Parameters.AddWithValue("@Project_EndDate", string.IsNullOrWhiteSpace(datepicker2.Text) ? DBNull.Value : (object)datepicker2.Text);
                            cmd.Parameters.AddWithValue("@ClientDBID", DropDownList17.SelectedValue);
                            cmd.Parameters.AddWithValue("@ClientName_ID", DropDownList18.SelectedValue);
                            cmd.Parameters.AddWithValue("@MarketSector_ID", DropDownList19.SelectedValue);
                            cmd.Parameters.AddWithValue("@Used_iField", CheckBox1.Checked);
                            cmd.Parameters.AddWithValue("@Project_Owner", TextBox22.Text);

                            cmd.Parameters.AddWithValue("@Status", "NEW");
                            if (Command == "Edit")
                            {

                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@Creator", Email);
                                cmd.Parameters.AddWithValue("@Created_DateTime", DateTime.Now);
                            }
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();


                            if (Command == "Edit")
                            {
                                DataTable dtData = (DataTable)Session["dtData"];
                                string DataChanged = "ID-" + dtData.Rows[0]["ID"].ToString() + "{";
                                if (dtData.Rows[0]["Symphony_Number"].ToString() != TextBox1.Text)
                                {
                                    DataChanged += "Symphony_Number of ID[" + dtData.Rows[0]["Symphony_Number"].ToString() + "," + TextBox1.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Number"].ToString() != TextBox2.Text)
                                {
                                    DataChanged += "Project_Number[" + dtData.Rows[0]["Project_Number"].ToString() + "," + TextBox2.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Name"].ToString() != TextBox3.Text)
                                {
                                    DataChanged += "Project_Name[" + dtData.Rows[0]["Project_Name"].ToString() + "," + TextBox3.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Type_ID"].ToString() != DropDownList1.SelectedItem.Value)
                                {
                                    DataChanged += "Project_Type_ID[" + dtData.Rows[0]["Project_Type_ID"].ToString() + "," + DropDownList1.SelectedItem.Value + "];";
                                }
                                if (dtData.Rows[0]["Project_SubType_ID"].ToString() != DropDownList2.SelectedItem.Value)
                                {
                                    DataChanged += "Project_SubType_ID[" + dtData.Rows[0]["Project_SubType_ID"].ToString() + "," + DropDownList2.SelectedItem.Value + "];";
                                }
                                if (dtData.Rows[0]["Product_Type_ID"].ToString() != DropDownList16.SelectedItem.Value)
                                {
                                    DataChanged += "Product_Type_ID[" + dtData.Rows[0]["Product_Type_ID"].ToString() + "," + DropDownList16.SelectedItem.Value + "];";
                                }
                                if (((DateTime)dtData.Rows[0]["Project_StartDate"]).ToString("yyyy-MM-dd") != datepicker1.Text)
                                {
                                    DataChanged += "Project_StartDate[" + dtData.Rows[0]["Project_StartDate"].ToString() + "," + datepicker1.Text + "];";
                                }
                                if (((DateTime)dtData.Rows[0]["Project_EndDate"]).ToString("yyyy-MM-dd") != datepicker2.Text)
                                {
                                    DataChanged += "Project_EndDate[" + dtData.Rows[0]["Project_EndDate"].ToString() + "," + datepicker2.Text + "];";
                                }
                                DataChanged += "}";
                                _core.Logs(_conn, "Project", "Update", DataChanged, Session["Email"].ToString());
                            }
                            else
                            {
                                _core.Logs(_conn, "Project", "Insert", "Insert Symphony_Number = " + TextBox1.Text, Session["Email"].ToString());
                            }
                        }
                    }





                }
                catch { ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คุณกรอกข้อมูลไม่ครบถ้วนตามที่ระบบต้องการ');", true); }
                string mainpath = "";
                if (ConfigurationManager.AppSettings["DefaultURL"].ToString() == "" || ConfigurationManager.AppSettings["DefaultURL"].ToString() == string.Empty)
                {
                }
                else
                {
                    mainpath = ConfigurationManager.AppSettings["DefaultURL"].ToString();
                }
                Response.Redirect("/Home");
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คุณกรอกข้อมูลไม่ครบถ้วนตามที่ระบบต้องการ');", true);
            }
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable dt = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[Project_SubType] Where [Status] = 1 AND Project_Type_ID = " + DropDownList1.SelectedItem.Value + " order by SubType ASC");
            DataRow dr = dt.NewRow();
            dr["SubType"] = "---Please Choose---";
            dr["ID"] = 0;
            dt.Rows.InsertAt(dr, 0);
            DropDownList2.DataSource = dt;
            DropDownList2.DataTextField = "SubType";
            DropDownList2.DataValueField = "ID";
            DropDownList2.DataBind();
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            dtCost = (DataTable)Session["dtCost"];
            DataTable dtSearch = _core.DatabaseQuery(_conn, "SELECT * FROM [Payment].[dbo].[Payment] Where Project_Cost_ID = " + dtCost.Rows[e.RowIndex]["ID"].ToString() + " AND (Status = 'NEW' OR Status = 'EDITED' OR Status = 'APPROVED')");
            if (dtSearch.Rows.Count > 0)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('Project Cost นี้มีการทำจ่ายแล้วไม่สามารถลบได้');", true);
            }
            else
            {
                _core.DatabaseInsert(_conn, "DELETE FROM [dbo].[Project_Cost] WHERE ID = " + dtCost.Rows[e.RowIndex]["ID"].ToString());
                dtCost.Rows[e.RowIndex].Delete();
                dtCost.AcceptChanges();

                Session["dtCost"] = dtCost;
            }

        }

        protected void Button6_Click(object sender, EventArgs e)
        {
            DataTable dtSearch = _core.DatabaseQuery(_conn, _sql.Select_Project_byColumn.Replace("@Column", DropDownList15.SelectedValue).Replace("@Value", TextBox21.Text));
            GridView2.DataSource = dtSearch;
            GridView2.DataBind();
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            DataTable dtSearch = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[Project] inner join Project_Type on Project_Type.ID = Project_Type_ID inner join Project_SubType on Project_SubType.ID = Project_SubType_ID WHERE " + DropDownList15.SelectedItem.Value + " LIKE N'%" + TextBox21.Text + "%'");
            GridView2.DataSource = dtSearch;
            GridView2.PageIndex = e.NewPageIndex;
            GridView2.DataBind();
        }

        protected void Button7_Click(object sender, EventArgs e)
        {
            EnableControl();
            Panel1.Visible = false;
        }

        protected void Button8_Click(object sender, EventArgs e)
        {
            string mainpath = "";
            if (ConfigurationManager.AppSettings["DefaultURL"].ToString() == "" || ConfigurationManager.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = ConfigurationManager.AppSettings["DefaultURL"].ToString();
            }
            Panel1.Visible = false;
            string ID = Request.QueryString["ID"];
            Response.Redirect("/Home/Default.aspx?Command=Edit&ID=" + ID);
        }

        protected void GridView2_SelectedIndexChanging(object sender, GridViewSelectEventArgs e)
        {
            string mainpath = "";
            if (ConfigurationManager.AppSettings["DefaultURL"].ToString() == "" || ConfigurationManager.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = ConfigurationManager.AppSettings["DefaultURL"].ToString();
            }
            Response.Redirect("/Home/Default.aspx?Command=View&ID=" + GridView2.Rows[e.NewSelectedIndex].Cells[1].Text);
        }



        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox1.Text = ChangeSymNumber(TextBox1.Text);
        }

        public string ChangeSymNumber(string SymNumber)
        {
            if (SymNumber.Length > 0)
            {
                SymNumber = SymNumber.Replace("-", "");
                if (SymNumber.Length >= 8)
                {
                    if (SymNumber.Length == 8)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                    }
                    else if (SymNumber.Length == 10)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                    }
                    else if (SymNumber.Length == 12)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                    }
                    else if (SymNumber.Length == 14)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                        SymNumber = SymNumber.Insert(15, "-");
                    }
                    else if (SymNumber.Length == 16)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                        SymNumber = SymNumber.Insert(15, "-");
                        SymNumber = SymNumber.Insert(18, "-");
                    }
                    else
                    {
                        SymNumber = string.Empty;
                    }
                }
                else
                {
                    SymNumber = string.Empty;
                }

            }
            return SymNumber;
        }

        public DataTable FindChangedFrom2Datatable(DataTable OldData, DataTable NewData)
        {

            DataTable ChangedTable = new DataTable();
            ChangedTable.Columns.Add("ColumnName");
            ChangedTable.Columns.Add("OldValue");
            ChangedTable.Columns.Add("NewValue");
            for (int j = 0; j < OldData.Columns.Count; j++)
            {
                for (int k = 0; k < NewData.Columns.Count; k++)
                {
                    if (OldData.Columns[j].ColumnName == NewData.Columns[k].ColumnName)
                    {
                        if (OldData.Rows[0][j].ToString() == NewData.Rows[0][k].ToString())
                        {

                        }
                        else
                        {
                            ChangedTable.Rows.Add(OldData.Columns[j].ColumnName, OldData.Rows[0][j], NewData.Rows[0][k]);
                        }
                    }
                    else
                    {

                    }

                }
            }



            return ChangedTable;
        }

        protected void Button9_Click(object sender, EventArgs e)
        {

        }

        protected void Button10_Click(object sender, EventArgs e)
        {

        }

        protected void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        protected void Button11_Click(object sender, EventArgs e)
        {
            string mainpath = "";
            if (ConfigurationManager.AppSettings["DefaultURL"].ToString() == "" || ConfigurationManager.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = ConfigurationManager.AppSettings["DefaultURL"].ToString();
            }
            Response.Redirect("/Project/Import-FW");
        }

        protected void TextBox21_TextChanged(object sender, EventArgs e)
        {
            Button6_Click(null, null);
        }

        
        protected void Button9_Click1(object sender, EventArgs e)
        {
            if (!FileUpload1.HasFile)
            {
                LblStatus.ForeColor = System.Drawing.Color.Red;
                LblStatus.Text = "Please select a file to upload.";
                return;
            }

            try
            {
                // Validate file extension
                string originalFileName = Path.GetFileName(FileUpload1.FileName);
                string fileExtension = Path.GetExtension(originalFileName);

                if (!AllowedFileExtensions.Contains(fileExtension))
                {
                    LblStatus.ForeColor = System.Drawing.Color.Red;
                    LblStatus.Text = "Invalid file type. Only Excel (.xlsx, .xls) and HTML files are allowed.";
                    return;
                }

                // Generate safe filename to prevent path traversal
                string safeFileName = $"{Guid.NewGuid()}{fileExtension}";
                string uploadDir = Server.MapPath("~/Upload/");

                // Ensure upload directory exists
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = Path.Combine(uploadDir, safeFileName);

                // Verify the path is still within upload directory
                string fullPath = Path.GetFullPath(filePath);
                if (!fullPath.StartsWith(Path.GetFullPath(uploadDir), StringComparison.OrdinalIgnoreCase))
                {
                    LblStatus.ForeColor = System.Drawing.Color.Red;
                    LblStatus.Text = "Invalid file path.";
                    return;
                }

                FileUpload1.SaveAs(filePath);

                // Read and process file
                DataTable dt;
                try
                {
                    dt = ReadExcelFile(filePath);
                }
                catch
                {
                    string htmlContent = File.ReadAllText(filePath);
                    dt = HtmlToDataTable(htmlContent);
                }

                InsertDataToDatabase(dt);

                // Clean up uploaded file after processing
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch { }

                // Show detailed upload results
                string resultMessage = $"<div class='upload-result success'><strong><i class='fas fa-check-circle'></i> Upload Complete!</strong><br/>" +
                    $"<i class='fas fa-plus-circle'></i> New projects added: <strong>{_insertedCount}</strong><br/>" +
                    $"<i class='fas fa-sync-alt'></i> Projects updated: <strong>{_updatedCount}</strong>";

                if (_skippedCount > 0)
                {
                    resultMessage += $"<br/><i class='fas fa-exclamation-triangle' style='color: #ef6c00;'></i> Skipped (older data): <strong>{_skippedCount}</strong>";
                    if (_skippedProjects.Count > 0 && _skippedProjects.Count <= 10)
                    {
                        resultMessage += $"<br/><small>Skipped projects: {string.Join(", ", _skippedProjects)}</small>";
                    }
                    else if (_skippedProjects.Count > 10)
                    {
                        resultMessage += $"<br/><small>Skipped projects: {string.Join(", ", _skippedProjects.Take(10))}... and {_skippedProjects.Count - 10} more</small>";
                    }
                }
                resultMessage += "</div>";

                LblStatus.ForeColor = System.Drawing.Color.Green;
                LblStatus.Text = resultMessage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in file upload: {ex.Message}");
                LblStatus.ForeColor = System.Drawing.Color.Red;
                LblStatus.Text = "Error processing file. Please try again.";
            }
        }

        private DataTable ReadExcelFile(string filePath)
        {
                using (var stream = FileUpload1.FileContent)
                {
                    DataTable dt = ReadExcelFile(stream);
                    InsertDataToDatabase(dt); // your method
                    return dt;
                }
            
           

        }

        public DataTable ReadExcelFile(Stream stream)
        {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataReader.ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true,
                            ReadHeaderRow = rowReader =>
                            {
                                // Example: skip first 4 rows so headers come from row 5
                                for (int i = 0; i < 6; i++)
                                    rowReader.Read();
                            }
                        }
                    });

                    DataTable dt = result.Tables[0];
                    InsertDataToDatabase(dt);
                    return dt;
                }
           
        }

        // Counter for tracking upload results
        private int _updatedCount = 0;
        private int _insertedCount = 0;
        private int _skippedCount = 0;
        private List<string> _skippedProjects = new List<string>();

        private void InsertDataToDatabase(DataTable dataTable)
        {
            string connectionString = _conn;
            _updatedCount = 0;
            _insertedCount = 0;
            _skippedCount = 0;
            _skippedProjects = new List<string>();

            using (SqlConnection conm = new SqlConnection(connectionString))
            {
                conm.Open();

                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // Adjust column names to match your Excel and DB schema
                        string Symphony_Number = row["Job ID"].ToString();
                        string Project_Name = row["Record Detail"].ToString().Replace(Symphony_Number + " - ", "");
                        string Status = row["JobBook Status"].ToString();
                        string ClientID = "0";
                        string ClientName = row["Billing Client Name"].ToString();
                        string Project_Owner = "";
                        try {
                        Project_Owner = row["Job Owner"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Owner"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                        }
                        catch { }
                        if (Project_Owner.Length == 0)
                        {
                            try {
                                Project_Owner = row["Job Manager"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Manager"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                            }
                            catch { }
                        }
                        if (Project_Owner.Length == 0)
                        {
                            try {
                                Project_Owner = row["Job Owner"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Owner"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                            }
                            catch { }
                        }
                        string MarketSectorID = "0";
                        string MarketSector = row["Client Sector"].ToString();
                        string ProjectTypeID = "0";
                        string Project_Type = row["DC Technique Group"].ToString();
                        string SubProjectTypeID = "0";
                        string SubProject_Type = row["DC Mode"].ToString();
                        string StartDate = row["Job Start Date"].ToString();
                        string EndDate = row["Job End Date"].ToString();

                        DataTable dtClient = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[ClientName] Where ClientName like '" + ClientName.Replace("'", "''").Replace("&amp;", "&") + "'");
                        if (dtClient.Rows.Count > 0)
                        {
                            ClientID = dtClient.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ClientID = Core.ExecuteScalar(_conn, "INSERT INTO [Project].[dbo].[ClientName] ([ClientName]) VALUES ('" + ClientName.Replace("'", "''").Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtMarketSector = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[MarketSector] Where SectorName like '" + MarketSector.Replace("&amp;", "&") + "'");
                        if (dtMarketSector.Rows.Count > 0)
                        {
                            MarketSectorID = dtMarketSector.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            MarketSectorID = Core.ExecuteScalar(_conn, "INSERT INTO [Project].[dbo].[MarketSector] ([SectorName]) VALUES ('" + MarketSector.Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtSubProjectType = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[Project_SubType] Where SubType like '" + SubProject_Type.Replace("&amp;", "&") + "'");
                        if (dtSubProjectType.Rows.Count > 0)
                        {
                            ProjectTypeID = dtSubProjectType.Rows[0]["Project_Type_ID"].ToString();
                            SubProjectTypeID = dtSubProjectType.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ProjectTypeID = Core.ExecuteScalar(_conn, "INSERT INTO [Project].[dbo].[Project_Type] ([Project_Type]) VALUES ('" + Project_Type.Replace("&amp;", "&") + "');").ToString();
                            SubProjectTypeID = Core.ExecuteScalar(_conn, "INSERT INTO [Project].[dbo].[Project_SubType] ([Project_Type_ID],[SubType]) VALUES (" + ProjectTypeID + ",'" + SubProject_Type.Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtProjectType = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[Project_Type] Where Project_Type like '" + Project_Type.Replace("&amp;", "&") + "'");
                        if (dtProjectType.Rows.Count > 0)
                        {
                            ProjectTypeID = dtProjectType.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ProjectTypeID = Core.ExecuteScalar(_conn, "INSERT INTO [Project].[dbo].[Project_Type] ([Project_Type]) VALUES ('" + Project_Type + "');").ToString();
                        }


                        DataTable dtProject = _core.DatabaseQuery(_conn, "SELECT * FROM [Project].[dbo].[Project] Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");

                        string ProjectStartDate = "";
                        string ProjectEndDate = "";
                        bool a = true;
                        bool b = true;
                        try
                        {
                            ProjectStartDate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            a = false;
                        }

                        try
                        {
                            ProjectEndDate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            b = false;
                        }
                        DateTime parsedDate;
                        if (DateTime.TryParseExact(EndDate, "yyyy-MM-dd",
    CultureInfo.InvariantCulture,
    DateTimeStyles.None,
    out parsedDate))
                        {
                            ProjectEndDate = parsedDate.ToString("yyyy-MM-dd");
                            b = true;
                        }
                        else
                        {
                            b = false;
                        }
                        try
                        {
                            // Check if project exists and compare dates
                            if (dtProject.Rows.Count > 0)
                            {
                                // Get existing project's end date from database
                                DateTime? existingEndDate = null;
                                if (dtProject.Rows[0]["Project_EndDate"] != DBNull.Value)
                                {
                                    existingEndDate = Convert.ToDateTime(dtProject.Rows[0]["Project_EndDate"]);
                                }

                                // Parse new end date from file
                                DateTime? newEndDate = null;
                                if (b && !string.IsNullOrEmpty(ProjectEndDate))
                                {
                                    newEndDate = DateTime.ParseExact(ProjectEndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                }

                                // Check if new data is newer than existing data
                                bool shouldUpdate = true;
                                if (existingEndDate.HasValue && newEndDate.HasValue)
                                {
                                    // Only update if new end date is equal to or later than existing
                                    if (newEndDate.Value < existingEndDate.Value)
                                    {
                                        // Skip this record - data in file is older
                                        _skippedCount++;
                                        _skippedProjects.Add(Symphony_Number);
                                        shouldUpdate = false;
                                    }
                                }

                                if (shouldUpdate && Symphony_Number.Length > 0)
                                {
                                    if (a == true && b == true)
                                    {
                                        _core.DatabaseInsert(_conn, "UPDATE [dbo].[Project] SET [Project_StartDate] = '" + ProjectStartDate + "', [Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '" + Project_Owner + "', ClientName_ID = '" + ClientID + "', Creator = '" + Session["Email"].ToString() + "' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                                    }
                                    else if (a == true && b == false)
                                    {
                                        _core.DatabaseInsert(_conn, "UPDATE [dbo].[Project] SET [Project_StartDate] = '" + ProjectStartDate + "', [Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '" + Project_Owner + "', ClientName_ID = '" + ClientID + "', Creator = '" + Session["Email"].ToString() + "' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                                    }
                                    else if (a == false && b == true)
                                    {
                                        _core.DatabaseInsert(_conn, "UPDATE [dbo].[Project] SET [Project_StartDate] = '" + ProjectStartDate + "', [Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '" + Project_Owner + "', ClientName_ID = '" + ClientID + "', Creator = '" + Session["Email"].ToString() + "' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                                    }
                                    else if (a == false && b == false)
                                    {
                                        _core.DatabaseInsert(_conn, "UPDATE [dbo].[Project] SET [Project_StartDate] = NULL, [Project_EndDate] = NULL, [Status] = '" + Status + "', [Project_Owner] = '" + Project_Owner + "', ClientName_ID = '" + ClientID + "', Creator = '" + Session["Email"].ToString() + "' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                                    }
                                    _updatedCount++;
                                }
                            }
                            else if (dtProject.Rows.Count == 0 && Symphony_Number.Length > 0)
                            {
                                // New project - insert
                                if (a == true && b == true)
                                {
                                    _core.DatabaseInsert(_conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "','" + Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd") + "','" + Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd") + "','" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                                }
                                else if (a == true && b == false)
                                {
                                    _core.DatabaseInsert(_conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "','" + Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd") + "',NULL,'" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                                }
                                else if (a == false && b == false)
                                {
                                    _core.DatabaseInsert(_conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "',NULL,NULL,'" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                                }
                                _insertedCount++;
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
            }

        }
        public DataTable HtmlToDataTable(string html, int startRowIndex = 8)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null) return null;

            var dt = new DataTable();

            // Get all rows
            var rows = table.SelectNodes(".//tr");
            if (rows == null) return dt;

            // We'll assume the columns are defined in the row BEFORE the startRowIndex,
            // e.g. row 5 (index 4), or you can define them manually.

            // Let's take columns from row 5 (index 4), so startRowIndex should be > 5

            if (startRowIndex <= 1 || startRowIndex > rows.Count)
                throw new ArgumentException("startRowIndex is out of range");

            // Define columns from row (startRowIndex - 1)
            var headerCells = rows[startRowIndex - 2].SelectNodes("th|td");
            if (headerCells == null) return dt;

            foreach (var cell in headerCells)
            {
                dt.Columns.Add(cell.InnerText.Trim());
            }

            // Now add data rows starting from startRowIndex (1-based)
            for (int i = startRowIndex - 1; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes("td");
                if (cells == null) continue;

                var dataRow = dt.NewRow();
                for (int j = 0; j < cells.Count && j < dt.Columns.Count; j++)
                {
                    dataRow[j] = cells[j].InnerText.Trim();
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
        }
        protected void Button10_Click1(object sender, EventArgs e)
        {
            if(Panel3.Visible == true)
            {
                Panel3.Visible = false;
            }
            else
            {
                Panel3.Visible = true;
            }
        }
    }
}