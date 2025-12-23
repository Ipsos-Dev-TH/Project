using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Configuration;

namespace Project.Admin
{
    public partial class Default : System.Web.UI.Page
    {
        
        Core Core = new Core();
        string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!IsPostBack)
                {
                    string Email = Session["Email"].ToString();




                }
            }
            catch { Response.Redirect("/"); }
        }

        
        protected void Button10_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=Project_Type");
        }

        protected void Button11_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=Project_SubType&TypeID=" + DropDownList4.SelectedItem.Value);
        }

        protected void Button12_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=Cost_Type");
        }

        

        protected void Button15_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=Payment_Report_Period");
        }

        protected void Button12_Click1(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=Product_Type");
        }

        protected void Button13_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=ClientName");
        }

        protected void Button14_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin/Edit.aspx?data=MarketSector");
        }
    }
}