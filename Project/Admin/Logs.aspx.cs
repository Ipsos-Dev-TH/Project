using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class Logs : System.Web.UI.Page
    {
        private const int MaxRows = 500;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AppAuth.IsAdmin)
            {
                Response.Redirect("~/", true);
                return;
            }

            if (!IsPostBack)
            {
                try
                {
                    BindFilterList(DdlTable, "Action_on_Table", "All tables");
                    BindFilterList(DdlTask, "Task", "All tasks");

                    // Optional deep link, e.g. Logs.aspx?text=ProjectID=123;
                    string text = Request.QueryString["text"];
                    if (!string.IsNullOrEmpty(text))
                    {
                        TxtText.Text = text;
                    }

                    BindGrid();
                }
                catch (Exception ex)
                {
                    AdminDb.ShowError(LblMessage, ex);
                }
            }
        }

        /// <summary>column is one of the hard-coded names Action_on_Table / Task.</summary>
        private static void BindFilterList(DropDownList ddl, string column, string allText)
        {
            DataTable dt = AdminDb.Query(
                "SELECT DISTINCT [" + column + "] AS Value FROM [dbo].[Logs] WHERE [" + column + "] IS NOT NULL AND [" + column + "] <> '' ORDER BY [" + column + "]");
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem(allText, string.Empty));
            foreach (DataRow row in dt.Rows)
            {
                string value = AdminDb.ToText(row["Value"]);
                ddl.Items.Add(new ListItem(value, value));
            }
        }

        private static bool TryParseDate(string text, out DateTime value)
        {
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };
            return DateTime.TryParseExact((text ?? string.Empty).Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }

        private void BindGrid()
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter> { AdminDb.P("@Max", MaxRows) };
            sql.Append("SELECT TOP (@Max) Action_DateTime, Action_on_Table, Task, Action_By, Data FROM [dbo].[Logs] WHERE 1 = 1 ");

            if (DdlTable.SelectedValue.Length > 0)
            {
                sql.Append("AND Action_on_Table = @Table ");
                parameters.Add(AdminDb.P("@Table", DdlTable.SelectedValue));
            }
            if (DdlTask.SelectedValue.Length > 0)
            {
                sql.Append("AND Task = @Task ");
                parameters.Add(AdminDb.P("@Task", DdlTask.SelectedValue));
            }

            string user = TxtUser.Text.Trim();
            if (user.Length > 0)
            {
                sql.Append("AND Action_By LIKE @User ");
                parameters.Add(AdminDb.P("@User", AdminDb.LikeContains(user)));
            }

            string text = TxtText.Text.Trim();
            if (text.Length > 0)
            {
                sql.Append("AND Data LIKE @Text ");
                parameters.Add(AdminDb.P("@Text", AdminDb.LikeContains(text)));
            }

            DateTime fromDate;
            if (TxtFrom.Text.Trim().Length > 0)
            {
                if (!TryParseDate(TxtFrom.Text, out fromDate))
                {
                    AdminDb.ShowMessage(LblMessage, "Invalid 'From' date.", "warning");
                    return;
                }
                sql.Append("AND Action_DateTime >= @From ");
                parameters.Add(AdminDb.P("@From", fromDate.Date));
            }

            DateTime toDate;
            if (TxtTo.Text.Trim().Length > 0)
            {
                if (!TryParseDate(TxtTo.Text, out toDate))
                {
                    AdminDb.ShowMessage(LblMessage, "Invalid 'To' date.", "warning");
                    return;
                }
                sql.Append("AND Action_DateTime < @To ");
                parameters.Add(AdminDb.P("@To", toDate.Date.AddDays(1)));
            }

            sql.Append("ORDER BY Action_DateTime DESC");

            DataTable dt = AdminDb.Query(sql.ToString(), parameters.ToArray());
            LblSummary.Text = dt.Rows.Count >= MaxRows
                ? "showing the newest " + MaxRows + " entries - refine the filters"
                : dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture) + " entr" + (dt.Rows.Count == 1 ? "y" : "ies");

            int pageCount = (dt.Rows.Count + GridLogs.PageSize - 1) / GridLogs.PageSize;
            if (GridLogs.PageIndex >= pageCount)
            {
                GridLogs.PageIndex = Math.Max(0, pageCount - 1);
            }
            GridLogs.DataSource = dt;
            GridLogs.DataBind();
        }

        private void SafeBindGrid()
        {
            try
            {
                BindGrid();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        protected void BtnSearch_Click(object sender, EventArgs e)
        {
            GridLogs.PageIndex = 0;
            SafeBindGrid();
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            DdlTable.SelectedIndex = 0;
            DdlTask.SelectedIndex = 0;
            TxtUser.Text = string.Empty;
            TxtText.Text = string.Empty;
            TxtFrom.Text = string.Empty;
            TxtTo.Text = string.Empty;
            GridLogs.PageIndex = 0;
            SafeBindGrid();
        }

        protected void GridLogs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridLogs.PageIndex = e.NewPageIndex;
            SafeBindGrid();
        }
    }
}
