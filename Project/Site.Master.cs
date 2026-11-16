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
                    string text = AppAuth.CurrentDisplayName + " (" + AppAuth.CurrentRole + ")";
                    if (AppAuth.IsDegraded)
                    {
                        text += " · โหมดสำรอง";
                    }
                    CurrentUserLabel.Text = HttpUtility.HtmlEncode(text);
                    CurrentUserLabel.ToolTip = AppAuth.IsDegraded
                        ? "เข้าสู่ระบบผ่านระบบสำรองขณะที่ระบบยืนยันตัวตนขัดข้อง สิทธิ์ Admin ถูกปิดชั่วคราว"
                        : AppAuth.CurrentEmail;
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