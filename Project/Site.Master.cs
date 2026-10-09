using System;
using System.Configuration;
using System.Web;
using System.Web.UI;

namespace Project
{
        public partial class SiteMaster : MasterPage
        {
            // Login and role checks are enforced centrally in Global.Application_PostAcquireRequestState.
            protected void Page_Load(object sender, EventArgs e)
            {
                if (AdminMenuItem != null)
                {
                    AdminMenuItem.Visible = AppAuth.IsAdmin;
                }
                if (CurrentUserLabel != null)
                {
                    CurrentUserLabel.Text = HttpUtility.HtmlEncode(AppAuth.CurrentEmail + " (" + AppAuth.CurrentRole + ")");
                }
            }

            protected void ImageButton1_Click(object sender, EventArgs e)
            {
                AppAuth.SignOut();
                Response.Redirect("/Default.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }
    
}