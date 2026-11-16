using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Project.Security;

namespace Project
{
    public static class AppAuth
    {
        public const string RoleAdmin = "Admin";
        public const string RoleEditor = "Editor";
        public const string RoleViewer = "Viewer";
        public const string RoleDisabled = "Disabled";

        public static readonly string[] AssignableRoles = { RoleAdmin, RoleEditor, RoleViewer };

        /// <summary>Force a fresh login after this long, so disabled AD accounts lose access.</summary>
        public static readonly TimeSpan MaxSessionAge = TimeSpan.FromHours(10);

        /// <summary>Sessions created by the AD fallback or offline cache must log in again after this long.</summary>
        public static readonly TimeSpan MaxDegradedSessionAge = TimeSpan.FromMinutes(45);

        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        private static HttpSessionStateBase SessionState =>
            HttpContext.Current?.Session == null ? null : new HttpSessionStateWrapper(HttpContext.Current.Session);

        public static bool IsLoggedIn => SessionState?["LoginStatus"] as string == "true";

        /// <summary>The user key: AD userPrincipalName in lower case.</summary>
        public static string CurrentEmail => SessionState?["Email"] as string ?? string.Empty;

        public static string CurrentDisplayName
        {
            get
            {
                string name = SessionState?["DisplayName"] as string;
                return string.IsNullOrWhiteSpace(name) ? CurrentEmail : name;
            }
        }

        public static string CurrentRole => SessionState?["Role"] as string ?? RoleViewer;

        public static bool IsDegraded => SessionState?["Degraded"] as bool? == true;

        public static bool IsAdmin => CurrentRole == RoleAdmin;

        public static bool CanEdit => CurrentRole == RoleAdmin || CurrentRole == RoleEditor;

        /// <summary>True when the session is older than its allowed age and must log in again.</summary>
        public static bool IsSessionExpired
        {
            get
            {
                var loginUtc = SessionState?["LoginUtc"] as DateTime?;
                if (loginUtc == null)
                {
                    return true;
                }
                TimeSpan age = DateTime.UtcNow - loginUtc.Value;
                return age > MaxSessionAge || (IsDegraded && age > MaxDegradedSessionAge);
            }
        }

        public static void SignIn(LoginResult result)
        {
            var session = HttpContext.Current.Session;
            session.Clear();
            session["LoginStatus"] = "true";
            session["Email"] = result.Profile.UserKey;
            session["DisplayName"] = result.Profile.DisplayName;
            session["Groups"] = result.Profile.Groups.ToArray();
            session["Role"] = result.Role;
            session["Degraded"] = result.Degraded;
            session["AuthMode"] = result.Mode;
            session["LoginUtc"] = DateTime.UtcNow;
        }

        public static void SignOut()
        {
            var context = HttpContext.Current;
            context.Session.Clear();
            context.Session.Abandon();
            context.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", string.Empty) { Expires = DateTime.Now.AddDays(-1) });
        }

        /// <summary>
        /// Order: App_User row (an explicit decision always wins, including Disabled) → AdminEmails →
        /// AD groups in AdminGroups / EditorGroups / ViewerGroups → DefaultRole.
        /// </summary>
        public static string ResolveRole(string userKey, IEnumerable<string> groups)
        {
            userKey = (userKey ?? string.Empty).Trim().ToLowerInvariant();
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand("SELECT [Role], [Status] FROM [dbo].[App_User] WHERE [Email] = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", userKey);
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

            if (SettingList("AdminEmails").Contains(userKey, StringComparer.OrdinalIgnoreCase))
            {
                return RoleAdmin;
            }

            var memberOf = new HashSet<string>(groups ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (SettingList("AdminGroups").Any(memberOf.Contains))
            {
                return RoleAdmin;
            }
            if (SettingList("EditorGroups").Any(memberOf.Contains))
            {
                return RoleEditor;
            }
            if (SettingList("ViewerGroups").Any(memberOf.Contains))
            {
                return RoleViewer;
            }

            string defaultRole = ConfigurationManager.AppSettings["DefaultRole"];
            return AssignableRoles.Contains(defaultRole) ? defaultRole : RoleEditor;
        }

        private static string[] SettingList(string key)
        {
            return (ConfigurationManager.AppSettings[key] ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }
    }
}
