using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Data.OleDb;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using Project;
using System.IO;
using System.Data.SqlClient;

namespace Project.App_Code
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        //removed static modifier
        //display error: Unknown web method searchInDictionary.
        public List<string> searchInDictionary(string prefixText, int count)
        {
            Core Core = new Core();


            using (SqlConnection conn = new SqlConnection())
            {
                //conn.ConnectionString = ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;
                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = "select ContactName from Customers where ContactName like @SearchText + '%'";
                    cmd.CommandText = "Select [ClientName] from ClientName Where Status = 1 and ClientName like '%' + @SearchText + '%'";

                    cmd.Parameters.AddWithValue("@SearchText", prefixText);
                    cmd.Connection = conn;
                    conn.Open();
                    List<string> customers = new List<string>();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            //customers.Add(sdr["ContactName"].ToString());
                            customers.Add(sdr["ClientName"].ToString());
                        }
                    }
                    conn.Close();
                    return customers;
                }
            }
        }
        
    }
}
