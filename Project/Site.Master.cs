using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Project
{
        public partial class SiteMaster : MasterPage
        {
            protected void Page_Load(object sender, EventArgs e)
            {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
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

        

        protected void ImageButton1_Click(object sender, ImageClickEventArgs e)
            {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
            if (e.X > 0 || e.Y > 0)
                {
                    Session["LoginStatus"] = "false";
                    Session["Email"] = null;
                    Response.Redirect("/Default.aspx");
                }
                else { }
            }
        }
    
}