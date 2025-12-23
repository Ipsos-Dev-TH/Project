using System;
using System.Configuration;
using System.Web;
using System.Web.UI;

namespace Project
{
        public partial class SiteMaster : MasterPage
        {
            protected void Page_Load(object sender, EventArgs e)
            {
            // Get default URL from configuration if needed
            string mainpath = ConfigurationManager.AppSettings["DefaultURL"] ?? "";
            if (HttpContext.Current.Request.Url.AbsolutePath.ToString().Contains("Default"))
                { }
                else
                {
                    try
                    {
                        string LoginStatus = Session["LoginStatus"].ToString();
                        if (LoginStatus == "true")
                        {

                        }
                        else
                        {
                            Response.Redirect("/Default.aspx");
                        }
                    }
                    catch
                    {
                        Session["LoginStatus"] = "false";
                        Response.Redirect("/Default.aspx");
                    }
                }
            }

        

        protected void ImageButton1_Click(object sender, EventArgs e)
            {
                // Clear session and redirect to login page
                Session["LoginStatus"] = "false";
                Session["Email"] = null;
                Response.Redirect("/Default.aspx");
            }
        }
    
}