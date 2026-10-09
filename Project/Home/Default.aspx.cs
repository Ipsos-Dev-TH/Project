using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
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

        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private string CurrentEmail
        {
            get { return AppAuth.CurrentEmail; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string command = Request.QueryString["Command"];
            string id = Request.QueryString["ID"];

            if (!IsPostBack)
            {
                LoadDropdownData();
                LoadProjectData(command, id);
                ShowQueryStringMessage();
            }

            ApplyRoleVisibility();
        }

        // ------------------------------------------------------------------
        // Role / messages
        // ------------------------------------------------------------------

        private void ApplyRoleVisibility()
        {
            if (AppAuth.CanEdit)
            {
                return;
            }

            Button7.Visible = false;
            Button8.Visible = false;
            Button10.Visible = false;
            BtnDelete.Visible = false;
            Button2.Visible = false;
            Panel3.Visible = false;
        }

        private bool EnsureCanEdit()
        {
            if (AppAuth.CanEdit)
            {
                return true;
            }
            ShowFormMessage("error", "คุณไม่มีสิทธิ์แก้ไขข้อมูล (read-only access).");
            return false;
        }

        private void ShowQueryStringMessage()
        {
            string msg = Request.QueryString["msg"];
            if (msg == "saved")
            {
                ShowFormMessage("success", "บันทึกข้อมูลเรียบร้อยแล้ว (Project saved).");
            }
            else if (msg == "deleted")
            {
                ShowFormMessage("success", "ลบโปรเจกต์เรียบร้อยแล้ว (Project deleted).");
            }
        }

        /// <summary>Shows a plain-text message (HTML-encoded) at the top of the project form.</summary>
        private void ShowFormMessage(string kind, string text)
        {
            ShowFormMessageHtml(kind, HttpUtility.HtmlEncode(text));
        }

        /// <summary>Shows a message whose HTML has already been encoded by the caller.</summary>
        private void ShowFormMessageHtml(string kind, string encodedHtml)
        {
            string icon = kind == "success" ? "check-circle" : (kind == "warning" ? "exclamation-triangle" : "times-circle");
            LblFormMessage.Text = "<div class='ipsos-alert ipsos-alert-" + kind + "'><i class='fas fa-" + icon + "'></i><div>" + encodedHtml + "</div></div>";
        }

        private void Alert(string text)
        {
            ClientScript.RegisterStartupScript(GetType(), "myalert",
                "alert('" + HttpUtility.JavaScriptStringEncode(text) + "');", true);
        }

        // ------------------------------------------------------------------
        // Helpers used by markup
        // ------------------------------------------------------------------

        /// <summary>Formats a date value as yyyy-MM-dd (Gregorian, invariant). Empty for null/DBNull.</summary>
        protected string FormatDate(object value)
        {
            DateTime? d = ToDateOrNull(value);
            return d.HasValue ? d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        /// <summary>Builds a safe CSS class (only [a-z0-9-]) from a status value.</summary>
        protected string StatusCssClass(object status)
        {
            string s = status == null || status == DBNull.Value ? string.Empty : status.ToString();
            s = s.Trim().ToLowerInvariant().Replace(" ", "-");
            s = Regex.Replace(s, "[^a-z0-9-]", string.Empty);
            if (s.Length == 0)
            {
                s = "unknown";
            }
            return "status-" + s;
        }

        private static DateTime? ToDateOrNull(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }
            if (value is DateTime)
            {
                return (DateTime)value;
            }
            DateTime parsed;
            if (DateTime.TryParseExact(Convert.ToString(value, CultureInfo.InvariantCulture),
                new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" },
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                return parsed;
            }
            return null;
        }

        private static string ToText(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }
            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------
        // Loading
        // ------------------------------------------------------------------

        private void LoadDropdownData()
        {
            // Project Type dropdown
            DataTable dttype = _core.DatabaseQueryWithParams(_conn, _sql.Select_Project_Type_all);
            AddDefaultRow(dttype, "Project_Type", "---Please Choose---");
            BindDropdown(DropDownList1, dttype, "Project_Type", "ID");

            // Product Type dropdown
            DataTable dtproducttype = _core.DatabaseQueryWithParams(_conn, _sql.Select_Product_Type_all);
            AddDefaultRow(dtproducttype, "Product_Type", "---Please Choose---");
            BindDropdown(DropDownList16, dtproducttype, "Product_Type", "ID");

            // Client DB dropdown
            DataTable dtclientdb = _core.DatabaseQueryWithParams(_conn, _sql.Select_ClientDB_active);
            AddDefaultRow(dtclientdb, "Name", "---Please Choose---");
            BindDropdown(DropDownList17, dtclientdb, "Name", "ID");

            // Client Name dropdown
            DataTable dtclientname = _core.DatabaseQueryWithParams(_conn, _sql.Select_ClientName_active);
            AddDefaultRow(dtclientname, "ClientName", "---Please Choose---");
            BindDropdown(DropDownList18, dtclientname, "ClientName", "ID");

            // Market Sector dropdown
            DataTable dtmarketsector = _core.DatabaseQueryWithParams(_conn, _sql.Select_MarketSector_active);
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
            if (command != "View" && command != "Edit")
            {
                return;
            }

            int projectId;
            if (!int.TryParse(id, out projectId))
            {
                return;
            }

            Button7.Enabled = false;
            Button8.Enabled = true;

            DataTable dtData = _core.DatabaseQueryWithParams(_conn,
                _sql.Select_Project_byIDSelected,
                new SqlParameter("@IDSelected", projectId));

            if (dtData.Rows.Count == 0)
            {
                Button8.Enabled = false;
                ShowFormMessage("warning", "ไม่พบข้อมูลโปรเจกต์ (อาจถูกลบแล้ว) / Project not found.");
                return;
            }

            DataRow row = dtData.Rows[0];

            TextBox1.Text = ToText(row["Symphony_Number"]);
            TextBox2.Text = ToText(row["Project_Number"]);
            TextBox3.Text = ToText(row["Project_Name"]);
            TextBox22.Text = ToText(row["Project_Owner"]);

            // Display Status (read-only)
            string status = ToText(row["Status"]);
            if (status.Length == 0)
            {
                status = "Unknown";
            }
            LblProjectStatus.Text = HttpUtility.HtmlEncode(status);
            LblProjectStatus.CssClass = "status-badge-lg " + StatusCssClass(status);

            datepicker1.Text = FormatDate(row["Project_StartDate"]);
            datepicker2.Text = FormatDate(row["Project_EndDate"]);

            // Set dropdown selections safely
            TrySelectDropdownValue(DropDownList1, ToText(row["Project_Type_ID"]));
            TrySelectDropdownValue(DropDownList16, ToText(row["Product_Type_ID"]));
            TrySelectDropdownText(DropDownList18, ToText(row["ClientName"]));
            TrySelectDropdownText(DropDownList19, ToText(row["SectorName"]));
            TrySelectDropdownValue(DropDownList17, ToText(row["ClientDBID"]));

            bool usediField;
            if (bool.TryParse(ToText(row["Used_iField"]), out usediField))
            {
                CheckBox1.Checked = usediField;
            }

            // Load project subtypes
            LoadProjectSubTypes(ToText(row["Project_SubType_ID"]));

            if (command == "Edit")
            {
                if (AppAuth.CanEdit)
                {
                    EnableControl();
                }
            }
            else
            {
                BtnDelete.Visible = AppAuth.CanEdit;
                LoadProjectHistory(projectId, ToText(row["Symphony_Number"]));
            }
        }

        private void LoadProjectHistory(int projectId, string symphonyNumber)
        {
            string idText = projectId.ToString(CultureInfo.InvariantCulture);
            DataTable dt = _core.DatabaseQueryWithParams(_conn, _sql.Select_Project_History,
                new SqlParameter("@NewPattern", "ProjectID=" + idText + ";%"),
                new SqlParameter("@LegacyPattern", "ID-" + idText + "{%"),
                new SqlParameter("@LegacyInsert", "Insert Symphony_Number = " + symphonyNumber));
            GridViewHistory.DataSource = dt;
            GridViewHistory.DataBind();
            PanelHistory.Visible = true;
        }

        private void TrySelectDropdownValue(DropDownList ddl, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            ddl.ClearSelection();
            ListItem item = ddl.Items.FindByValue(value);
            if (item != null)
            {
                item.Selected = true;
            }
        }

        private void TrySelectDropdownText(DropDownList ddl, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ddl.ClearSelection();
            ListItem item = ddl.Items.FindByText(text);
            if (item != null)
            {
                item.Selected = true;
            }
        }

        private void LoadProjectSubTypes(string selectedSubTypeId)
        {
            int typeId;
            if (!int.TryParse(DropDownList1.SelectedValue, out typeId) || typeId <= 0)
            {
                DropDownList2.Items.Clear();
                DropDownList2.Items.Add(new ListItem("---Please Choose---", "0"));
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
            TextBox3.Enabled = true;
            TextBox21.Enabled = true;
            TextBox22.Enabled = true;
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

        // ------------------------------------------------------------------
        // Save (insert / edit)
        // ------------------------------------------------------------------

        private static bool TryParseFormDate(string text, out DateTime? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            DateTime parsed;
            if (DateTime.TryParseExact(text.Trim(), new[] { "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                value = parsed;
                return true;
            }
            return false;
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            if (!EnsureCanEdit())
            {
                return;
            }

            string command = Request.QueryString["Command"];
            string idText = Request.QueryString["ID"];
            bool isEdit = command == "Edit";
            if (command == "View")
            {
                return;
            }

            int editId = 0;
            if (isEdit && !int.TryParse(idText, out editId))
            {
                return;
            }

            // ---- Validation: name every missing / invalid field ----
            List<string> problems = new List<string>();
            string rawSym = TextBox1.Text.Trim();
            string formattedSym = rawSym.Length == 0 ? string.Empty : ChangeSymNumber(rawSym);
            if (rawSym.Length == 0)
            {
                problems.Add("Symphony Number");
            }
            else if (formattedSym.Length == 0)
            {
                problems.Add("Symphony Number (รูปแบบไม่ถูกต้อง เช่น 26-076446-01-01)");
            }
            if (TextBox3.Text.Trim().Length == 0) problems.Add("Project Name");
            if (DropDownList1.SelectedIndex <= 0) problems.Add("Project Type");
            if (DropDownList2.SelectedIndex <= 0) problems.Add("Project Sub Type");
            if (DropDownList17.SelectedIndex <= 0) problems.Add("Client DB");
            if (DropDownList18.SelectedIndex <= 0) problems.Add("Client Name");
            if (DropDownList19.SelectedIndex <= 0) problems.Add("Market Sector");
            string owner = TextBox22.Text.Trim();
            if (owner.Length == 0)
            {
                problems.Add("Project Owner Email");
            }
            else if (!EmailRegex.IsMatch(owner))
            {
                problems.Add("Project Owner Email (รูปแบบอีเมลไม่ถูกต้อง)");
            }

            DateTime? startDate;
            DateTime? endDate;
            if (!TryParseFormDate(datepicker1.Text, out startDate)) problems.Add("Start Date (รูปแบบวันที่ไม่ถูกต้อง)");
            if (!TryParseFormDate(datepicker2.Text, out endDate)) problems.Add("End Date (รูปแบบวันที่ไม่ถูกต้อง)");
            if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            {
                problems.Add("End Date ต้องไม่น้อยกว่า Start Date");
            }

            if (problems.Count > 0)
            {
                string header = "กรุณากรอกข้อมูลให้ถูกต้อง / Please check: ";
                ShowFormMessage("error", header + string.Join(", ", problems));
                Alert(header + "\n- " + string.Join("\n- ", problems));
                return;
            }

            TextBox1.Text = formattedSym;
            TextBox22.Text = owner;

            // ---- Duplicate check ----
            DataTable dtcheck = _core.DatabaseQueryWithParams(_conn, _sql.Select_Project_CheckExist,
                new SqlParameter("@SymphonyKey", NormalizeSymNumber(formattedSym)),
                new SqlParameter("@ExcludeId", editId));
            if (dtcheck.Rows.Count > 0)
            {
                string dupMsg = "Symphony Number นี้มีอยู่ในระบบแล้ว (ID " + ToText(dtcheck.Rows[0]["ID"]) + ")";
                ShowFormMessage("error", dupMsg);
                Alert(dupMsg);
                return;
            }

            // Old values for the change log (edit only)
            DataRow oldRow = null;
            if (isEdit)
            {
                DataTable dtOld = _core.DatabaseQueryWithParams(_conn, _sql.Select_Project_byIDSelected,
                    new SqlParameter("@IDSelected", editId));
                if (dtOld.Rows.Count == 0)
                {
                    ShowFormMessage("error", "ไม่พบโปรเจกต์ที่ต้องการแก้ไข (อาจถูกลบแล้ว) / Project not found.");
                    return;
                }
                oldRow = dtOld.Rows[0];
            }

            int savedId = 0;
            string error = null;
            DateTime now = DateTime.Now;
            try
            {
                using (SqlConnection con = new SqlConnection(_conn))
                using (SqlCommand cmd = new SqlCommand(isEdit ? _sql.Update_Project_byID : _sql.Insert_Project_new, con))
                {
                    cmd.Parameters.AddWithValue("@Symphony_Number", formattedSym);
                    cmd.Parameters.AddWithValue("@Project_Number", TextBox2.Text);
                    cmd.Parameters.AddWithValue("@Project_Name", TextBox3.Text.Trim());
                    cmd.Parameters.AddWithValue("@Project_Type_ID", DropDownList1.SelectedValue);
                    cmd.Parameters.AddWithValue("@Project_SubType_ID", DropDownList2.SelectedValue);
                    cmd.Parameters.AddWithValue("@Product_Type_ID", DropDownList16.SelectedValue);
                    cmd.Parameters.AddWithValue("@Project_StartDate", startDate.HasValue ? (object)startDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Project_EndDate", endDate.HasValue ? (object)endDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ClientDBID", DropDownList17.SelectedValue);
                    cmd.Parameters.AddWithValue("@ClientName_ID", DropDownList18.SelectedValue);
                    cmd.Parameters.AddWithValue("@MarketSector_ID", DropDownList19.SelectedValue);
                    cmd.Parameters.AddWithValue("@Used_iField", CheckBox1.Checked);
                    cmd.Parameters.AddWithValue("@Project_Owner", owner);

                    if (isEdit)
                    {
                        cmd.Parameters.AddWithValue("@IDSelected", editId);
                        cmd.Parameters.AddWithValue("@Modified_By", CurrentEmail);
                        cmd.Parameters.AddWithValue("@Modified_DateTime", now);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Status", "NEW");
                        cmd.Parameters.AddWithValue("@Creator", CurrentEmail);
                        cmd.Parameters.AddWithValue("@Created_DateTime", now);
                    }

                    con.Open();
                    if (isEdit)
                    {
                        int affected = cmd.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            error = "ไม่พบโปรเจกต์ที่ต้องการแก้ไข (อาจถูกลบแล้ว) / Project not found.";
                        }
                        else
                        {
                            savedId = editId;
                        }
                    }
                    else
                    {
                        object result = cmd.ExecuteScalar();
                        savedId = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
                        if (savedId <= 0)
                        {
                            error = "บันทึกไม่สำเร็จ: ไม่ได้รับ ID ของโปรเจกต์ใหม่";
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                error = "บันทึกไม่สำเร็จ (database error): " + ex.Message;
            }

            if (error != null)
            {
                ShowFormMessage("error", error);
                return;
            }

            // Audit log - the save already succeeded, so a log failure must not block the redirect
            try
            {
                if (isEdit)
                {
                    _core.Logs(_conn, "Project", "Update", BuildChangeLog(savedId, oldRow, formattedSym, startDate, endDate), CurrentEmail);
                }
                else
                {
                    string data = "ProjectID=" + savedId.ToString(CultureInfo.InvariantCulture) + ";"
                        + "Symphony_Number=" + formattedSym + ";"
                        + "Project_Name=" + TextBox3.Text.Trim() + ";"
                        + "Project_Type_ID=" + DropDownList1.SelectedValue + ";"
                        + "Project_SubType_ID=" + DropDownList2.SelectedValue + ";"
                        + "ClientName_ID=" + DropDownList18.SelectedValue + ";"
                        + "MarketSector_ID=" + DropDownList19.SelectedValue + ";"
                        + "Project_Owner=" + owner + ";";
                    _core.Logs(_conn, "Project", "Insert", data, CurrentEmail);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Project log failed: " + ex.Message);
            }

            Response.Redirect("/Home/Default.aspx?Command=View&ID=" + savedId.ToString(CultureInfo.InvariantCulture) + "&msg=saved", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string BuildChangeLog(int projectId, DataRow oldRow, string newSym, DateTime? newStart, DateTime? newEnd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ProjectID=").Append(projectId.ToString(CultureInfo.InvariantCulture)).Append(";");

            AppendChange(sb, "Symphony_Number", ToText(oldRow["Symphony_Number"]), newSym);
            AppendChange(sb, "Project_Number", ToText(oldRow["Project_Number"]), TextBox2.Text);
            AppendChange(sb, "Project_Name", ToText(oldRow["Project_Name"]), TextBox3.Text.Trim());
            AppendChange(sb, "Project_Type_ID", ToText(oldRow["Project_Type_ID"]), DropDownList1.SelectedValue);
            AppendChange(sb, "Project_SubType_ID", ToText(oldRow["Project_SubType_ID"]), DropDownList2.SelectedValue);
            AppendChange(sb, "Product_Type_ID", ToText(oldRow["Product_Type_ID"]), DropDownList16.SelectedValue);
            AppendChange(sb, "Project_StartDate", FormatDate(oldRow["Project_StartDate"]), FormatDate(newStart));
            AppendChange(sb, "Project_EndDate", FormatDate(oldRow["Project_EndDate"]), FormatDate(newEnd));
            AppendChange(sb, "ClientDBID", ToText(oldRow["ClientDBID"]), DropDownList17.SelectedValue);
            AppendChange(sb, "ClientName_ID", ToText(oldRow["ClientName_ID"]), DropDownList18.SelectedValue);
            AppendChange(sb, "MarketSector_ID", ToText(oldRow["MarketSector_ID"]), DropDownList19.SelectedValue);
            AppendChange(sb, "Used_iField", ToText(oldRow["Used_iField"]), CheckBox1.Checked.ToString());
            AppendChange(sb, "Project_Owner", ToText(oldRow["Project_Owner"]), TextBox22.Text.Trim());
            return sb.ToString();
        }

        private static void AppendChange(StringBuilder sb, string field, string oldValue, string newValue)
        {
            oldValue = oldValue ?? string.Empty;
            newValue = newValue ?? string.Empty;
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                sb.Append(field).Append("[").Append(oldValue).Append(",").Append(newValue).Append("];");
            }
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadProjectSubTypes(null);
        }

        // ------------------------------------------------------------------
        // Delete (soft)
        // ------------------------------------------------------------------

        protected void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!EnsureCanEdit())
            {
                return;
            }

            int projectId;
            if (!int.TryParse(Request.QueryString["ID"], out projectId))
            {
                return;
            }

            DataTable dtOld = _core.DatabaseQueryWithParams(_conn, _sql.Select_Project_byIDSelected,
                new SqlParameter("@IDSelected", projectId));
            if (dtOld.Rows.Count == 0)
            {
                ShowFormMessage("error", "ไม่พบโปรเจกต์ (อาจถูกลบแล้ว) / Project not found.");
                return;
            }
            string symphony = ToText(dtOld.Rows[0]["Symphony_Number"]);
            string projectName = ToText(dtOld.Rows[0]["Project_Name"]);

            int affected;
            try
            {
                using (SqlConnection con = new SqlConnection(_conn))
                using (SqlCommand cmd = new SqlCommand(_sql.SoftDelete_Project_byID, con))
                {
                    cmd.Parameters.AddWithValue("@IDSelected", projectId);
                    cmd.Parameters.AddWithValue("@Modified_By", CurrentEmail);
                    cmd.Parameters.AddWithValue("@Modified_DateTime", DateTime.Now);
                    con.Open();
                    affected = cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                ShowFormMessage("error", "ลบไม่สำเร็จ (database error): " + ex.Message);
                return;
            }

            if (affected == 0)
            {
                ShowFormMessage("error", "ไม่พบโปรเจกต์ (อาจถูกลบแล้ว) / Project not found.");
                return;
            }

            try
            {
                _core.Logs(_conn, "Project", "Delete",
                    "ProjectID=" + projectId.ToString(CultureInfo.InvariantCulture) + ";Symphony_Number=" + symphony + ";Project_Name=" + projectName + ";",
                    CurrentEmail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Delete log failed: " + ex.Message);
            }

            Response.Redirect("/Home/Default.aspx?msg=deleted", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ------------------------------------------------------------------
        // Search / export
        // ------------------------------------------------------------------

        protected void Button6_Click(object sender, EventArgs e)
        {
            GridView2.PageIndex = 0;
            SearchProjects();
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            SearchProjects();
        }

        private DataTable RunSearch()
        {
            string searchColumn = DropDownList15.SelectedValue;
            string searchValue = (TextBox21.Text ?? string.Empty).Trim();

            // Validate column name against whitelist
            if (!SQL.IsValidSearchColumn(searchColumn))
            {
                return null;
            }

            return _core.DatabaseQueryWithParams(_conn, _sql.GetSearchQuery(searchColumn),
                new SqlParameter("@Pattern", SQL.ContainsPattern(searchValue)),
                new SqlParameter("@KeyPattern", SQL.ContainsPattern(NormalizeSymNumber(searchValue))));
        }

        private void SearchProjects()
        {
            DataTable dtSearch = RunSearch();
            if (dtSearch == null)
            {
                return;
            }
            GridView2.DataSource = dtSearch;
            GridView2.DataBind();
        }

        protected void BtnExportCsv_Click(object sender, EventArgs e)
        {
            DataTable dt = RunSearch();
            if (dt == null)
            {
                return;
            }

            string[] headers = { "ID", "Symphony Number", "Project Name", "Type", "SubType", "Client", "Market Sector", "Owner", "Start Date", "End Date", "Status" };
            string[] fields = { "ID", "Symphony_Number", "Project_Name", "Project_Type", "SubType", "ClientName", "SectorName", "Project_Owner", "Project_StartDate", "Project_EndDate", "Status" };

            string fileName = "projects_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";

            Response.Clear();
            Response.ClearHeaders();
            Response.Buffer = false;
            Response.ContentType = "text/csv";
            Response.Charset = "utf-8";
            Response.ContentEncoding = new UTF8Encoding(false);
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);

            byte[] bom = Encoding.UTF8.GetPreamble();
            Response.OutputStream.Write(bom, 0, bom.Length);

            Response.Write(string.Join(",", headers.Select(h => CsvField(h))) + "\r\n");
            foreach (DataRow row in dt.Rows)
            {
                List<string> values = new List<string>(fields.Length);
                foreach (string field in fields)
                {
                    object value = dt.Columns.Contains(field) ? row[field] : null;
                    values.Add(CsvField(ToText(value)));
                }
                Response.Write(string.Join(",", values) + "\r\n");
            }

            Response.Flush();
            Response.SuppressContent = true;
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>Quotes a CSV value and neutralizes spreadsheet formula injection.</summary>
        private static string CsvField(string value)
        {
            string s = value ?? string.Empty;
            if (s.Length > 0 && "=+-@\t\r".IndexOf(s[0]) >= 0)
            {
                s = "'" + s;
            }
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        // ------------------------------------------------------------------
        // Navigation
        // ------------------------------------------------------------------

        protected void Button7_Click(object sender, EventArgs e)
        {
            if (!EnsureCanEdit())
            {
                return;
            }
            EnableControl();
            Panel1.Visible = false;
        }

        protected void Button8_Click(object sender, EventArgs e)
        {
            if (!EnsureCanEdit())
            {
                return;
            }
            int projectId;
            if (!int.TryParse(Request.QueryString["ID"], out projectId))
            {
                return;
            }
            Response.Redirect("/Home/Default.aspx?Command=Edit&ID=" + projectId.ToString(CultureInfo.InvariantCulture), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void GridView2_SelectedIndexChanging(object sender, GridViewSelectEventArgs e)
        {
            object key = GridView2.DataKeys[e.NewSelectedIndex].Value;
            int projectId;
            if (key == null || !int.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), out projectId))
            {
                return;
            }
            Response.Redirect("/Home/Default.aspx?Command=View&ID=" + projectId.ToString(CultureInfo.InvariantCulture), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox1.Text = ChangeSymNumber(TextBox1.Text);
        }

        protected void TextBox21_TextChanged(object sender, EventArgs e)
        {
            Button6_Click(null, null);
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

        private static string NormalizeSymNumber(string symphonyNumber)
        {
            return (symphonyNumber ?? "").Replace("-", "").Trim();
        }

        // ------------------------------------------------------------------
        // Upload panel
        // ------------------------------------------------------------------

        protected void Button10_Click1(object sender, EventArgs e)
        {
            if (!AppAuth.CanEdit)
            {
                Panel3.Visible = false;
                return;
            }
            Panel3.Visible = !Panel3.Visible;
            if (Panel3.Visible)
            {
                BindUploadHistory();
            }
        }

        private void BindUploadHistory()
        {
            DataTable dt = _core.DatabaseQueryWithParams(_conn, _sql.Select_Upload_History_Recent);
            GridViewUploadHistory.DataSource = dt;
            GridViewUploadHistory.DataBind();
        }

        protected void Button9_Click1(object sender, EventArgs e)
        {
            RunUpload(false);
        }

        protected void BtnPreview_Click(object sender, EventArgs e)
        {
            RunUpload(true);
        }

        // Columns required in the uploaded file (Job Owner / Job Manager are optional)
        private static readonly string[] RequiredUploadColumns =
        {
            "Job ID", "Record Detail", "Status", "Billing Client Name", "Client Brand Market Sector",
            "DC Technique Group", "DC Mode", "Job Start Date", "Job End Date"
        };

        private const string RowNumberColumn = "__RowNo";
        private const int MaxErrorsShown = 20;
        private const int MaxErrorDetailLength = 20000;

        private void SetUploadStatus(string cssKind, string encodedHtml)
        {
            LblStatus.ForeColor = System.Drawing.Color.Empty;
            LblStatus.Text = "<div class='upload-result " + cssKind + "'>" + encodedHtml + "</div>";
        }

        private void RunUpload(bool preview)
        {
            if (!AppAuth.CanEdit)
            {
                Panel3.Visible = false;
                ShowFormMessage("error", "คุณไม่มีสิทธิ์อัปโหลดข้อมูล (read-only access).");
                return;
            }

            Panel3.Visible = true;

            if (!FileUpload1.HasFile)
            {
                SetUploadStatus("error", "Please select a file to upload.");
                BindUploadHistory();
                return;
            }

            string originalFileName = Path.GetFileName(FileUpload1.FileName);
            string fileExtension = Path.GetExtension(originalFileName);
            if (!AllowedFileExtensions.Contains(fileExtension))
            {
                SetUploadStatus("error", "Invalid file type. Only Excel (.xlsx, .xls) and HTML files are allowed.");
                BindUploadHistory();
                return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + fileExtension.ToLowerInvariant());
            try
            {
                FileUpload1.SaveAs(tempPath);

                DataTable dt;
                try
                {
                    dt = ReadUploadFile(tempPath, fileExtension);
                }
                catch (Exception ex)
                {
                    SetUploadStatus("error", "อ่านไฟล์ไม่สำเร็จ / Cannot read file: " + HttpUtility.HtmlEncode(ex.Message));
                    return;
                }

                // Required columns up front - process nothing if any are missing
                List<string> missing = RequiredUploadColumns.Where(c => !dt.Columns.Contains(c)).ToList();
                if (missing.Count > 0)
                {
                    string missingText = string.Join(", ", missing);
                    SetUploadStatus("error", "<strong><i class='fas fa-times-circle'></i> ไฟล์ขาดคอลัมน์ที่จำเป็น / Missing required columns:</strong> "
                        + HttpUtility.HtmlEncode(missingText) + "<br/>ไม่มีการประมวลผลข้อมูลใดๆ (nothing was processed).");
                    WriteUploadHistory(originalFileName, preview, 0, 1, "Missing required columns: " + missingText);
                    return;
                }

                bool committed = ProcessUpload(dt, preview);

                List<string> details = new List<string>();
                if (_fatalError != null)
                {
                    details.Add("FATAL: " + _fatalError);
                }
                details.AddRange(_rowErrors);
                details.AddRange(_rowWarnings.Select(w => "[Warning] " + w));
                WriteUploadHistory(originalFileName, preview, _totalRows, _errorCount, string.Join("\n", details));

                ShowUploadResult(originalFileName, preview, committed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in file upload: " + ex);
                SetUploadStatus("error", "Error processing file: " + HttpUtility.HtmlEncode(ex.Message));
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Temp file cleanup failed: " + ex.Message);
                }
                BindUploadHistory();
            }
        }

        private void ShowUploadResult(string fileName, bool preview, bool committed)
        {
            bool hasProblems = _errorCount > 0 || _fatalError != null;
            string cssKind = _fatalError != null ? "error" : (hasProblems || _rowWarnings.Count > 0 ? "warning" : "success");
            if (preview && cssKind == "success")
            {
                cssKind = "preview";
            }

            StringBuilder sb = new StringBuilder();
            if (preview)
            {
                sb.Append("<strong><i class='fas fa-eye'></i> PREVIEW - ยังไม่ได้บันทึกข้อมูล (nothing was saved)</strong><br/>");
            }
            else if (_fatalError != null)
            {
                sb.Append("<strong><i class='fas fa-times-circle'></i> Upload aborted - ไม่มีการบันทึกข้อมูล (nothing was saved)</strong><br/>");
            }
            else
            {
                sb.Append("<strong><i class='fas fa-")
                  .Append(hasProblems ? "exclamation-circle" : "check-circle")
                  .Append("'></i> Upload Complete!</strong><br/>");
            }

            sb.Append("<small>File: ").Append(HttpUtility.HtmlEncode(fileName)).Append("</small><br/>");

            if (_fatalError != null)
            {
                sb.Append("<span style='color: #c62828;'>").Append(HttpUtility.HtmlEncode(_fatalError)).Append("</span><br/>");
            }

            string verbIns = preview ? "Would be added" : "New projects added";
            string verbUpd = preview ? "Would be updated" : "Projects updated";
            sb.Append("Rows processed: <strong>").Append(_totalRows).Append("</strong><br/>");
            sb.Append("<i class='fas fa-plus-circle' style='color: #2e7d32;'></i> ").Append(verbIns).Append(": <strong>").Append(_insertedCount).Append("</strong><br/>");
            sb.Append("<i class='fas fa-sync-alt' style='color: #1565c0;'></i> ").Append(verbUpd).Append(": <strong>").Append(_updatedCount).Append("</strong>");

            if (_skippedCount > 0)
            {
                sb.Append("<br/><i class='fas fa-forward' style='color: #ef6c00;'></i> Skipped (older data): <strong>").Append(_skippedCount).Append("</strong>");
                IEnumerable<string> shown = _skippedProjects.Take(MaxErrorsShown).Select(p => HttpUtility.HtmlEncode(p));
                sb.Append("<br/><small style='color: #666;'>Skipped: ").Append(string.Join(", ", shown));
                if (_skippedProjects.Count > MaxErrorsShown)
                {
                    sb.Append("... (+").Append(_skippedProjects.Count - MaxErrorsShown).Append(" more)");
                }
                sb.Append("</small>");
            }

            if (_errorCount > 0)
            {
                sb.Append("<br/><i class='fas fa-times-circle' style='color: #c62828;'></i> Errors: <strong>").Append(_errorCount).Append("</strong>");
                AppendMessageList(sb, _rowErrors, "#c62828");
            }

            if (_rowWarnings.Count > 0)
            {
                sb.Append("<br/><i class='fas fa-exclamation-triangle' style='color: #ef6c00;'></i> Warnings: <strong>").Append(_rowWarnings.Count).Append("</strong>");
                AppendMessageList(sb, _rowWarnings, "#ef6c00");
            }

            SetUploadStatus(cssKind, sb.ToString());
        }

        private static void AppendMessageList(StringBuilder sb, List<string> messages, string color)
        {
            sb.Append("<ul class='upload-messages' style='color: ").Append(color).Append(";'>");
            foreach (string m in messages.Take(MaxErrorsShown))
            {
                sb.Append("<li>").Append(HttpUtility.HtmlEncode(m)).Append("</li>");
            }
            if (messages.Count > MaxErrorsShown)
            {
                sb.Append("<li>... (+").Append(messages.Count - MaxErrorsShown).Append(" more)</li>");
            }
            sb.Append("</ul>");
        }

        private void WriteUploadHistory(string fileName, bool preview, int totalRows, int errorCount, string errorDetail)
        {
            try
            {
                string name = fileName ?? string.Empty;
                if (name.Length > 260)
                {
                    name = name.Substring(0, 260);
                }
                string detail = errorDetail ?? string.Empty;
                if (detail.Length > MaxErrorDetailLength)
                {
                    detail = detail.Substring(0, MaxErrorDetailLength);
                }

                _core.DatabaseInsertWithParams(_conn, _sql.Insert_Upload_History,
                    new SqlParameter("@File_Name", name),
                    new SqlParameter("@Uploaded_By", CurrentEmail),
                    new SqlParameter("@Uploaded_DateTime", DateTime.Now),
                    new SqlParameter("@Is_Preview", preview),
                    new SqlParameter("@Total_Rows", totalRows),
                    new SqlParameter("@Inserted_Count", _insertedCount),
                    new SqlParameter("@Updated_Count", _updatedCount),
                    new SqlParameter("@Skipped_Count", _skippedCount),
                    new SqlParameter("@Error_Count", errorCount),
                    new SqlParameter("@Error_Detail", detail.Length == 0 ? (object)DBNull.Value : detail));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Upload_History insert failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // File reading
        // ------------------------------------------------------------------

        private DataTable ReadUploadFile(string filePath, string extension)
        {
            bool isHtml = extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);

            List<object[]> rawRows;
            if (isHtml)
            {
                rawRows = ReadHtmlRows(File.ReadAllText(filePath));
            }
            else
            {
                try
                {
                    rawRows = ReadExcelRows(filePath);
                }
                catch (Exception excelEx)
                {
                    // Some ".xls" exports are actually HTML tables
                    System.Diagnostics.Debug.WriteLine("Excel read failed, trying HTML: " + excelEx.Message);
                    rawRows = ReadHtmlRows(File.ReadAllText(filePath));
                }
            }

            return BuildUploadTable(rawRows);
        }

        private static List<object[]> ReadExcelRows(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false
                    }
                });

                List<object[]> rows = new List<object[]>();
                if (result.Tables.Count == 0)
                {
                    return rows;
                }
                foreach (DataRow row in result.Tables[0].Rows)
                {
                    rows.Add(row.ItemArray);
                }
                return rows;
            }
        }

        private static List<object[]> ReadHtmlRows(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);

            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null)
            {
                throw new InvalidDataException("ไม่พบตารางข้อมูลในไฟล์ (no <table> found). Please upload the Excel/HTML export.");
            }

            List<object[]> rows = new List<object[]>();
            HtmlNodeCollection trs = table.SelectNodes(".//tr");
            if (trs == null)
            {
                return rows;
            }

            foreach (HtmlNode tr in trs)
            {
                HtmlNodeCollection cells = tr.SelectNodes("th|td");
                if (cells == null)
                {
                    rows.Add(new object[0]);
                    continue;
                }
                object[] values = new object[cells.Count];
                for (int i = 0; i < cells.Count; i++)
                {
                    values[i] = WebUtility.HtmlDecode(cells[i].InnerText ?? string.Empty).Replace(' ', ' ').Trim();
                }
                rows.Add(values);
            }
            return rows;
        }

        private static string CellText(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture).Replace(' ', ' ').Trim();
        }

        /// <summary>
        /// Finds the header row (first row containing a "Job ID" cell), de-duplicates header names
        /// and returns a table of raw cell values plus the source row number.
        /// </summary>
        private static DataTable BuildUploadTable(List<object[]> rawRows)
        {
            int headerIndex = -1;
            for (int i = 0; i < rawRows.Count && headerIndex < 0; i++)
            {
                foreach (object cell in rawRows[i])
                {
                    if (string.Equals(CellText(cell), "Job ID", StringComparison.OrdinalIgnoreCase))
                    {
                        headerIndex = i;
                        break;
                    }
                }
            }

            if (headerIndex < 0)
            {
                throw new InvalidDataException("ไม่พบแถวหัวตารางที่มีคอลัมน์ \"Job ID\" (header row with \"Job ID\" not found).");
            }

            DataTable dt = new DataTable();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { RowNumberColumn };
            object[] header = rawRows[headerIndex];
            for (int c = 0; c < header.Length; c++)
            {
                string name = CellText(header[c]);
                if (name.Length == 0)
                {
                    name = "Column_" + (c + 1).ToString(CultureInfo.InvariantCulture);
                }
                string unique = name;
                int suffix = 2;
                while (used.Contains(unique))
                {
                    unique = name + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                }
                used.Add(unique);
                dt.Columns.Add(unique, typeof(object));
            }
            dt.Columns.Add(RowNumberColumn, typeof(int));

            int dataColumnCount = header.Length;
            for (int r = headerIndex + 1; r < rawRows.Count; r++)
            {
                object[] cells = rawRows[r];
                bool allBlank = true;
                for (int c = 0; c < cells.Length && c < dataColumnCount; c++)
                {
                    if (CellText(cells[c]).Length > 0)
                    {
                        allBlank = false;
                        break;
                    }
                }
                if (allBlank)
                {
                    continue;
                }

                DataRow dataRow = dt.NewRow();
                for (int c = 0; c < cells.Length && c < dataColumnCount; c++)
                {
                    dataRow[c] = cells[c] ?? DBNull.Value;
                }
                dataRow[RowNumberColumn] = r + 1;
                dt.Rows.Add(dataRow);
            }

            return dt;
        }

        // ------------------------------------------------------------------
        // Upload processing
        // ------------------------------------------------------------------

        private sealed class ExistingProject
        {
            public int Id;
            public DateTime? EndDate;
        }

        // Counters / results for the current run
        private int _totalRows;
        private int _updatedCount;
        private int _insertedCount;
        private int _skippedCount;
        private int _errorCount;
        private string _fatalError;
        private List<string> _skippedProjects = new List<string>();
        private List<string> _rowErrors = new List<string>();
        private List<string> _rowWarnings = new List<string>();

        // Lookup caches
        private Dictionary<string, int> _clientCache;
        private Dictionary<string, int> _marketSectorCache;
        private Dictionary<string, int> _projectTypeCache;
        private Dictionary<string, int> _subTypeCache;              // key: "typeId|subtype"
        private Dictionary<string, ExistingProject> _existingProjectsCache; // key: dash-less Symphony Number

        // Cache changes made by the current row, undone if the row is rolled back to its savepoint
        private readonly List<Action> _rowUndo = new List<Action>();

        /// <summary>Runs the upload pipeline. Returns true when changes were committed.</summary>
        private bool ProcessUpload(DataTable dataTable, bool preview)
        {
            _totalRows = 0;
            _updatedCount = 0;
            _insertedCount = 0;
            _skippedCount = 0;
            _errorCount = 0;
            _fatalError = null;
            _skippedProjects = new List<string>();
            _rowErrors = new List<string>();
            _rowWarnings = new List<string>();

            string userEmail = CurrentEmail;
            bool committed = false;

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    LoadLookupCaches(conn, transaction);

                    try
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            DataRow row = dataTable.Rows[i];
                            string jobId = GetCell(row, "Job ID");
                            if (jobId.Length == 0)
                            {
                                continue;
                            }

                            _totalRows++;
                            string rowNo = Convert.ToString(row[RowNumberColumn], CultureInfo.InvariantCulture);
                            string savepoint = "r" + i.ToString(CultureInfo.InvariantCulture);
                            List<string> warnings = new List<string>();

                            _rowUndo.Clear();
                            transaction.Save(savepoint);
                            try
                            {
                                ProcessDataRow(row, jobId, conn, transaction, userEmail, warnings);
                                foreach (string w in warnings)
                                {
                                    _rowWarnings.Add("Row " + rowNo + " (Job ID " + jobId + "): " + w);
                                }
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    transaction.Rollback(savepoint);
                                }
                                catch (Exception rollbackEx)
                                {
                                    // The transaction is no longer usable - abort the whole upload
                                    throw new InvalidOperationException(
                                        "Row " + rowNo + " (Job ID " + jobId + "): " + ex.Message + " / rollback failed: " + rollbackEx.Message, ex);
                                }

                                UndoRowCacheChanges();
                                _errorCount++;
                                _rowErrors.Add("Row " + rowNo + " (Job ID " + jobId + "): " + ex.Message);
                            }
                        }

                        if (preview)
                        {
                            transaction.Rollback();
                        }
                        else
                        {
                            transaction.Commit();
                            committed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Everything was rolled back - nothing inserted or updated
                        _fatalError = ex.Message;
                        _insertedCount = 0;
                        _updatedCount = 0;
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception rbEx)
                        {
                            System.Diagnostics.Debug.WriteLine("Rollback failed: " + rbEx.Message);
                        }
                        committed = false;
                    }
                }
            }

            return committed;
        }

        private void UndoRowCacheChanges()
        {
            for (int i = _rowUndo.Count - 1; i >= 0; i--)
            {
                _rowUndo[i]();
            }
            _rowUndo.Clear();
        }

        private void LoadLookupCaches(SqlConnection conn, SqlTransaction transaction)
        {
            _clientCache = LoadNameCache(conn, transaction, "SELECT ID, ClientName AS Name FROM [ClientName] ORDER BY ID");
            _marketSectorCache = LoadNameCache(conn, transaction, "SELECT ID, SectorName AS Name FROM [MarketSector] ORDER BY ID");
            _projectTypeCache = LoadNameCache(conn, transaction, "SELECT ID, Project_Type AS Name FROM [Project_Type] ORDER BY ID");

            // Subtypes are unique per type, not globally
            _subTypeCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (SqlCommand cmd = new SqlCommand("SELECT ID, Project_Type_ID, SubType FROM [Project_SubType] ORDER BY ID", conn, transaction))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string name = CellText(reader["SubType"]);
                    if (name.Length == 0 || reader["Project_Type_ID"] == DBNull.Value)
                    {
                        continue;
                    }
                    string key = SubTypeKey(Convert.ToInt32(reader["Project_Type_ID"], CultureInfo.InvariantCulture), name);
                    if (!_subTypeCache.ContainsKey(key))
                    {
                        _subTypeCache[key] = Convert.ToInt32(reader["ID"], CultureInfo.InvariantCulture);
                    }
                }
            }

            // Active projects only, keyed by the dash-less number
            _existingProjectsCache = new Dictionary<string, ExistingProject>(StringComparer.OrdinalIgnoreCase);
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ID, Symphony_Key, Project_EndDate FROM [Project] WHERE Is_Deleted = 0 ORDER BY ID", conn, transaction))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string key = CellText(reader["Symphony_Key"]);
                    if (key.Length == 0 || _existingProjectsCache.ContainsKey(key))
                    {
                        continue;
                    }
                    ExistingProject p = new ExistingProject();
                    p.Id = Convert.ToInt32(reader["ID"], CultureInfo.InvariantCulture);
                    p.EndDate = reader["Project_EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Project_EndDate"], CultureInfo.InvariantCulture);
                    _existingProjectsCache[key] = p;
                }
            }
        }

        private static Dictionary<string, int> LoadNameCache(SqlConnection conn, SqlTransaction transaction, string sql)
        {
            Dictionary<string, int> cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string name = CellText(reader["Name"]);
                    if (name.Length > 0 && !cache.ContainsKey(name))
                    {
                        cache[name] = Convert.ToInt32(reader["ID"], CultureInfo.InvariantCulture);
                    }
                }
            }
            return cache;
        }

        private static string SubTypeKey(int typeId, string subType)
        {
            return typeId.ToString(CultureInfo.InvariantCulture) + "|" + subType.Trim();
        }

        /// <summary>Reads a cell as text, decoding "&amp;". Never throws for missing columns.</summary>
        private static string GetCell(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column))
            {
                return string.Empty;
            }
            object value = row[column];
            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            return CellText(value).Replace("&amp;", "&");
        }

        private static readonly string[] UploadDateFormats =
        {
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy/MM/dd",
            "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "d/M/yyyy H:mm",
            "dd-MMM-yyyy", "d-MMM-yyyy", "dd MMM yyyy", "d MMM yyyy", "dd-MMM-yy", "d-MMM-yy",
            "MM/dd/yyyy", "M/d/yyyy", "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt"
        };

        /// <summary>
        /// Type-aware date reading. Returns false when the cell has a value that cannot be read as a date.
        /// Culture-independent (never uses the server culture, which may be th-TH / Buddhist calendar).
        /// </summary>
        private static bool TryReadDate(DataRow row, string column, out DateTime? value)
        {
            value = null;
            if (row == null || !row.Table.Columns.Contains(column))
            {
                return true;
            }

            object raw = row[column];
            if (raw == null || raw == DBNull.Value)
            {
                return true;
            }
            if (raw is DateTime)
            {
                value = ((DateTime)raw).Date;
                return true;
            }
            if (raw is double)
            {
                return TryFromOADate((double)raw, out value);
            }

            string text = CellText(raw);
            if (text.Length == 0)
            {
                return true;
            }

            DateTime parsed;
            if (DateTime.TryParseExact(text, UploadDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                value = parsed.Date;
                return true;
            }

            double serial;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out serial))
            {
                return TryFromOADate(serial, out value);
            }

            return false;
        }

        private static bool TryFromOADate(double serial, out DateTime? value)
        {
            value = null;
            // Roughly 1954..2119 - anything else is not a plausible project date
            if (serial < 20000 || serial > 80000)
            {
                return false;
            }
            try
            {
                value = DateTime.FromOADate(serial).Date;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void ProcessDataRow(DataRow row, string symphonyNumber, SqlConnection conn, SqlTransaction transaction,
            string userEmail, List<string> warnings)
        {
            string symphonyKey = NormalizeSymNumber(symphonyNumber);

            string projectName = GetCell(row, "Record Detail");
            string prefix = symphonyNumber + " - ";
            if (projectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                projectName = projectName.Substring(prefix.Length).Trim();
            }
            string status = GetCell(row, "Status");
            string clientName = GetCell(row, "Billing Client Name");
            string marketSector = GetCell(row, "Client Brand Market Sector");
            string projectType = GetCell(row, "DC Technique Group");
            string subProjectType = GetCell(row, "DC Mode");

            DateTime? startDate;
            DateTime? endDate;
            if (!TryReadDate(row, "Job Start Date", out startDate))
            {
                warnings.Add("อ่าน Job Start Date ไม่ได้ (\"" + GetCell(row, "Job Start Date") + "\")");
            }
            if (!TryReadDate(row, "Job End Date", out endDate))
            {
                warnings.Add("อ่าน Job End Date ไม่ได้ (\"" + GetCell(row, "Job End Date") + "\")");
            }

            string projectOwner = ParseProjectOwner(row);

            ExistingProject existing;
            bool exists = _existingProjectsCache.TryGetValue(symphonyKey, out existing);

            // Skip rule: data in the file is older than what is in the database
            if (exists && existing.EndDate.HasValue && endDate.HasValue && endDate.Value < existing.EndDate.Value)
            {
                _skippedCount++;
                _skippedProjects.Add(symphonyNumber);
                return;
            }

            // Lookups
            int clientId = GetOrCreateLookupId(_clientCache, "ClientName", "ClientName", clientName, conn, transaction);
            int marketSectorId = GetOrCreateLookupId(_marketSectorCache, "MarketSector", "SectorName", marketSector, conn, transaction);

            int projectTypeId = 0;
            int subTypeId = 0;
            if (projectType.Length == 0)
            {
                warnings.Add("DC Technique Group ว่าง - ไม่ได้กำหนด Project Type/SubType");
            }
            else
            {
                projectTypeId = GetOrCreateLookupId(_projectTypeCache, "Project_Type", "Project_Type", projectType, conn, transaction);
                if (subProjectType.Length == 0)
                {
                    warnings.Add("DC Mode ว่าง - ไม่ได้กำหนด Project SubType");
                }
                else
                {
                    subTypeId = GetOrCreateSubTypeId(projectTypeId, subProjectType, conn, transaction);
                }
            }

            if (exists)
            {
                UpdateProject(conn, transaction, existing.Id, projectName, projectTypeId, subTypeId, marketSectorId, clientId,
                    startDate, endDate, status, projectOwner, userEmail);

                WriteUploadLog(conn, transaction, "Upload Update", existing.Id, symphonyNumber, projectName, status,
                    clientName, marketSector, projectType, subProjectType, startDate, endDate, projectOwner, userEmail);

                if (endDate.HasValue)
                {
                    DateTime? previousEnd = existing.EndDate;
                    existing.EndDate = endDate;
                    _rowUndo.Add(() => { existing.EndDate = previousEnd; });
                }
                _updatedCount++;
            }
            else
            {
                string formattedSym = ChangeSymNumber(symphonyNumber);
                if (formattedSym.Length == 0)
                {
                    throw new InvalidDataException("รูปแบบ Job ID ไม่ถูกต้อง (invalid Job ID format)");
                }

                int newId = InsertProject(conn, transaction, formattedSym, projectName, projectTypeId, subTypeId,
                    startDate, endDate, projectOwner, userEmail, status, clientId, marketSectorId);

                WriteUploadLog(conn, transaction, "Upload Insert", newId, formattedSym, projectName, status,
                    clientName, marketSector, projectType, subProjectType, startDate, endDate, projectOwner, userEmail);

                ExistingProject added = new ExistingProject();
                added.Id = newId;
                added.EndDate = endDate;
                _existingProjectsCache[symphonyKey] = added;
                _rowUndo.Add(() => { _existingProjectsCache.Remove(symphonyKey); });
                _insertedCount++;
            }
        }

        private static string ParseOwnerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains(","))
            {
                return string.Empty;
            }
            string[] parts = value.Split(',');
            string last = parts[0].Trim().Replace(" ", "");
            string first = parts[1].Trim().Replace(" ", "");
            if (last.Length == 0 || first.Length == 0)
            {
                return string.Empty;
            }
            return first + "." + last + "@ipsos.com";
        }

        private static string ParseProjectOwner(DataRow row)
        {
            string projectOwner = ParseOwnerName(GetCell(row, "Job Owner"));
            if (projectOwner.Length == 0)
            {
                projectOwner = ParseOwnerName(GetCell(row, "Job Manager"));
            }
            return projectOwner;
        }

        /// <summary>
        /// Gets (or creates with Status = 1) a lookup row. Table/column names are internal constants, never user input.
        /// </summary>
        private int GetOrCreateLookupId(Dictionary<string, int> cache, string table, string nameColumn, string value,
            SqlConnection conn, SqlTransaction transaction)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }
            string name = value.Trim();

            int id;
            if (cache.TryGetValue(name, out id))
            {
                return id;
            }

            string sql = "INSERT INTO [" + table + "] ([" + nameColumn + "], [Status]) OUTPUT INSERTED.ID VALUES (@Name, 1)";
            using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Name", name);
                int newId = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                cache[name] = newId;
                _rowUndo.Add(() => { cache.Remove(name); });
                return newId;
            }
        }

        private int GetOrCreateSubTypeId(int projectTypeId, string subType, SqlConnection conn, SqlTransaction transaction)
        {
            if (projectTypeId <= 0 || string.IsNullOrWhiteSpace(subType))
            {
                return 0;
            }
            string name = subType.Trim();
            string key = SubTypeKey(projectTypeId, name);

            int id;
            if (_subTypeCache.TryGetValue(key, out id))
            {
                return id;
            }

            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO [Project_SubType] ([Project_Type_ID], [SubType], [Status]) OUTPUT INSERTED.ID VALUES (@TypeId, @Name, 1)",
                conn, transaction))
            {
                cmd.Parameters.AddWithValue("@TypeId", projectTypeId);
                cmd.Parameters.AddWithValue("@Name", name);
                int newId = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                _subTypeCache[key] = newId;
                _rowUndo.Add(() => { _subTypeCache.Remove(key); });
                return newId;
            }
        }

        private static void UpdateProject(SqlConnection conn, SqlTransaction transaction, int projectId, string projectName,
            int projectTypeId, int subTypeId, int marketSectorId, int clientId, DateTime? startDate, DateTime? endDate,
            string status, string projectOwner, string modifiedBy)
        {
            // Blank / unresolved values in the file keep the current database value.
            const string sql = @"UPDATE [Project] SET
                [Project_Name] = CASE WHEN @ProjectName <> N'' THEN @ProjectName ELSE [Project_Name] END,
                [Project_Type_ID] = CASE WHEN @TypeId > 0 THEN @TypeId ELSE [Project_Type_ID] END,
                [Project_SubType_ID] = CASE WHEN @SubTypeId > 0 THEN @SubTypeId
                                            WHEN @TypeId > 0 AND ISNULL([Project_Type_ID], 0) <> @TypeId THEN 0
                                            ELSE [Project_SubType_ID] END,
                [MarketSector_ID] = CASE WHEN @MarketSectorId > 0 THEN @MarketSectorId ELSE [MarketSector_ID] END,
                [ClientName_ID] = CASE WHEN @ClientId > 0 THEN @ClientId ELSE [ClientName_ID] END,
                [Project_StartDate] = COALESCE(@StartDate, [Project_StartDate]),
                [Project_EndDate] = COALESCE(@EndDate, [Project_EndDate]),
                [Status] = CASE WHEN @Status <> N'' THEN @Status ELSE [Status] END,
                [Project_Owner] = CASE WHEN @ProjectOwner <> N'' THEN @ProjectOwner ELSE [Project_Owner] END,
                [Modified_By] = @ModifiedBy,
                [Modified_DateTime] = @ModifiedDateTime
                WHERE ID = @Id";

            using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@ProjectName", projectName ?? string.Empty);
                cmd.Parameters.AddWithValue("@TypeId", projectTypeId);
                cmd.Parameters.AddWithValue("@SubTypeId", subTypeId);
                cmd.Parameters.AddWithValue("@MarketSectorId", marketSectorId);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.HasValue ? (object)startDate.Value : DBNull.Value;
                cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.HasValue ? (object)endDate.Value : DBNull.Value;
                cmd.Parameters.AddWithValue("@Status", status ?? string.Empty);
                cmd.Parameters.AddWithValue("@ProjectOwner", projectOwner ?? string.Empty);
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy ?? string.Empty);
                cmd.Parameters.AddWithValue("@ModifiedDateTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@Id", projectId);

                if (cmd.ExecuteNonQuery() == 0)
                {
                    throw new InvalidOperationException("Project ID " + projectId.ToString(CultureInfo.InvariantCulture) + " not found for update");
                }
            }
        }

        private static int InsertProject(SqlConnection conn, SqlTransaction transaction, string symphonyNumber,
            string projectName, int projectTypeId, int subTypeId, DateTime? startDate, DateTime? endDate,
            string projectOwner, string creator, string status, int clientId, int marketSectorId)
        {
            const string sql = @"INSERT INTO [Project]
                ([Symphony_Number], [Project_Name], [Project_Type_ID], [Project_SubType_ID],
                 [Project_StartDate], [Project_EndDate], [Project_Owner], [Creator],
                 [Created_DateTime], [Status], [ClientName_ID], [MarketSector_ID])
                OUTPUT INSERTED.ID
                VALUES
                (@SymphonyNumber, @ProjectName, @ProjectTypeId, @SubTypeId,
                 @StartDate, @EndDate, @ProjectOwner, @Creator,
                 @CreatedDateTime, @Status, @ClientId, @MarketSectorId)";

            using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@SymphonyNumber", symphonyNumber);
                cmd.Parameters.AddWithValue("@ProjectName", projectName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ProjectTypeId", projectTypeId);
                cmd.Parameters.AddWithValue("@SubTypeId", subTypeId);
                cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.HasValue ? (object)startDate.Value : DBNull.Value;
                cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.HasValue ? (object)endDate.Value : DBNull.Value;
                cmd.Parameters.AddWithValue("@ProjectOwner", projectOwner ?? string.Empty);
                cmd.Parameters.AddWithValue("@Creator", creator ?? string.Empty);
                cmd.Parameters.AddWithValue("@CreatedDateTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@Status", status ?? string.Empty);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@MarketSectorId", marketSectorId);
                return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private void WriteUploadLog(SqlConnection conn, SqlTransaction transaction, string task, int projectId,
            string symphonyNumber, string projectName, string status, string clientName, string marketSector,
            string projectType, string subType, DateTime? startDate, DateTime? endDate, string projectOwner, string userEmail)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ProjectID=").Append(projectId.ToString(CultureInfo.InvariantCulture)).Append(";");
            sb.Append("Symphony_Number=").Append(symphonyNumber).Append(";");
            sb.Append("Project_Name=").Append(projectName).Append(";");
            sb.Append("Status=").Append(status).Append(";");
            sb.Append("Client=").Append(clientName).Append(";");
            sb.Append("MarketSector=").Append(marketSector).Append(";");
            sb.Append("Type=").Append(projectType).Append(";");
            sb.Append("SubType=").Append(subType).Append(";");
            sb.Append("StartDate=").Append(FormatDate(startDate)).Append(";");
            sb.Append("EndDate=").Append(FormatDate(endDate)).Append(";");
            sb.Append("Owner=").Append(projectOwner).Append(";");

            string data = sb.ToString();
            if (data.Length > 4000)
            {
                data = data.Substring(0, 4000);
            }

            using (SqlCommand cmd = new SqlCommand(_sql.Insert_Log, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Action_on_Table", "Project");
                cmd.Parameters.AddWithValue("@Task", task);
                cmd.Parameters.AddWithValue("@Data", data);
                cmd.Parameters.AddWithValue("@Action_By", userEmail ?? string.Empty);
                cmd.Parameters.AddWithValue("@Action_DateTime", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
