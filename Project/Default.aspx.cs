using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.DirectoryServices.Protocols;
using System.Net;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Configuration;
using System.Security.Cryptography;
using System.Web.Services;
using System.Web.Script.Services;

namespace Project
{
    public partial class _Default : Page
    {

        Core Core = new Core();
        string ldap = ConfigurationSettings.AppSettings["LDAP"];
        protected void Page_Load(object sender, EventArgs e)
        {
            string email = Core.GetURIparameter("access", HttpContext.Current.Request.Url.AbsoluteUri);
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {

            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }

            try
            {
                if(email.Length > 0)
                {
                    //email = Core.Crypt(email);
                    email = Core.Derypt(email);
                    email = email.ToLower();
                    if (email.Contains("@ipsos.com"))
                    {
                        Session["LoginStatus"] = "true";
                        Session["Email"] = email;
                        Response.Redirect("/Home");
                    }
                }
            }
            catch
            {

            }
        }
        protected void Button1_Click(object sender, EventArgs e)
        {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
            try
            {
                //PrincipalContext pc = new PrincipalContext(ContextType.Domain, "ipsosgroup.ipsos.com");
                //bool LoginStatus = pc.ValidateCredentials(TextBox1.Text, TextBox2.Text);
                //bool LoginStatus = true;
                bool isValid = false;
                try
                {
                    //DirectoryContext context = new DirectoryContext(DirectoryContextType.DirectoryServer, "10.137.157.210", TextBox1.Text, TextBox2.Text);
                    //DirectoryEntry deDoc = Domain.GetDomain(context).GetDirectoryEntry();


                    //DirectorySearcher search = new DirectorySearcher(deDoc);

                    //search.Filter = "(mail=" + TextBox1.Text + ")";

                    //search.PropertiesToLoad.Add("cn");

                    //SearchResult result = search.FindOne();

                    //if (result == null)
                    //{
                    //    LoginStatus = false;
                    //}
                    //else
                    //{
                    //    LoginStatus = true;
                    //}
                    if (checkEmailFormat(TextBox1.Text) && Regex.IsMatch(TextBox1.Text[0].ToString(), "^[a-zA-Z]*$") && Regex.IsMatch(TextBox1.Text[TextBox1.Text.Length - 1].ToString(), "^[a-zA-Z]*$"))
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, ldap))
                        {
                            // validate the credentials
                            isValid = pc.ValidateCredentials(TextBox1.Text, TextBox2.Text);
                        }
                    }
                    else
                    {

                    }

                }
                catch (Exception ex)
                {
                }
                if (isValid == true)
                {
                    Session["LoginStatus"] = "true";
                    Session["Email"] = TextBox1.Text;
                    Response.Redirect("/Home");
                }
                else
                {
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('Login Failed');", true);
                }
            }
            catch (Exception ex) { }

        }


        public bool checkEmailFormat(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}