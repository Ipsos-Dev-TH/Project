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
            string email = _core.GetURIparameter("access", HttpContext.Current.Request.Url.AbsoluteUri);

            try
            {
                if (!string.IsNullOrEmpty(email))
                {
                    email = _core.Decrypt(email);
                    email = email.ToLower();

                    if (IsValidIpsosEmail(email))
                    {
                        Session["LoginStatus"] = "true";
                        Session["Email"] = email;
                        Response.Redirect("/Home", false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Page_Load: {ex.Message}");
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                bool isValid = false;

                if (IsValidEmailInput(TextBox1.Text))
                {
                    try
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, _ldap))
                        {
                            isValid = pc.ValidateCredentials(TextBox1.Text, TextBox2.Text);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LDAP authentication error: {ex.Message}");
                    }
                }

                if (isValid)
                {
                    Session["LoginStatus"] = "true";
                    Session["Email"] = TextBox1.Text;
                    Response.Redirect("/Home", false);
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

        private bool IsValidIpsosEmail(string email)
        {
            return !string.IsNullOrEmpty(email) &&
                   email.EndsWith("@ipsos.com", StringComparison.OrdinalIgnoreCase);
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