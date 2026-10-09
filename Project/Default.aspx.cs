using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace Project
{
    public partial class _Default : Page
    {
        private readonly Core _core = new Core();
        private readonly string _ldap = ConfigurationManager.AppSettings["LDAP"];

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && AppAuth.IsLoggedIn)
            {
                Response.Redirect("/Home", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                bool isValid = false;
                string email = TextBox1.Text.Trim();

                if (IsValidEmailInput(email) && !string.IsNullOrEmpty(TextBox2.Text))
                {
                    try
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, _ldap))
                        {
                            isValid = pc.ValidateCredentials(email, TextBox2.Text);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LDAP authentication error: {ex.Message}");
                    }
                }

                if (isValid && AppAuth.SignIn(email))
                {
                    try
                    {
                        _core.Logs(ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString,
                            "App_User", "Login", "Login from " + Request.UserHostAddress, email.ToLowerInvariant());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Login log failed: {ex.Message}");
                    }
                    Response.Redirect("/Home", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else if (isValid)
                {
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('บัญชีนี้ถูกปิดการใช้งาน กรุณาติดต่อผู้ดูแลระบบ');", true);
                }
                else
                {
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('Login Failed');", true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Button1_Click: {ex.Message}");
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('An error occurred. Please try again.');", true);
            }
        }

        private bool IsValidEmailInput(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length < 3)
            {
                return false;
            }

            return CheckEmailFormat(email) &&
                   Regex.IsMatch(email[0].ToString(), "^[a-zA-Z]$") &&
                   Regex.IsMatch(email[email.Length - 1].ToString(), "^[a-zA-Z]$");
        }

        public bool CheckEmailFormat(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }

            try
            {
                var mailAddress = new MailAddress(emailAddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}