using System;
using System.Data;
using System.Globalization;

namespace Project.Admin
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!AppAuth.IsAdmin)
            {
                Response.Redirect("~/", true);
                return;
            }

            if (!IsPostBack)
            {
                LoadCounts();
            }
        }

        private void LoadCounts()
        {
            try
            {
                DataTable dt = AdminDb.Query(@"
SELECT
    (SELECT COUNT(*) FROM [dbo].[Project] WHERE Is_Deleted = 0) AS ActiveProjects,
    (SELECT COUNT(*) FROM [dbo].[Project] WHERE Is_Deleted = 1) AS DeletedProjects,
    (SELECT COUNT(*) FROM (
        SELECT Symphony_Key FROM [dbo].[Project]
        WHERE Is_Deleted = 0 AND Symphony_Key IS NOT NULL
        GROUP BY Symphony_Key HAVING COUNT(*) > 1) d) AS DuplicateGroups,
    (SELECT COUNT(*) FROM [dbo].[App_User] WHERE [Status] = 1) AS ActiveUsers");

                if (dt.Rows.Count == 0)
                {
                    return;
                }

                DataRow row = dt.Rows[0];
                LblActiveProjects.Text = FormatCount(row["ActiveProjects"]);
                LblDeletedProjects.Text = FormatCount(row["DeletedProjects"]);
                LblActiveUsers.Text = FormatCount(row["ActiveUsers"]);

                int duplicates = Convert.ToInt32(row["DuplicateGroups"]);
                LblDuplicateGroups.Text = FormatCount(duplicates);
                if (duplicates > 0)
                {
                    LblDuplicateGroups.CssClass = "admin-stat-value admin-stat-alert";
                }
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        private static string FormatCount(object value)
        {
            return Convert.ToInt32(value).ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
