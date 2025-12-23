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
    public partial class Edit : System.Web.UI.Page
    {
        Core Core = new Core();
        string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;
        static DataTable dt = new DataTable();
        static DataTable dtshow = new DataTable();
        static string data = string.Empty;
        static string BU = string.Empty;
        static string supplierid = string.Empty;
        static string Email = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                Email = Session["Email"].ToString();
                
                data = Request.QueryString["data"];
                BU = Request.QueryString["BU"];
                supplierid = Request.QueryString["supplierid"];
                if (!IsPostBack)
                {

                    readData();
                }
                else { }
            }
            catch
            {
                Response.Redirect("/Home");
            }
        }

        public void readData()
        {
            Email = Session["Email"].ToString();
            if (data == "Project_SubType")
            {
                string ID = Request.QueryString["TypeID"];
                DataTable dtt = Core.DatabaseQuery(conn, "Select [Project_Type] From Project_Type Where [Status] = 'True' AND  ID = " + ID);
                dt = Core.DatabaseQuery(conn, "Select * From Project_SubType Where [Status] = 'True' AND  Project_Type_ID = " + ID);
                string name = dtt.Rows[0][0].ToString();
                Label1.Text = "Editing Data of " + data + " that Project Type is " + name;
            }
            else if ( data == "Project_Type"  || data == "Product_Type" || data == "ClientName" || data == "MarketSector")
            {
                string query = "Select * From " + data + " Where [Status] = 'True' order by id desc";
                
                dt = Core.DatabaseQuery(conn, query);
                Label1.Text = "Editing Data of " + data;
            }
            else
            {
                Response.Redirect("/");
            }

            dtshow = new DataTable();
            dtshow.Merge(dt);
            for (int i = 0; i < dtshow.Columns.Count; i++)
            {
                if (dtshow.Columns[i].ColumnName.ToLower() == "id")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "bu" && data.ToLower() != "bu")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "typeid")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "bu_id")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "status")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "cost_type_id")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
                else if (dtshow.Columns[i].ColumnName.ToLower() == "project_type_id")
                {
                    dtshow.Columns.Remove(dtshow.Columns[i].ColumnName);
                    i--;
                }
            }

            GridView1.DataSource = dtshow;
            GridView1.DataBind();
        }

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView1.EditIndex = e.NewEditIndex;
            GridView1.DataSource = dtshow;
            GridView1.DataBind();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            e.Cancel = true;
            GridView1.EditIndex = -1;
            GridView1.DataSource = dtshow;
            GridView1.DataBind();
        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            Email = Session["Email"].ToString();
            GridViewRow row = GridView1.Rows[e.RowIndex];
            string cmd = "UPDATE " + data + " SET ";
            string DataChanged = "ID-" + dt.Rows[e.RowIndex]["ID"].ToString() + "{";
            for (int i = 0; i < dtshow.Columns.Count; i++)
            {
                string columnname = dtshow.Columns[i].ColumnName;
                if(dtshow.Rows[e.RowIndex][columnname].ToString() != ((TextBox)(row.Cells[2 + i].Controls[0])).Text)
                {

                    DataChanged += columnname + "["+ dtshow.Rows[e.RowIndex][columnname].ToString() + ","+ ((TextBox)(row.Cells[2 + i].Controls[0])).Text + "];";
                }
                cmd += columnname + " = '" + ((TextBox)(row.Cells[2 + i].Controls[0])).Text + "'";
                if (i != dtshow.Columns.Count - 1)
                {
                    cmd += ", ";
                }
            }
            cmd += " WHERE ID = " + Convert.ToInt64(dt.Rows[e.RowIndex]["ID"].ToString());
            try
            {
                Core.DatabaseInsert(conn, cmd);
                DataChanged += "}";
                Core.Logs(conn, Request.QueryString["data"], "Update", DataChanged, Session["Email"].ToString());

            }
            catch (Exception ex)
            {
                throw ex;
            }
            GridView1.EditIndex = -1;
            readData();
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string logde = "";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                logde += dt.Rows[e.RowIndex][i].ToString() + ";";
            }
            Core.DatabaseInsert(conn, "UPDATE " + data + " SET [Status] = 'False' WHERE ID = " + Convert.ToInt64(dt.Rows[e.RowIndex]["ID"].ToString()));
            Core.Logs(conn, Request.QueryString["data"], "Update", "ID-"+ Convert.ToInt64(dt.Rows[e.RowIndex]["ID"].ToString()) + "{Status[True,False];}", Session["Email"].ToString());
            readData();
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Email = Session["Email"].ToString();
            DataTable dtupdate = dt;
            try
            {
                dtupdate.Columns.Remove("ID");
                dtupdate.Columns.Remove("Status");
            }
            catch { }
            string cmd = "INSERT INTO  " + data + " (";
            for (int i = 0; i < dtupdate.Columns.Count; i++)
            {
                cmd += "[" + dtupdate.Columns[i].ColumnName + "]";
                if (i != dtupdate.Columns.Count - 1)
                {
                    cmd += ", ";
                }
            }
            cmd += ") VALUES (";
            for (int i = 0; i < dtupdate.Columns.Count; i++)
            {
                if (dtupdate.Columns[i].ColumnName == "BU" && data.ToLower() != "bu")
                {
                    cmd += BU;
                }
                else if (dtupdate.Columns[i].ColumnName == "Project_Type_ID")
                {
                    string ID = Request.QueryString["TypeID"];
                    cmd += ID;
                }
                else if (dtupdate.Columns[i].ColumnName == "Cost_Type_ID")
                {
                    string ID = Request.QueryString["Cost_Type_ID"];
                    cmd += ID;
                }
                else if (dtupdate.Columns[i].ColumnName == "BU_ID")
                {
                    DataTable dtBu = Core.DatabaseQuery(conn, "SELECT  * FROM [Payment].[dbo].[User] Where Status = 1 AND [User] = '" + Email + "'");
                    string ID = dtBu.Rows[0]["BU_ID"].ToString();
                    cmd += ID;
                }
                else
                {
                    cmd += "''";
                }
                if (i != dtupdate.Columns.Count - 1)
                {
                    cmd += ", ";
                }
            }
            cmd += ")";
            

            try
            {
                Core.DatabaseInsert(conn, cmd);
                Core.Logs(conn, Request.QueryString["data"], "Insert", "", Session["Email"].ToString());

            }
            catch (Exception ex)
            {
                throw ex;
            }
            readData();
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView1.PageIndex = e.NewPageIndex;
            GridView1.DataSource = dtshow;
            GridView1.DataBind();
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            Response.Redirect("/Admin");
        }
    }
}