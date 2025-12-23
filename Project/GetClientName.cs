
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

namespace Project
{
    /// <summary>
    /// Summary description for MyDictionary
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class GetClientName : System.Web.Services.WebService
    {

        public GetClientName()
        {

            //Uncomment the following line if using designed components 
            //InitializeComponent(); 
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        //removed static modifier
        //display error: Unknown web method searchInDictionary.
        public List<string> searchInDictionary(string prefixText, int count)
        {
            Core Core = new Core();
            string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

            DataTable dt = Core.DatabaseQuery(conn, "Select [ClientName] from ClientName Where Status = 1 And ClientName like '%" + prefixText + "%'");
            List<string> names = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                names.Add(dt.Rows[i][0].ToString());
            }
            return names;
        }

    }
}