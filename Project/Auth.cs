using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Project
{
    public static class AppAuth
    {
        public const string RoleAdmin = "Admin";
        public const string RoleEditor = "Editor";
        public const string RoleViewer = "Viewer";
        public const string RoleDisabled = "Disabled";

        public static readonly string[] AssignableRoles = { RoleAdmin, RoleEditor, RoleViewer };

        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        private static HttpSessionStateBase SessionState =>
            HttpContext.Current?.Session == null ? null : new HttpSessionStateWrapper(HttpContext.Current.Session);

        public static bool IsLoggedIn => SessionState?["LoginStatus"] as string == "true";

        public static string CurrentEmail => SessionState?["Email"] as string ?? string.Empty;

        public static string CurrentRole => SessionState?["Role"] as string ?? RoleViewer;

        public static bool IsAdmin => CurrentRole == RoleAdmin;

        public static bool CanEdit => CurrentRole == RoleAdmin || CurrentRole == RoleEditor;

        /// <summary>Returns false when the account is disabled in App_User.</summary>
        public static bool SignIn(string email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            string role = ResolveRole(email);
            if (role == RoleDisabled)
            {
                return false;
            }

            var session = HttpContext.Current.Session;
            session.Clear();
            session["LoginStatus"] = "true";
            session["Email"] = email;
            session["Role"] = role;
            return true;
        }

        public static void SignOut()
        {
            var context = HttpContext.Current;
            context.Session.Clear();
            context.Session.Abandon();
            context.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", string.Empty) { Expires = DateTime.Now.AddDays(-1) });
        }

        public static string ResolveRole(string email)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand("SELECT [Role], [Status] FROM [dbo].[App_User] WHERE [Email] = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool active = reader["Status"] != DBNull.Value && Convert.ToBoolean(reader["Status"]);
                            string role = reader["Role"]?.ToString();
                            if (!active)
                            {
                                return RoleDisabled;
                            }
                            if (AssignableRoles.Contains(role))
                            {
                                return role;
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // App_User may not exist yet if the migration has not been applied.
                System.Diagnostics.Debug.WriteLine($"ResolveRole: {ex.Message}");
            }

            string admins = ConfigurationManager.AppSettings["AdminEmails"] ?? string.Empty;
            bool isConfiguredAdmin = admins
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(a => string.Equals(a.Trim(), email, StringComparison.OrdinalIgnoreCase));
            if (isConfiguredAdmin)
            {
                return RoleAdmin;
            }

            string defaultRole = ConfigurationManager.AppSettings["DefaultRole"];
            return AssignableRoles.Contains(defaultRole) ? defaultRole : RoleEditor;
        }
    }
}
