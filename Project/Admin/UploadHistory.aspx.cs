using System;
using System.Data;
using System.Web;
using System.Web.UI.WebControls;

namespace Project.Admin
{
    public partial class UploadHistory : System.Web.UI.Page
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
                SafeBindGrid();
            }
        }

        private void BindGrid()
        {
            DataTable dt = AdminDb.Query(@"
SELECT TOP 200 ID, [File_Name], Uploaded_By, Uploaded_DateTime, Is_Preview, Total_Rows, Inserted_Count,
       Updated_Count, Skipped_Count, Error_Count,
       CAST(CASE WHEN Error_Detail IS NULL OR DATALENGTH(Error_Detail) = 0 THEN 0 ELSE 1 END AS bit) AS HasDetail
FROM [dbo].[Upload_History]
ORDER BY Uploaded_DateTime DESC, ID DESC");

            int pageCount = (dt.Rows.Count + GridUploads.PageSize - 1) / GridUploads.PageSize;
            if (GridUploads.PageIndex >= pageCount)
            {
                GridUploads.PageIndex = Math.Max(0, pageCount - 1);
            }
            GridUploads.DataSource = dt;
            GridUploads.DataBind();
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

        protected void GridUploads_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridUploads.PageIndex = e.NewPageIndex;
            SafeBindGrid();
        }

        protected void GridUploads_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ViewDetail")
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
                DataTable dt = AdminDb.Query(
                    "SELECT [File_Name], Uploaded_By, Uploaded_DateTime, Error_Detail FROM [dbo].[Upload_History] WHERE ID = @Id",
                    AdminDb.P("@Id", id));
                if (dt.Rows.Count == 0)
                {
                    AdminDb.ShowMessage(LblMessage, "This upload record no longer exists.", "warning");
                    PnlDetail.Visible = false;
                    return;
                }

                DataRow row = dt.Rows[0];
                string uploadedAt = row["Uploaded_DateTime"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToDateTime(row["Uploaded_DateTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                LblDetailTitle.Text = HttpUtility.HtmlEncode(
                    "Errors - " + AdminDb.ToText(row["File_Name"]) + " (" + AdminDb.ToText(row["Uploaded_By"]) + ", " + uploadedAt + ")");

                string detail = AdminDb.ToText(row["Error_Detail"]);
                LitDetail.Text = detail.Length == 0 ? "(no error detail)" : detail;
                PnlDetail.Visible = true;
            }
            catch (Exception ex)
            {
                AdminDb.ShowError(LblMessage, ex);
            }
        }

        protected void BtnCloseDetail_Click(object sender, EventArgs e)
        {
            PnlDetail.Visible = false;
            LitDetail.Text = string.Empty;
        }
    }
}
