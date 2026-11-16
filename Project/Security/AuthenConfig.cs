using System;
using System.Collections.Generic;
using System.Configuration;

namespace Project.Security
{
    /// <summary>
    /// Login settings from Web.config appSettings. Secrets (API key, offline pepper) belong
    /// only in the server's Web.config, never in git.
    /// </summary>
    public sealed class AuthenConfig
    {
        public const string DefaultBaseUrl = "https://authen.hondacx.me";

        private static readonly Lazy<AuthenConfig> _current = new Lazy<AuthenConfig>(Load);

        public static AuthenConfig Current => _current.Value;

        public Uri ApiBaseUri { get; private set; }
        public string ApiKey { get; private set; }
        public string UserAgent { get; private set; }

        public string DirectoryServer { get; private set; }
        public bool DirectoryUseLdaps { get; private set; }

        public int OfflineWindowHours { get; private set; }
        public byte[] OfflinePepper { get; private set; }

        /// <summary>Non-empty when the configuration is invalid; login is refused with a configuration error.</summary>
        public string ConfigurationError { get; private set; }

        public bool ApiConfigured => ConfigurationError == null && !string.IsNullOrEmpty(ApiKey);
        public bool DirectoryConfigured => !string.IsNullOrEmpty(DirectoryServer);
        public bool OfflineEnabled => ApiConfigured && OfflineWindowHours > 0 && OfflinePepper != null;

        private static AuthenConfig Load()
        {
            var settings = ConfigurationManager.AppSettings;
            var config = new AuthenConfig();
            var errors = new List<string>();

            config.ApiKey = (settings["AuthenApiKey"] ?? string.Empty).Trim();
            config.UserAgent = "ProjectManagement/2.0 (" + Environment.MachineName + ")";

            string baseUrl = (settings["AuthenApiBaseUrl"] ?? string.Empty).Trim();
            if (baseUrl.Length == 0)
            {
                baseUrl = DefaultBaseUrl;
            }
            Uri uri;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || (uri.AbsolutePath != "/" && uri.AbsolutePath.Length > 0)
                || uri.Query.Length > 0)
            {
                errors.Add("AuthenApiBaseUrl must be an https URL with no path, e.g. " + DefaultBaseUrl);
            }
            else
            {
                config.ApiBaseUri = uri;
            }

            // The dedicated fallback server (LDAPS, FQDN) is preferred. The legacy "LDAP" setting keeps
            // today's behaviour (Kerberos/NTLM bind with signing and sealing on 389) until it is set.
            string adServer = (settings["AuthenAdServer"] ?? string.Empty).Trim();
            if (adServer.Length > 0)
            {
                config.DirectoryServer = adServer;
                config.DirectoryUseLdaps = !string.Equals(settings["AuthenAdUseLdaps"], "false", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                config.DirectoryServer = (settings["LDAP"] ?? string.Empty).Trim();
                config.DirectoryUseLdaps = false;
            }

            int hours;
            int.TryParse(settings["AuthenOfflineWindowHours"], out hours);
            config.OfflineWindowHours = Math.Max(0, Math.Min(hours, 24));
            string pepper = settings["AuthenOfflinePepper"] ?? string.Empty;
            if (config.OfflineWindowHours > 0)
            {
                if (pepper.Length < 32)
                {
                    errors.Add("AuthenOfflinePepper must be at least 32 characters when AuthenOfflineWindowHours > 0");
                }
                else
                {
                    config.OfflinePepper = System.Text.Encoding.UTF8.GetBytes(pepper);
                }
            }

            if (errors.Count > 0)
            {
                config.ConfigurationError = string.Join("; ", errors);
            }
            return config;
        }
    }

    public sealed class AuthProfile
    {
        public string UserKey { get; set; }
        public string UserPrincipalName { get; set; }
        public string SamAccountName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public List<string> Groups { get; set; } = new List<string>();

        /// <summary>UPN lower-case, else sAMAccountName lower-case, else null (login must be refused).</summary>
        public static string BuildUserKey(string upn, string sam)
        {
            if (!string.IsNullOrWhiteSpace(upn))
            {
                return upn.Trim().ToLowerInvariant();
            }
            if (!string.IsNullOrWhiteSpace(sam))
            {
                return sam.Trim().ToLowerInvariant();
            }
            return null;
        }
    }
}
