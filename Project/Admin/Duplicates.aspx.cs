using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class Duplicates : System.Web.UI.Page
    {
        /// <summary>Columns that may be filled on the kept row from the other rows (whitelist).</summary>
        private static readonly string[] FillColumns =
        {
            "Project_Number", "Project_Name", "Project_Type_ID", "Product_Type_ID",
            "Project_StartDate", "Project_EndDate", "ClientDBID", "ClientName_ID", "MarketSector_ID",
            "Used_iField", "Project_Owner", "Status"
        };

        private sealed class FillValue
        {
            public string Column;
            public object Value;
            public int FromId;
        }

        private string _lastGroupKey;
        private int _groupIndex;

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
            DataTable dt = AdminDb.Query(@"
SELECT p.ID, p.Symphony_Number, p.Symphony_Key, p.Project_Name, pt.Project_Type, st.SubType, cn.ClientName,
       p.Project_StartDate, p.Project_EndDate, p.[Status], p.Creator, p.Created_DateTime
FROM [dbo].[Project] p
LEFT JOIN [dbo].[Project_Type] pt ON pt.ID = p.Project_Type_ID
LEFT JOIN [dbo].[Project_SubType] st ON st.ID = p.Project_SubType_ID
LEFT JOIN [dbo].[ClientName] cn ON cn.ID = p.ClientName_ID
WHERE p.Is_Deleted = 0
  AND p.Symphony_Key IN (
      SELECT Symphony_Key FROM [dbo].[Project]
      WHERE Is_Deleted = 0 AND Symphony_Key IS NOT NULL
      GROUP BY Symphony_Key HAVING COUNT(*) > 1)
ORDER BY p.Symphony_Key, COALESCE(p.Modified_DateTime, p.Created_DateTime) DESC, p.ID DESC");

            int groups = dt.AsEnumerable().Select(r => AdminDb.ToText(r["Symphony_Key"])).Distinct().Count();
            LblSummary.Text = groups.ToString("N0", CultureInfo.InvariantCulture) + " group(s), "
                + dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture) + " project(s)";
            PnlNoDuplicates.Visible = groups == 0;

            _lastGroupKey = null;
            _groupIndex = 0;
            GridDuplicates.DataSource = dt;
            GridDuplicates.DataBind();
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

        protected void GridDuplicates_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var data = (DataRowView)e.Row.DataItem;
            string key = AdminDb.ToText(data["Symphony_Key"]);
            bool newGroup = _lastGroupKey == null || !string.Equals(key, _lastGroupKey, StringComparison.Ordinal);
            if (newGroup && _lastGroupKey != null)
            {
                _groupIndex++;
            }
            _lastGroupKey = key;

            string css = _groupIndex % 2 == 0 ? "dup-group-a" : "dup-group-b";
            if (newGroup)
            {
                css += " dup-group-start";
            }
            e.Row.CssClass = css;
        }

        private static bool IsIdColumn(string column)
        {
            return column.EndsWith("_ID", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column, "ClientDBID", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>NULL, blank text, or 0 in a foreign-key column counts as empty.</summary>
        private static bool IsEmpty(object value, string column)
        {
            if (value == null || value == DBNull.Value)
            {
                return true;
            }

            string text = value as string;
            if (text != null)
            {
                string trimmed = text.Trim();
                return trimmed.Length == 0 || (IsIdColumn(column) && trimmed == "0");
            }

            if (IsIdColumn(column) && (value is int || value is long || value is short || value is byte || value is decimal))
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture) == 0m;
            }

            return false;
        }

        protected void GridDuplicates_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Keep")
            {
                return;
            }

            int keptId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out keptId))
            {
                return;
            }

            try
            {
                string email = AppAuth.CurrentEmail;
                var mergedIds = new List<int>();
                var fills = new List<FillValue>();
                string symphonyNumber;

                using (SqlConnection conn = AdminDb.Open())
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    DataTable keptTable = AdminDb.QueryTx(conn, tx,
                        "SELECT * FROM [dbo].[Project] WITH (UPDLOCK, HOLDLOCK) WHERE ID = @Id AND Is_Deleted = 0",
                        AdminDb.P("@Id", keptId));
                    if (keptTable.Rows.Count == 0)
                    {
                        AdminDb.ShowMessage(LblMessage, "Project " + keptId + " no longer exists or was already deleted.", "warning");
                        BindGrid();
                        return;
                    }

                    DataRow kept = keptTable.Rows[0];
                    symphonyNumber = AdminDb.ToText(kept["Symphony_Number"]);
                    if (kept["Symphony_Key"] == DBNull.Value)
                    {
                        AdminDb.ShowMessage(LblMessage, "Project " + keptId + " has no Symphony Number.", "warning");
                        return;
                    }

                    DataTable others = AdminDb.QueryTx(conn, tx,
                        "SELECT * FROM [dbo].[Project] WITH (UPDLOCK, HOLDLOCK) WHERE Is_Deleted = 0 AND Symphony_Key = @Key AND ID <> @Id "
                        + "ORDER BY COALESCE(Modified_DateTime, Created_DateTime) DESC, ID DESC",
                        AdminDb.P("@Key", kept["Symphony_Key"]),
                        AdminDb.P("@Id", keptId));
                    if (others.Rows.Count == 0)
                    {
                        AdminDb.ShowMessage(LblMessage, "Project " + keptId + " no longer has duplicates.", "info");
                        BindGrid();
                        return;
                    }

                    // 1) Fill empty fields of the kept row; the most recent non-empty value wins.
                    foreach (string column in FillColumns)
                    {
                        if (!keptTable.Columns.Contains(column) || !IsEmpty(kept[column], column))
                        {
                            continue;
                        }
                        foreach (DataRow other in others.Rows)
                        {
                            if (!IsEmpty(other[column], column))
                            {
                                fills.Add(new FillValue { Column = column, Value = other[column], FromId = Convert.ToInt32(other["ID"]) });
                                break;
                            }
                        }
                    }

                    // SubType must belong to the (possibly just filled) project type.
                    const string subTypeColumn = "Project_SubType_ID";
                    if (keptTable.Columns.Contains(subTypeColumn) && IsEmpty(kept[subTypeColumn], subTypeColumn))
                    {
                        FillValue typeFill = fills.FirstOrDefault(f => f.Column == "Project_Type_ID");
                        string effectiveType = typeFill != null ? AdminDb.ToText(typeFill.Value) : AdminDb.ToText(kept["Project_Type_ID"]);
                        foreach (DataRow other in others.Rows)
                        {
                            if (IsEmpty(other[subTypeColumn], subTypeColumn))
                            {
                                continue;
                            }
                            if (effectiveType.Length == 0 || IsEmpty(effectiveType, "Project_Type_ID")
                                || AdminDb.ToText(other["Project_Type_ID"]) == effectiveType)
                            {
                                fills.Add(new FillValue { Column = subTypeColumn, Value = other[subTypeColumn], FromId = Convert.ToInt32(other["ID"]) });
                                break;
                            }
                        }
                    }

                    var updateParams = new List<SqlParameter>();
                    var setClauses = new List<string>();
                    for (int i = 0; i < fills.Count; i++)
                    {
                        string paramName = "@f" + i.ToString(CultureInfo.InvariantCulture);
                        setClauses.Add("[" + fills[i].Column + "] = " + paramName);
                        updateParams.Add(AdminDb.P(paramName, fills[i].Value));
                    }
                    setClauses.Add("Modified_By = @By");
                    setClauses.Add("Modified_DateTime = GETDATE()");
                    updateParams.Add(AdminDb.P("@By", email));
                    updateParams.Add(AdminDb.P("@KeptId", keptId));
                    AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[Project] SET " + string.Join(", ", setClauses) + " WHERE ID = @KeptId",
                        updateParams.ToArray());

                    // 2) Soft-delete the other rows.
                    var deleteParams = new List<SqlParameter> { AdminDb.P("@By", email) };
                    var idParams = new List<string>();
                    foreach (DataRow other in others.Rows)
                    {
                        int otherId = Convert.ToInt32(other["ID"]);
                        mergedIds.Add(otherId);
                        string paramName = "@d" + idParams.Count.ToString(CultureInfo.InvariantCulture);
                        idParams.Add(paramName);
                        deleteParams.Add(AdminDb.P(paramName, otherId));
                    }
                    AdminDb.ExecuteTx(conn, tx,
                        "UPDATE [dbo].[Project] SET Is_Deleted = 1, Modified_By = @By, Modified_DateTime = GETDATE() WHERE Is_Deleted = 0 AND ID IN ("
                        + string.Join(", ", idParams) + ")",
                        deleteParams.ToArray());

                    // 3) Audit trail: one row for the kept project, one per merged project (for its own history).
                    string merged = string.Join(",", mergedIds.Select(id => id.ToString(CultureInfo.InvariantCulture)));
                    string filled = string.Join(",", fills.Select(f => f.Column + "(from " + f.FromId.ToString(CultureInfo.InvariantCulture) + ")"));
                    AdminDb.LogTx(conn, tx, "Project", "Merge",
                        "ProjectID=" + keptId + ";merged=" + merged + ";Symphony_Number=" + symphonyNumber
                        + (filled.Length > 0 ? ";filled=" + filled : string.Empty),
                        email);
                    foreach (int otherId in mergedIds)
                    {
                        AdminDb.LogTx(conn, tx, "Project", "Merge",
                            "ProjectID=" + otherId + ";merged_into=" + keptId + ";Is_Deleted[0 -> 1]",
                            email);
                    }

                    tx.Commit();
                }

                string message = "Kept project " + keptId + " (" + symphonyNumber + "); soft-deleted " + mergedIds.Count + " duplicate(s): "
                    + string.Join(", ", mergedIds) + ".";
                if (fills.Count > 0)
                {
                    message += " Filled empty fields: " + string.Join(", ", fills.Select(f => f.Column)) + ".";
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
