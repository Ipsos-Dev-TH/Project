using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Script.Services;

namespace Project
{
    /// <summary>
    /// Web service for client name autocomplete functionality
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class GetClientName : WebService
    {
        private readonly Core _core = new Core();
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        [WebMethod]
        [ScriptMethod]
        public List<string> searchInDictionary(string prefixText, int count)
        {
            List<string> names = new List<string>();

            if (string.IsNullOrWhiteSpace(prefixText))
            {
                return names;
            }

            // Limit count to prevent excessive data retrieval
            if (count <= 0 || count > 100)
            {
                count = 20;
            }

            string query = "SELECT TOP (@Count) [ClientName] FROM ClientName WHERE Status = 1 AND ClientName LIKE @SearchPattern ORDER BY ClientName";

            DataTable dt = _core.DatabaseQueryWithParams(_connectionString, query,
                new SqlParameter("@Count", count),
                new SqlParameter("@SearchPattern", "%" + prefixText + "%"));

            foreach (DataRow row in dt.Rows)
            {
                if (row[0] != null)
                {
                    names.Add(row[0].ToString());
                }
            }

            return names;
        }
    }
}