using System;
using System.Web;
using System.Web.UI;
using Project.Security;

namespace Project
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            if (AppAuth.IsLoggedIn && !AppAuth.IsSessionExpired)
            {
                Response.Redirect("/Home", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (Request.QueryString["expired"] == "1")
            {
                ShowMessage("เซสชันหมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง", "info");
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            LoginResult result;
            try
            {
                // The password is passed exactly as typed: never trimmed, hashed or logged.
                result = LoginService.Login(TextBox1.Text, TextBox2.Text, Request.UserHostAddress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Login failed unexpectedly: " + ex.GetType().Name);
                result = new LoginResult { Outcome = LoginOutcome.Unavailable };
            }

            if (result.Outcome != LoginOutcome.Success)
            {
                ShowMessage(result.UserMessage, result.Outcome == LoginOutcome.InvalidCredentials ? "error" : "warning");
                return;
            }

            AppAuth.SignIn(result);
            Response.Redirect("/Home", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowMessage(string message, string kind)
        {
            LblLoginMessage.Text = HttpUtility.HtmlEncode(message);
            LblLoginMessage.CssClass = "login-message login-message-" + kind;
            LblLoginMessage.Visible = true;
        }
    }
}
