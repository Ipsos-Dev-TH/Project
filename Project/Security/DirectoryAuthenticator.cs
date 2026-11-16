using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Project.Security
{
    public enum DirectoryOutcome
    {
        Success,
        /// <summary>LDAP result 49. See SubCode (52e wrong password or unknown user, 775 locked, 533 disabled, ...).</summary>
        Rejected,
        /// <summary>Server down, connect error, timeout or any other failure: the password was not judged.</summary>
        Unreachable,
        /// <summary>The bind succeeded but the account could not be identified uniquely.</summary>
        LookupFailed
    }

    public sealed class DirectoryResult
    {
        public DirectoryOutcome Outcome { get; set; }
        public string SubCode { get; set; }
        public AuthProfile Profile { get; set; }
        public string Detail { get; set; }
    }

    /// <summary>
    /// One bind against AD and nothing more: never retries and never tries other name formats,
    /// because every failed bind counts towards the AD lockout.
    /// </summary>
    public static class DirectoryAuthenticator
    {
        private const int LdapInvalidCredentials = 49;
        private static readonly Regex SubCodePattern = new Regex(@"data\s+([0-9a-fA-F]{3,8})", RegexOptions.Compiled);

        public static DirectoryResult Bind(AuthenConfig config, string username, string password)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(username))
            {
                // A simple bind with an empty password is an anonymous bind and would "succeed".
                return new DirectoryResult { Outcome = DirectoryOutcome.Rejected, Detail = "Empty credentials" };
            }

            int port = config.DirectoryUseLdaps ? 636 : 389;
            var identifier = new LdapDirectoryIdentifier(config.DirectoryServer, port, true, false);
            try
            {
                using (var connection = new LdapConnection(identifier))
                {
                    connection.Timeout = TimeSpan.FromSeconds(15);
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                    if (config.DirectoryUseLdaps)
                    {
                        // Certificate is validated by Windows against the DC's FQDN; never bypassed.
                        connection.SessionOptions.SecureSocketLayer = true;
                        connection.AuthType = AuthType.Basic;
                        connection.Bind(new NetworkCredential(username, password));
                    }
                    else
                    {
                        connection.SessionOptions.Signing = true;
                        connection.SessionOptions.Sealing = true;
                        connection.AuthType = AuthType.Negotiate;
                        connection.Bind(ToNegotiateCredential(username, password));
                    }

                    AuthProfile profile = LookupProfile(connection, username);
                    if (profile == null || profile.UserKey == null)
                    {
                        return new DirectoryResult { Outcome = DirectoryOutcome.LookupFailed, Detail = "Bind ok, account lookup failed" };
                    }
                    return new DirectoryResult { Outcome = DirectoryOutcome.Success, Profile = profile, Detail = "Bind ok (port " + port + ")" };
                }
            }
            catch (LdapException ex)
            {
                if (ex.ErrorCode == LdapInvalidCredentials)
                {
                    string subCode = ParseSubCode(ex.ServerErrorMessage);
                    return new DirectoryResult
                    {
                        Outcome = DirectoryOutcome.Rejected,
                        SubCode = subCode,
                        Detail = "LDAP 49" + (subCode == null ? string.Empty : " data " + subCode)
                    };
                }
                return new DirectoryResult { Outcome = DirectoryOutcome.Unreachable, Detail = "LDAP " + ex.ErrorCode };
            }
            catch (Exception ex)
            {
                return new DirectoryResult { Outcome = DirectoryOutcome.Unreachable, Detail = ex.GetType().Name };
            }
        }

        private static NetworkCredential ToNegotiateCredential(string username, string password)
        {
            int slash = username.IndexOf('\\');
            if (slash > 0)
            {
                return new NetworkCredential(username.Substring(slash + 1), password, username.Substring(0, slash));
            }
            return new NetworkCredential(username, password);
        }

        private static string ParseSubCode(string serverMessage)
        {
            if (string.IsNullOrEmpty(serverMessage))
            {
                return null;
            }
            Match match = SubCodePattern.Match(serverMessage);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }

        private static AuthProfile LookupProfile(LdapConnection connection, string username)
        {
            var rootRequest = new SearchRequest(string.Empty, "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
            var rootResponse = (SearchResponse)connection.SendRequest(rootRequest);
            if (rootResponse.Entries.Count == 0)
            {
                return null;
            }
            string baseDn = FirstValue(rootResponse.Entries[0], "defaultNamingContext");
            if (string.IsNullOrEmpty(baseDn))
            {
                return null;
            }

            string filter;
            int slash = username.IndexOf('\\');
            if (slash >= 0)
            {
                filter = "(sAMAccountName=" + Escape(username.Substring(slash + 1)) + ")";
            }
            else if (username.IndexOf('@') >= 0)
            {
                string value = Escape(username);
                filter = "(|(userPrincipalName=" + value + ")(mail=" + value + "))";
            }
            else
            {
                string value = Escape(username);
                filter = "(|(sAMAccountName=" + value + ")(userPrincipalName=" + value + "@*))";
            }
            filter = "(&(objectCategory=person)(objectClass=user)" + filter + ")";

            var request = new SearchRequest(baseDn, filter, SearchScope.Subtree,
                "userPrincipalName", "sAMAccountName", "displayName", "mail", "memberOf");
            request.SizeLimit = 2;
            SearchResponse response;
            try
            {
                response = (SearchResponse)connection.SendRequest(request);
            }
            catch (DirectoryOperationException)
            {
                return null;
            }
            if (response.Entries.Count != 1)
            {
                return null;
            }

            SearchResultEntry entry = response.Entries[0];
            string upn = FirstValue(entry, "userPrincipalName");
            string sam = FirstValue(entry, "sAMAccountName");
            var profile = new AuthProfile
            {
                UserPrincipalName = upn,
                SamAccountName = sam,
                DisplayName = FirstValue(entry, "displayName"),
                Email = FirstValue(entry, "mail"),
                UserKey = AuthProfile.BuildUserKey(upn, sam)
            };
            DirectoryAttribute memberOf = entry.Attributes["memberOf"];
            if (memberOf != null)
            {
                foreach (object value in memberOf.GetValues(typeof(string)))
                {
                    string cn = CommonName(value as string);
                    if (!string.IsNullOrEmpty(cn))
                    {
                        profile.Groups.Add(cn);
                    }
                }
            }
            return profile;
        }

        private static string FirstValue(SearchResultEntry entry, string attribute)
        {
            DirectoryAttribute attr = entry.Attributes[attribute];
            if (attr == null || attr.Count == 0)
            {
                return string.Empty;
            }
            object[] values = attr.GetValues(typeof(string));
            return values.Length == 0 ? string.Empty : ((string)values[0]).Trim();
        }

        private static string CommonName(string distinguishedName)
        {
            if (string.IsNullOrEmpty(distinguishedName) || !distinguishedName.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var name = new StringBuilder();
            for (int i = 3; i < distinguishedName.Length; i++)
            {
                char c = distinguishedName[i];
                if (c == '\\' && i + 1 < distinguishedName.Length)
                {
                    name.Append(distinguishedName[++i]);
                }
                else if (c == ',')
                {
                    break;
                }
                else
                {
                    name.Append(c);
                }
            }
            return name.ToString();
        }

        /// <summary>RFC 4515 filter escaping.</summary>
        private static string Escape(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\5c"); break;
                    case '*': sb.Append(@"\2a"); break;
                    case '(': sb.Append(@"\28"); break;
                    case ')': sb.Append(@"\29"); break;
                    case '\0': sb.Append(@"\00"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
