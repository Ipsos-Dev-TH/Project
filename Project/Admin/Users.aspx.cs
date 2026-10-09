using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class Users : System.Web.UI.Page
    {
        private const string LogTable = "App_User";
        private static readonly Regex EmailPattern = new Regex(@"^[a-z0-9._%+\-']+@ipsos\.com$", RegexOptions.Compiled);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AppAuth.IsAdmin)
            {
                Response.Redirect("~/", true);
                return;
            }

            if (!IsPostBack)
            {
                DdlRole.Items.Clear();
                foreach (string role in AppAuth.AssignableRoles)
                {
                    DdlRole.Items.Add(new ListItem(role, role));
                }
                DdlRole.SelectedValue = AppAuth.RoleEditor;
                SafeBindGrid();
            }
        }

        private void BindGrid()
        {
            var parameters = new List<SqlParameter>();
            string sql = "SELECT ID, Email, [Role], CAST([Status] AS bit) AS IsActive, Created_By, Created_DateTime, Modified_By, Modified_DateTime FROM [dbo].[App_User] ";
            string search = TxtSearch.Text.Trim();
            if (search.Length > 0)
            {
                sql += "WHERE Email LIKE @Search ";
                parameters.Add(AdminDb.P("@Search", AdminDb.LikeContains(search)));
            }
            sql += "ORDER BY [Status] DESC, Email";

            DataTable dt = AdminDb.Query(sql, parameters.ToArray());
            int pageCount = (dt.Rows.Count + GridUsers.PageSize - 1) / GridUsers.PageSize;
            if (GridUsers.PageIndex >= pageCount)
            {
                GridUsers.PageIndex = Math.Max(0, pageCount - 1);
            }
            GridUsers.DataSource = dt;
            GridUsers.DataBind();
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

        private static bool IsSelf(string email)
        {
            return string.Equals((email ?? string.Empty).Trim(), AppAuth.CurrentEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        protected void BtnSearch_Click(object sender, EventArgs e)
        {
            GridUsers.PageIndex = 0;
            SafeBindGrid();
        }

        protected void GridUsers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridUsers.PageIndex = e.NewPageIndex;
            SafeBindGrid();
        }

        protected void GridUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var data = (DataRowView)e.Row.DataItem;
            string role = AdminDb.ToText(data["Role"]);
            bool self = IsSelf(AdminDb.ToText(data["Email"]));

            var ddl = e.Row.FindControl("DdlRowRole") as DropDownList;
            if (ddl != null)
            {
                ddl.Items.Clear();
                foreach (string r in AppAuth.AssignableRoles)
                {
                    ddl.Items.Add(new ListItem(r, r));
                }
                if (ddl.Items.FindByValue(role) != null)
                {
                    ddl.SelectedValue = role;
                }
                ddl.Enabled = !self;
            }

            var btnRole = e.Row.FindControl("BtnChangeRole") as Button;
            if (btnRole != null)
            {
                btnRole.Visible = !self;
            }

            var btnDisable = e.Row.FindControl("BtnDisable") as Button;
            if (btnDisable != null && self)
            {
                btnDisable.Visible = false;
            }
        }

        protected void BtnAddUser_Click(object sender, EventArgs e)
        {
            string email = (TxtEmail.Text ?? string.Empty).Trim().ToLowerInvariant();
            string role = DdlRole.SelectedValue;

            if (email.Length == 0)
            {
                AdminDb.ShowMessage(LblMessage, "Please enter an email address.", "warning");
                return;
            }
            if (!email.EndsWith("@ipsos.com", StringComparison.Ordinal) || !EmailPattern.IsMatch(email))
            {
                AdminDb.ShowMessage(LblMessage, "Email must be a valid @ipsos.com address (e.g. firstname.lastname@ipsos.com).", "warning");
                return;
            }
            if (!AppAuth.AssignableRoles.Contains(role))
            {
                AdminDb.ShowMessage(LblMessage, "Please select a valid role.", "warning");
                return;
            }

            try
            {
                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    object existing = AdminDb.ScalarTx(conn, tx,
                        "SELECT COUNT(*) FROM [dbo].[App_User] WITH (UPDLOCK, HOLDLOCK) WHERE Email = @Email",
                        AdminDb.P("@Email", email));
                    if (Convert.ToInt32(existing) > 0)
                    {
                        AdminDb.ShowMessage(LblMessage, email + " already exists. Change the role in the list below.", "warning");
                        return;
                    }

                    object newId = AdminDb.ScalarTx(conn, tx,
                        "INSERT INTO [dbo].[App_User] (Email, [Role], [Status], Created_By, Created_DateTime) VALUES (@Email, @Role, 1, @By, GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS int);",
                        AdminDb.P("@Email", email),
                        AdminDb.P("@Role", role),
                        AdminDb.P("@By", AppAuth.CurrentEmail));

                    AdminDb.LogTx(conn, tx, LogTable, "Insert",
                        "ID=" + Convert.ToString(newId) + ";Email=" + email + ";Role=" + role + ";Status=1", AppAuth.CurrentEmail);
                    tx.Commit();
                }

                TxtEmail.Text = string.Empty;
                AdminDb.ShowMessage(LblMessage, "Added " + email + " as " + role + ".", "success");
                SafeBindGrid();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        protected void GridUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string command = e.CommandName;
            if (command != "ChangeRole" && command != "DisableUser" && command != "EnableUser")
            {
                return;
            }

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                return;
            }

            string newRole = null;
            if (command == "ChangeRole")
            {
                var row = ((Control)e.CommandSource).NamingContainer as GridViewRow;
                var ddl = row == null ? null : row.FindControl("DdlRowRole") as DropDownList;
                newRole = ddl == null ? null : ddl.SelectedValue;
                if (newRole == null || !AppAuth.AssignableRoles.Contains(newRole))
                {
                    AdminDb.ShowMessage(LblMessage, "Please select a valid role.", "warning");
                    return;
                }
            }

            try
            {
                string message;
                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    DataTable current = AdminDb.QueryTx(conn, tx,
                        "SELECT Email, [Role], CAST([Status] AS bit) AS IsActive FROM [dbo].[App_User] WITH (UPDLOCK) WHERE ID = @Id",
                        AdminDb.P("@Id", id));
                    if (current.Rows.Count == 0)
                    {
                        AdminDb.ShowMessage(LblMessage, "This user no longer exists.", "warning");
                        BindGrid();
                        return;
                    }

                    string email = AdminDb.ToText(current.Rows[0]["Email"]);
                    string oldRole = AdminDb.ToText(current.Rows[0]["Role"]);
                    bool isActive = Convert.ToBoolean(current.Rows[0]["IsActive"]);

                    if (IsSelf(email))
                    {
                        AdminDb.ShowMessage(LblMessage, "You cannot change your own role or disable your own account. Ask another admin.", "warning");
                        return;
                    }

                    bool removesAdmin = isActive && oldRole == AppAuth.RoleAdmin
                        && (command == "DisableUser" || (command == "ChangeRole" && newRole != AppAuth.RoleAdmin));
                    if (removesAdmin)
                    {
                        int otherAdmins = Convert.ToInt32(AdminDb.ScalarTx(conn, tx,
                            "SELECT COUNT(*) FROM [dbo].[App_User] WHERE [Role] = 'Admin' AND [Status] = 1 AND ID <> @Id",
                            AdminDb.P("@Id", id)));
                        if (otherAdmins == 0)
                        {
                            AdminDb.ShowMessage(LblMessage, "At least one active admin must remain.", "warning");
                            return;
                        }
                    }

                    if (command == "ChangeRole")
                    {
                        if (newRole == oldRole)
                        {
                            AdminDb.ShowMessage(LblMessage, email + " already has the role " + oldRole + ".", "info");
                            return;
                        }
                        AdminDb.ExecuteTx(conn, tx,
                            "UPDATE [dbo].[App_User] SET [Role] = @Role, Modified_By = @By, Modified_DateTime = GETDATE() WHERE ID = @Id",
                            AdminDb.P("@Role", newRole),
                            AdminDb.P("@By", AppAuth.CurrentEmail),
                            AdminDb.P("@Id", id));
                        AdminDb.LogTx(conn, tx, LogTable, "ChangeRole",
                            "ID=" + id + ";Email=" + email + ";Role[" + oldRole + " -> " + newRole + "]", AppAuth.CurrentEmail);
                        message = email + " is now " + newRole + " (applies at next login).";
                    }
                    else
                    {
                        bool enable = command == "EnableUser";
                        if (enable == isActive)
                        {
                            AdminDb.ShowMessage(LblMessage, email + " is already " + (isActive ? "enabled" : "disabled") + ".", "info");
                            BindGrid();
                            return;
                        }
                        AdminDb.ExecuteTx(conn, tx,
                            "UPDATE [dbo].[App_User] SET [Status] = @Status, Modified_By = @By, Modified_DateTime = GETDATE() WHERE ID = @Id",
                            AdminDb.P("@Status", enable),
                            AdminDb.P("@By", AppAuth.CurrentEmail),
                            AdminDb.P("@Id", id));
                        AdminDb.LogTx(conn, tx, LogTable, enable ? "Enable" : "Disable",
                            "ID=" + id + ";Email=" + email + (enable ? ";Status[0 -> 1]" : ";Status[1 -> 0]"), AppAuth.CurrentEmail);
                        message = email + (enable ? " has been enabled." : " has been disabled (applies at next login).");
                    }

                    tx.Commit();
                }

                AdminDb.ShowMessage(LblMessage, message, "success");
                BindGrid();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }
    }
}
