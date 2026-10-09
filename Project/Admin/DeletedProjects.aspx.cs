using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class DeletedProjects : System.Web.UI.Page
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
                SafeBindGrid();
            }
        }

        private void BindGrid()
        {
            var parameters = new List<SqlParameter> { AdminDb.P("@Max", MaxRows) };
            string sql = @"
SELECT TOP (@Max) p.ID, p.Symphony_Number, p.Project_Name, pt.Project_Type, cn.ClientName,
       p.[Status], p.Creator, p.Modified_By, p.Modified_DateTime
FROM [dbo].[Project] p
LEFT JOIN [dbo].[Project_Type] pt ON pt.ID = p.Project_Type_ID
LEFT JOIN [dbo].[ClientName] cn ON cn.ID = p.ClientName_ID
WHERE p.Is_Deleted = 1 ";

            string search = TxtSearch.Text.Trim();
            if (search.Length > 0)
            {
                sql += @"AND (p.Symphony_Number LIKE @Search OR p.Symphony_Key LIKE @SearchKey OR p.Project_Name LIKE @Search
     OR cn.ClientName LIKE @Search OR p.Modified_By LIKE @Search ";
                parameters.Add(AdminDb.P("@Search", AdminDb.LikeContains(search)));
                parameters.Add(AdminDb.P("@SearchKey", AdminDb.LikeContains(search.Replace("-", string.Empty))));

                int id;
                if (int.TryParse(search, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                {
                    sql += "OR p.ID = @SearchId ";
                    parameters.Add(AdminDb.P("@SearchId", id));
                }
                sql += ") ";
            }
            sql += "ORDER BY p.Modified_DateTime DESC, p.ID DESC";

            DataTable dt = AdminDb.Query(sql, parameters.ToArray());
            LblSummary.Text = dt.Rows.Count >= MaxRows
                ? "showing the latest " + MaxRows + " - refine the search"
                : dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture) + " project(s)";

            int pageCount = (dt.Rows.Count + GridDeleted.PageSize - 1) / GridDeleted.PageSize;
            if (GridDeleted.PageIndex >= pageCount)
            {
                GridDeleted.PageIndex = Math.Max(0, pageCount - 1);
            }
            GridDeleted.DataSource = dt;
            GridDeleted.DataBind();
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
            GridDeleted.PageIndex = 0;
            SafeBindGrid();
        }

        protected void GridDeleted_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridDeleted.PageIndex = e.NewPageIndex;
            SafeBindGrid();
        }

        protected void GridDeleted_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Restore")
            {
                return;
            }

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                return;
            }

            try
            {
                string symphonyNumber;
                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    DataTable current = AdminDb.QueryTx(conn, tx,
                        "SELECT ID, Symphony_Number, Symphony_Key, Is_Deleted FROM [dbo].[Project] WITH (UPDLOCK, HOLDLOCK) WHERE ID = @Id",
                        AdminDb.P("@Id", id));
                    if (current.Rows.Count == 0 || !Convert.ToBoolean(current.Rows[0]["Is_Deleted"]))
                    {
                        AdminDb.ShowMessage(LblMessage, "Project " + id + " is not in the deleted list any more.", "info");
                        BindGrid();
                        return;
                    }

                    symphonyNumber = AdminDb.ToText(current.Rows[0]["Symphony_Number"]);
                    object key = current.Rows[0]["Symphony_Key"];
                    if (key != DBNull.Value)
                    {
                        DataTable active = AdminDb.QueryTx(conn, tx,
                            "SELECT TOP 1 ID FROM [dbo].[Project] WITH (UPDLOCK, HOLDLOCK) WHERE Is_Deleted = 0 AND Symphony_Key = @Key AND ID <> @Id",
                            AdminDb.P("@Key", key),
                            AdminDb.P("@Id", id));
                        if (active.Rows.Count > 0)
                        {
                            AdminDb.ShowMessage(LblMessage,
                                "Cannot restore: active project " + active.Rows[0]["ID"] + " already uses Symphony Number " + symphonyNumber
                                + ". ไม่สามารถกู้คืนได้เพราะจะทำให้ Symphony Number ซ้ำ",
                                "warning");
                            return;
                        }
                    }

                    AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[Project] SET Is_Deleted = 0, Modified_By = @By, Modified_DateTime = GETDATE() WHERE ID = @Id",
                        AdminDb.P("@By", AppAuth.CurrentEmail),
                        AdminDb.P("@Id", id));
                    AdminDb.LogTx(conn, tx, "Project", "Restore",
                        "ProjectID=" + id + ";Symphony_Number=" + symphonyNumber + ";Is_Deleted[1 -> 0]",
                        AppAuth.CurrentEmail);
                    tx.Commit();
                }

                AdminDb.ShowMessage(LblMessage, "Restored project " + id + " (" + symphonyNumber + ").", "success");
                BindGrid();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }
    }
}
