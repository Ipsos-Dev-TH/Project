using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class Edit : System.Web.UI.Page
    {
        private const int ParentColumnIndex = 2;

        private MasterTable _table;
        private DataTable _parentTypes;

        private string CurrentEmail
        {
            get { return AppAuth.CurrentEmail; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AppAuth.IsAdmin)
            {
                Response.Redirect("~/", true);
                return;
            }

            // The table always comes from the query string (validated against the whitelist), never from Session,
            // so several tabs can edit different tables at the same time.
            _table = MasterTable.Find(Request.QueryString["data"]);
            if (_table == null)
            {
                Response.Redirect("~/Admin/Default.aspx", true);
                return;
            }

            Page.Title = _table.Title;
            LblTitle.Text = HttpUtility.HtmlEncode("Manage " + _table.Title);

            bool hasParent = _table.HasParent;
            PnlAddParent.Visible = hasParent;
            PnlParentFilter.Visible = hasParent;
            GridData.Columns[ParentColumnIndex].Visible = hasParent;
            LitMergeNote.Text = _table.Key == "Project_Type"
                ? "Sub types of the source type are moved to the target type as well."
                : (hasParent ? "Source and target must belong to the same project type." : string.Empty);

            if (!IsPostBack)
            {
                try
                {
                    if (hasParent)
                    {
                        BindParentLists();
                    }
                    BindGrid();
                    BindMergeLists();
                }
                catch (Exception ex)
                {
                    AdminDb.ShowError(LblMessage, ex);
                }
            }
        }

        #region Data loading

        private DataTable ParentTypes
        {
            get
            {
                if (_parentTypes == null)
                {
                    _parentTypes = AdminDb.Query(
                        "SELECT ID, Project_Type, CAST(ISNULL([Status], 0) AS bit) AS IsActive FROM [dbo].[Project_Type] ORDER BY Project_Type");
                }
                return _parentTypes;
            }
        }

        private void BindParentLists()
        {
            DdlAddParent.Items.Clear();
            DdlAddParent.Items.Add(new ListItem("-- Select project type --", string.Empty));
            DdlParentFilter.Items.Clear();
            DdlParentFilter.Items.Add(new ListItem("All project types", string.Empty));

            foreach (DataRow row in ParentTypes.Rows)
            {
                string id = row["ID"].ToString();
                string name = AdminDb.ToText(row["Project_Type"]);
                bool active = Convert.ToBoolean(row["IsActive"]);
                if (active)
                {
                    DdlAddParent.Items.Add(new ListItem(name, id));
                }
                DdlParentFilter.Items.Add(new ListItem(active ? name : name + " (inactive)", id));
            }

            // Backwards compatible with the old hub link: Edit.aspx?data=Project_SubType&TypeID=<id>
            string typeId = Request.QueryString["TypeID"];
            if (!string.IsNullOrEmpty(typeId))
            {
                if (DdlParentFilter.Items.FindByValue(typeId) != null)
                {
                    DdlParentFilter.SelectedValue = typeId;
                }
                if (DdlAddParent.Items.FindByValue(typeId) != null)
                {
                    DdlAddParent.SelectedValue = typeId;
                }
            }
        }

        private string BaseSelect()
        {
            var sql = new StringBuilder();
            sql.Append("SELECT t.ID, t.[").Append(_table.NameColumn).Append("] AS Name, ");
            sql.Append("CAST(ISNULL(t.[Status], 0) AS bit) AS IsActive, ISNULL(pc.Cnt, 0) AS ProjectCount, ");
            if (_table.HasParent)
            {
                sql.Append("t.Project_Type_ID AS ParentID, pt.Project_Type AS ParentName ");
            }
            else
            {
                sql.Append("CAST(NULL AS int) AS ParentID, CAST(NULL AS nvarchar(1)) AS ParentName ");
            }
            sql.Append("FROM [dbo].[").Append(_table.Key).Append("] t ");
            sql.Append("LEFT JOIN (SELECT [").Append(_table.ProjectFk).Append("] AS RefID, COUNT(*) AS Cnt FROM [dbo].[Project] ");
            sql.Append("WHERE Is_Deleted = 0 GROUP BY [").Append(_table.ProjectFk).Append("]) pc ON pc.RefID = t.ID ");
            if (_table.HasParent)
            {
                sql.Append("LEFT JOIN [dbo].[Project_Type] pt ON pt.ID = t.Project_Type_ID ");
            }
            return sql.ToString();
        }

        private void BindGrid()
        {
            var sql = new StringBuilder(BaseSelect());
            var parameters = new List<SqlParameter>();
            sql.Append("WHERE 1 = 1 ");

            switch (DdlStatusFilter.SelectedValue)
            {
                case "Inactive":
                    sql.Append("AND ISNULL(t.[Status], 0) = 0 ");
                    break;
                case "All":
                    break;
                default:
                    sql.Append("AND t.[Status] = 1 ");
                    break;
            }

            string search = TxtSearch.Text.Trim();
            if (search.Length > 0)
            {
                sql.Append("AND t.[").Append(_table.NameColumn).Append("] LIKE @Search ");
                parameters.Add(AdminDb.P("@Search", AdminDb.LikeContains(search)));
            }

            int parentId;
            if (_table.HasParent && int.TryParse(DdlParentFilter.SelectedValue, out parentId))
            {
                sql.Append("AND t.Project_Type_ID = @ParentId ");
                parameters.Add(AdminDb.P("@ParentId", parentId));
            }

            sql.Append(_table.HasParent ? "ORDER BY pt.Project_Type, Name, t.ID" : "ORDER BY Name, t.ID");

            DataTable dt = AdminDb.Query(sql.ToString(), parameters.ToArray());
            LblCount.Text = dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture) + " record(s)";

            int pageCount = (dt.Rows.Count + GridData.PageSize - 1) / GridData.PageSize;
            if (GridData.PageIndex >= pageCount)
            {
                GridData.PageIndex = Math.Max(0, pageCount - 1);
            }

            GridData.DataSource = dt;
            GridData.DataBind();
        }

        private void BindMergeLists()
        {
            DataTable dt = AdminDb.Query(BaseSelect() + (_table.HasParent ? "ORDER BY pt.Project_Type, Name, t.ID" : "ORDER BY Name, t.ID"));

            DdlMergeSource.Items.Clear();
            DdlMergeTarget.Items.Clear();
            DdlMergeSource.Items.Add(new ListItem("-- Select source --", string.Empty));
            DdlMergeTarget.Items.Add(new ListItem("-- Select target --", string.Empty));

            foreach (DataRow row in dt.Rows)
            {
                bool active = Convert.ToBoolean(row["IsActive"]);
                var text = new StringBuilder();
                if (_table.HasParent)
                {
                    text.Append(AdminDb.ToText(row["ParentName"])).Append(" / ");
                }
                text.Append(AdminDb.ToText(row["Name"]));
                text.Append(" [ID ").Append(row["ID"]).Append("]");
                text.Append(" - ").Append(row["ProjectCount"]).Append(" project(s)");
                if (!active)
                {
                    text.Append(" (inactive)");
                }

                string id = row["ID"].ToString();
                DdlMergeSource.Items.Add(new ListItem(text.ToString(), id));
                if (active)
                {
                    DdlMergeTarget.Items.Add(new ListItem(text.ToString(), id));
                }
            }
        }

        private void RefreshAll()
        {
            BindGrid();
            BindMergeLists();
        }

        /// <summary>Returns the first row with the same trimmed, case-insensitive name (and parent for sub types), or null.</summary>
        private DataRow FindDuplicate(SqlConnection conn, SqlTransaction tx, string name, int? parentId, int? excludeId)
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            sql.Append("SELECT TOP 1 ID, CAST(ISNULL([Status], 0) AS bit) AS IsActive FROM [dbo].[").Append(_table.Key).Append("] ");
            sql.Append("WHERE LOWER(LTRIM(RTRIM([").Append(_table.NameColumn).Append("]))) = LOWER(@Name) ");
            parameters.Add(AdminDb.P("@Name", name));
            if (_table.HasParent && parentId.HasValue)
            {
                sql.Append("AND Project_Type_ID = @ParentId ");
                parameters.Add(AdminDb.P("@ParentId", parentId.Value));
            }
            if (excludeId.HasValue)
            {
                sql.Append("AND ID <> @ExcludeId ");
                parameters.Add(AdminDb.P("@ExcludeId", excludeId.Value));
            }

            DataTable dt = AdminDb.QueryTx(conn, tx, sql.ToString(), parameters.ToArray());
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private string DuplicateMessage(string name, DataRow duplicate)
        {
            bool active = Convert.ToBoolean(duplicate["IsActive"]);
            string msg = "\"" + name + "\" already exists (ID " + duplicate["ID"] + ")";
            return active
                ? msg + ". ไม่สามารถบันทึกชื่อซ้ำได้"
                : msg + " but is inactive. Use the Inactive filter and click Reactivate instead.";
        }

        private static string NormalizeName(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        #endregion

        #region Add

        protected void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = NormalizeName(TxtAddName.Text);
            if (name.Length == 0)
            {
                AdminDb.ShowMessage(LblMessage, "Please enter a name.", "warning");
                return;
            }

            int? parentId = null;
            if (_table.HasParent)
            {
                int parsed;
                if (!int.TryParse(DdlAddParent.SelectedValue, out parsed) || parsed <= 0)
                {
                    AdminDb.ShowMessage(LblMessage, "Please select a project type.", "warning");
                    return;
                }
                parentId = parsed;
            }

            try
            {
                int newId;
                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    if (_table.HasParent)
                    {
                        object exists = AdminDb.ScalarTx(conn, tx,
                            "SELECT COUNT(*) FROM [dbo].[Project_Type] WHERE ID = @ParentId AND [Status] = 1",
                            AdminDb.P("@ParentId", parentId.Value));
                        if (Convert.ToInt32(exists) == 0)
                        {
                            AdminDb.ShowMessage(LblMessage, "The selected project type is not active.", "warning");
                            return;
                        }
                    }

                    DataRow duplicate = FindDuplicate(conn, tx, name, parentId, null);
                    if (duplicate != null)
                    {
                        AdminDb.ShowMessage(LblMessage, DuplicateMessage(name, duplicate), "warning");
                        return;
                    }

                    string sql = _table.HasParent
                        ? "INSERT INTO [dbo].[Project_SubType] ([SubType], [Project_Type_ID], [Status]) VALUES (@Name, @ParentId, 1); SELECT CAST(SCOPE_IDENTITY() AS int);"
                        : "INSERT INTO [dbo].[" + _table.Key + "] ([" + _table.NameColumn + "], [Status]) VALUES (@Name, 1); SELECT CAST(SCOPE_IDENTITY() AS int);";

                    var parameters = new List<SqlParameter> { AdminDb.P("@Name", name) };
                    if (_table.HasParent)
                    {
                        parameters.Add(AdminDb.P("@ParentId", parentId.Value));
                    }
                    newId = Convert.ToInt32(AdminDb.ScalarTx(conn, tx, sql, parameters.ToArray()));

                    string logData = "ID=" + newId + ";" + _table.NameColumn + "=" + name + ";Status=1";
                    if (_table.HasParent)
                    {
                        logData += ";Project_Type_ID=" + parentId.Value;
                    }
                    AdminDb.LogTx(conn, tx, _table.Key, "Insert", logData, CurrentEmail);
                    tx.Commit();
                }

                TxtAddName.Text = string.Empty;
                AdminDb.ShowMessage(LblMessage, "Added \"" + name + "\" (ID " + newId + ").", "success");
                RefreshAll();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        #endregion

        #region Filter

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            GridData.PageIndex = 0;
            GridData.EditIndex = -1;
            SafeBindGrid();
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            DdlStatusFilter.SelectedValue = "Active";
            TxtSearch.Text = string.Empty;
            if (DdlParentFilter.Items.Count > 0)
            {
                DdlParentFilter.SelectedIndex = 0;
            }
            GridData.PageIndex = 0;
            GridData.EditIndex = -1;
            SafeBindGrid();
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

        #endregion

        #region Grid

        protected void GridData_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridData.PageIndex = e.NewPageIndex;
            GridData.EditIndex = -1;
            SafeBindGrid();
        }

        protected void GridData_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridData.EditIndex = e.NewEditIndex;
            SafeBindGrid();
        }

        protected void GridData_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            e.Cancel = true;
            GridData.EditIndex = -1;
            SafeBindGrid();
        }

        protected void GridData_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow || !_table.HasParent)
            {
                return;
            }
            if ((e.Row.RowState & DataControlRowState.Edit) == 0)
            {
                return;
            }

            var ddl = e.Row.FindControl("DdlEditParent") as DropDownList;
            if (ddl == null)
            {
                return;
            }

            var data = (DataRowView)e.Row.DataItem;
            string currentParent = AdminDb.ToText(data["ParentID"]);

            ddl.Items.Clear();
            foreach (DataRow row in ParentTypes.Rows)
            {
                string id = row["ID"].ToString();
                bool active = Convert.ToBoolean(row["IsActive"]);
                if (active || id == currentParent)
                {
                    string name = AdminDb.ToText(row["Project_Type"]);
                    ddl.Items.Add(new ListItem(active ? name : name + " (inactive)", id));
                }
            }
            if (ddl.Items.FindByValue(currentParent) != null)
            {
                ddl.SelectedValue = currentParent;
            }
        }

        protected void GridData_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            e.Cancel = true;
            try
            {
                int id = Convert.ToInt32(GridData.DataKeys[e.RowIndex].Value);
                GridViewRow gridRow = GridData.Rows[e.RowIndex];
                var txtName = gridRow.FindControl("TxtEditName") as TextBox;
                string newName = NormalizeName(txtName == null ? null : txtName.Text);
                if (newName.Length == 0)
                {
                    AdminDb.ShowMessage(LblMessage, "Name cannot be empty.", "warning");
                    return;
                }

                int? newParent = null;
                if (_table.HasParent)
                {
                    var ddlParent = gridRow.FindControl("DdlEditParent") as DropDownList;
                    int parsed;
                    if (ddlParent == null || !int.TryParse(ddlParent.SelectedValue, out parsed))
                    {
                        AdminDb.ShowMessage(LblMessage, "Please select a project type.", "warning");
                        return;
                    }
                    newParent = parsed;
                }

                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    string selectSql = "SELECT [" + _table.NameColumn + "] AS Name"
                        + (_table.HasParent ? ", Project_Type_ID AS ParentID" : string.Empty)
                        + " FROM [dbo].[" + _table.Key + "] WITH (UPDLOCK) WHERE ID = @Id";
                    DataTable current = AdminDb.QueryTx(conn, tx, selectSql, AdminDb.P("@Id", id));
                    if (current.Rows.Count == 0)
                    {
                        AdminDb.ShowMessage(LblMessage, "This record no longer exists.", "warning");
                        GridData.EditIndex = -1;
                        RefreshAll();
                        return;
                    }

                    string oldName = AdminDb.ToText(current.Rows[0]["Name"]);
                    string oldParent = _table.HasParent ? AdminDb.ToText(current.Rows[0]["ParentID"]) : string.Empty;
                    bool nameChanged = !string.Equals(oldName, newName, StringComparison.Ordinal);
                    bool parentChanged = _table.HasParent && oldParent != newParent.Value.ToString(CultureInfo.InvariantCulture);

                    if (!nameChanged && !parentChanged)
                    {
                        GridData.EditIndex = -1;
                        AdminDb.ShowMessage(LblMessage, "No changes to save.", "info");
                        BindGrid();
                        return;
                    }

                    DataRow duplicate = FindDuplicate(conn, tx, newName, newParent, id);
                    if (duplicate != null)
                    {
                        AdminDb.ShowMessage(LblMessage, DuplicateMessage(newName, duplicate), "warning");
                        return;
                    }

                    var parameters = new List<SqlParameter> { AdminDb.P("@Name", newName), AdminDb.P("@Id", id) };
                    string updateSql = "UPDATE [dbo].[" + _table.Key + "] SET [" + _table.NameColumn + "] = @Name";
                    if (_table.HasParent)
                    {
                        updateSql += ", Project_Type_ID = @ParentId";
                        parameters.Add(AdminDb.P("@ParentId", newParent.Value));
                    }
                    updateSql += " WHERE ID = @Id";
                    AdminDb.ExecuteTx(conn, tx, updateSql, parameters.ToArray());

                    var logData = new StringBuilder("ID=" + id + ";");
                    if (nameChanged)
                    {
                        logData.Append(_table.NameColumn).Append("[").Append(oldName).Append(" -> ").Append(newName).Append("];");
                    }
                    if (parentChanged)
                    {
                        logData.Append("Project_Type_ID[").Append(oldParent).Append(" -> ").Append(newParent.Value).Append("];");
                    }
                    AdminDb.LogTx(conn, tx, _table.Key, "Update", logData.ToString(), CurrentEmail);
                    tx.Commit();
                }

                GridData.EditIndex = -1;
                AdminDb.ShowMessage(LblMessage, "Saved \"" + newName + "\".", "success");
                RefreshAll();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        protected void GridData_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            bool deactivate = e.CommandName == "Deactivate";
            bool reactivate = e.CommandName == "Reactivate";
            if (!deactivate && !reactivate)
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
                string name;
                int projectCount;
                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    DataTable current = AdminDb.QueryTx(conn, tx,
                        "SELECT [" + _table.NameColumn + "] AS Name, CAST(ISNULL([Status], 0) AS bit) AS IsActive"
                        + (_table.HasParent ? ", Project_Type_ID AS ParentID" : string.Empty)
                        + " FROM [dbo].[" + _table.Key + "] WITH (UPDLOCK) WHERE ID = @Id",
                        AdminDb.P("@Id", id));
                    if (current.Rows.Count == 0)
                    {
                        AdminDb.ShowMessage(LblMessage, "This record no longer exists.", "warning");
                        RefreshAll();
                        return;
                    }

                    name = AdminDb.ToText(current.Rows[0]["Name"]);
                    bool isActive = Convert.ToBoolean(current.Rows[0]["IsActive"]);
                    if (isActive == reactivate)
                    {
                        AdminDb.ShowMessage(LblMessage, "\"" + name + "\" is already " + (isActive ? "active" : "inactive") + ".", "info");
                        RefreshAll();
                        return;
                    }

                    if (reactivate)
                    {
                        int? parentId = null;
                        if (_table.HasParent && current.Rows[0]["ParentID"] != DBNull.Value)
                        {
                            parentId = Convert.ToInt32(current.Rows[0]["ParentID"]);
                        }
                        DataRow duplicate = FindDuplicate(conn, tx, NormalizeName(name), parentId, id);
                        if (duplicate != null && Convert.ToBoolean(duplicate["IsActive"]))
                        {
                            AdminDb.ShowMessage(LblMessage,
                                "Cannot reactivate: an active item with the same name exists (ID " + duplicate["ID"] + "). Merge them instead.",
                                "warning");
                            return;
                        }
                    }

                    AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[" + _table.Key + "] SET [Status] = @Status WHERE ID = @Id",
                        AdminDb.P("@Status", reactivate),
                        AdminDb.P("@Id", id));

                    projectCount = Convert.ToInt32(AdminDb.ScalarTx(conn, tx,
                        "SELECT COUNT(*) FROM [dbo].[Project] WHERE [" + _table.ProjectFk + "] = @Id AND Is_Deleted = 0",
                        AdminDb.P("@Id", id)));

                    string logData = "ID=" + id + ";" + _table.NameColumn + "=" + name
                        + (reactivate ? ";Status[0 -> 1]" : ";Status[1 -> 0]")
                        + ";ActiveProjects=" + projectCount;
                    AdminDb.LogTx(conn, tx, _table.Key, reactivate ? "Reactivate" : "Deactivate", logData, CurrentEmail);
                    tx.Commit();
                }

                string message = (reactivate ? "Reactivated \"" : "Deactivated \"") + name + "\".";
                if (deactivate && projectCount > 0)
                {
                    message += " " + projectCount + " active project(s) still use it; consider merging it into another item.";
                }
                AdminDb.ShowMessage(LblMessage, message, "success");
                GridData.EditIndex = -1;
                RefreshAll();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        #endregion

        #region Merge

        protected void BtnMerge_Click(object sender, EventArgs e)
        {
            int sourceId;
            int targetId;
            if (!int.TryParse(DdlMergeSource.SelectedValue, out sourceId) || !int.TryParse(DdlMergeTarget.SelectedValue, out targetId))
            {
                AdminDb.ShowMessage(LblMessage, "Please select both a source and a target.", "warning");
                return;
            }
            if (sourceId == targetId)
            {
                AdminDb.ShowMessage(LblMessage, "Source and target must be different items (cannot merge an item into itself).", "warning");
                return;
            }

            try
            {
                string sourceName;
                string targetName;
                int projectsMoved;
                int subTypesMoved = 0;

                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    DataTable rows = AdminDb.QueryTx(conn, tx,
                        "SELECT ID, [" + _table.NameColumn + "] AS Name, CAST(ISNULL([Status], 0) AS bit) AS IsActive"
                        + (_table.HasParent ? ", Project_Type_ID AS ParentID" : ", CAST(NULL AS int) AS ParentID")
                        + " FROM [dbo].[" + _table.Key + "] WITH (UPDLOCK) WHERE ID IN (@SourceId, @TargetId)",
                        AdminDb.P("@SourceId", sourceId),
                        AdminDb.P("@TargetId", targetId));

                    DataRow source = null;
                    DataRow target = null;
                    foreach (DataRow row in rows.Rows)
                    {
                        int id = Convert.ToInt32(row["ID"]);
                        if (id == sourceId)
                        {
                            source = row;
                        }
                        else if (id == targetId)
                        {
                            target = row;
                        }
                    }

                    if (source == null || target == null)
                    {
                        AdminDb.ShowMessage(LblMessage, "The selected source or target no longer exists.", "warning");
                        RefreshAll();
                        return;
                    }
                    if (!Convert.ToBoolean(target["IsActive"]))
                    {
                        AdminDb.ShowMessage(LblMessage, "The target must be active. Reactivate it first.", "warning");
                        return;
                    }
                    if (_table.HasParent && AdminDb.ToText(source["ParentID"]) != AdminDb.ToText(target["ParentID"]))
                    {
                        AdminDb.ShowMessage(LblMessage, "Source and target sub types belong to different project types and cannot be merged.", "warning");
                        return;
                    }

                    sourceName = AdminDb.ToText(source["Name"]);
                    targetName = AdminDb.ToText(target["Name"]);

                    projectsMoved = AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[Project] SET [" + _table.ProjectFk + "] = @TargetId, Modified_By = @By, Modified_DateTime = GETDATE() WHERE [" + _table.ProjectFk + "] = @SourceId",
                        AdminDb.P("@TargetId", targetId),
                        AdminDb.P("@By", CurrentEmail),
                        AdminDb.P("@SourceId", sourceId));

                    if (_table.Key == "Project_Type")
                    {
                        subTypesMoved = AdminDb.ExecuteTx(conn, tx,
                            "UPDATE [dbo].[Project_SubType] SET Project_Type_ID = @TargetId WHERE Project_Type_ID = @SourceId",
                            AdminDb.P("@TargetId", targetId),
                            AdminDb.P("@SourceId", sourceId));
                    }

                    AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[" + _table.Key + "] SET [Status] = 0 WHERE ID = @SourceId",
                        AdminDb.P("@SourceId", sourceId));

                    string logData = "SourceID=" + sourceId + " (" + sourceName + ");TargetID=" + targetId + " (" + targetName + ")"
                        + ";ProjectsMoved=" + projectsMoved
                        + (_table.Key == "Project_Type" ? ";SubTypesMoved=" + subTypesMoved : string.Empty)
                        + ";Source Status[" + (Convert.ToBoolean(source["IsActive"]) ? "1" : "0") + " -> 0]";
                    AdminDb.LogTx(conn, tx, _table.Key, "Merge", logData, CurrentEmail);
                    tx.Commit();
                }

                string message = "Merged \"" + sourceName + "\" into \"" + targetName + "\": " + projectsMoved + " project(s) moved";
                if (_table.Key == "Project_Type")
                {
                    message += ", " + subTypesMoved + " sub type(s) moved (check the target type for duplicate sub types)";
                }
                AdminDb.ShowMessage(LblMessage, message + ".", "success");
                GridData.EditIndex = -1;
                RefreshAll();
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        #endregion
    }
}
