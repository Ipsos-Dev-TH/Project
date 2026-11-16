using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Project.Security
{
    public enum LoginOutcome
    {
        Success,
        InvalidCredentials,
        AccountDisabled,
        RetryLater,
        ConfigurationError,
        Unavailable
    }

    public sealed class LoginResult
    {
        public LoginOutcome Outcome { get; set; }
        public AuthProfile Profile { get; set; }
        public string Role { get; set; }
        /// <summary>True when the password was checked by the AD fallback or the offline cache, not AuthenAPI.</summary>
        public bool Degraded { get; set; }
        /// <summary>online | ad-direct | ad-fallback | offline</summary>
        public string Mode { get; set; }

        public string UserMessage
        {
            get
            {
                switch (Outcome)
                {
                    case LoginOutcome.Success: return string.Empty;
                    case LoginOutcome.InvalidCredentials: return "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
                    case LoginOutcome.AccountDisabled: return "บัญชีนี้ถูกปิดการใช้งานในระบบ กรุณาติดต่อผู้ดูแลระบบ";
                    case LoginOutcome.RetryLater: return "มีการพยายามเข้าสู่ระบบบ่อยเกินไป กรุณาลองใหม่ภายหลัง";
                    case LoginOutcome.ConfigurationError: return "ระบบยืนยันตัวตนตั้งค่าไม่ถูกต้อง กรุณาติดต่อผู้ดูแลระบบ";
                    default: return "ระบบยืนยันตัวตนขัดข้องชั่วคราว กรุณาลองใหม่ภายหลัง";
                }
            }
        }
    }

    /// <summary>
    /// Login flow from the AuthenAPI integration guide:
    /// 1) AuthenAPI (always first)  2) one AD bind, only if AuthenAPI was unreachable or reported AD down
    /// 3) offline cache (≤ window), same condition  4) otherwise "temporarily unavailable".
    /// A 401, timeout or 500 from AuthenAPI ends the flow: no fallback.
    /// </summary>
    public static class LoginService
    {
        private static readonly Regex AllowedUsername = new Regex(@"^[A-Za-z0-9._@\\-]{1,256}$", RegexOptions.Compiled);

        public static LoginResult Login(string typedUsername, string password, string clientIp)
        {
            // Only surrounding whitespace is removed; the name is otherwise sent exactly as typed.
            string username = (typedUsername ?? string.Empty).Trim();
            AuthenConfig config = AuthenConfig.Current;
            LoginResult result;
            string detail;

            if (!AllowedUsername.IsMatch(username) || string.IsNullOrEmpty(password))
            {
                result = Fail(LoginOutcome.InvalidCredentials);
                detail = "Rejected locally: invalid username format or empty password";
            }
            else if (LoginThrottle.IsBlocked(username, clientIp))
            {
                result = Fail(LoginOutcome.RetryLater);
                detail = "Local throttle";
            }
            else if (config.ConfigurationError != null)
            {
                result = Fail(LoginOutcome.ConfigurationError);
                detail = config.ConfigurationError;
            }
            else if (!config.ApiConfigured)
            {
                result = DirectoryOnly(config, username, password, out detail);
            }
            else
            {
                result = ViaAuthenApi(config, username, password, out detail);
            }

            if (result.Outcome == LoginOutcome.Success)
            {
                LoginThrottle.Reset(username);
            }
            else if (result.Outcome == LoginOutcome.InvalidCredentials)
            {
                LoginThrottle.RecordFailure(username, clientIp);
            }

            Audit(username, clientIp, result, detail);
            return result;
        }

        private static LoginResult ViaAuthenApi(AuthenConfig config, string username, string password, out string detail)
        {
            if (!AuthenCircuitBreaker.TryAcquire())
            {
                return Fallback(config, username, password, "AuthenAPI circuit open", out detail);
            }

            ApiResult api = AuthenApiClient.Authenticate(config, username, password);
            detail = "AuthenAPI: " + api.Outcome + " (" + api.Detail + ")";

            switch (api.Outcome)
            {
                case ApiOutcome.Success:
                {
                    AuthenCircuitBreaker.RecordReached();
                    LoginResult ok = Succeed(api.Profile, false, "online");
                    if (config.OfflineEnabled)
                    {
                        RefreshOfflineCache(config, username, password, ok);
                    }
                    return ok;
                }
                case ApiOutcome.InvalidCredentials:
                    AuthenCircuitBreaker.RecordReached();
                    if (config.OfflineEnabled)
                    {
                        TryRemoveOffline(username, null);
                    }
                    return Fail(LoginOutcome.InvalidCredentials);
                case ApiOutcome.RateLimited:
                    AuthenCircuitBreaker.RecordReached();
                    return Fail(LoginOutcome.RetryLater);
                case ApiOutcome.ConfigurationError:
                    AuthenCircuitBreaker.RecordReached();
                    AlertTeam(detail);
                    return Fail(LoginOutcome.ConfigurationError);
                case ApiOutcome.Unreachable:
                case ApiOutcome.DirectoryDown:
                {
                    AuthenCircuitBreaker.RecordOutage();
                    string fallbackDetail;
                    LoginResult fallback = Fallback(config, username, password, detail, out fallbackDetail);
                    detail = fallbackDetail;
                    return fallback;
                }
                default:
                    // Timeout / 500 / 504 / reset: AuthenAPI may already have checked the password with AD.
                    AuthenCircuitBreaker.RecordInconclusive();
                    return Fail(LoginOutcome.Unavailable);
            }
        }

        /// <summary>Steps 2 and 3: only reached when AuthenAPI was never reached or reported AD down.</summary>
        private static LoginResult Fallback(AuthenConfig config, string username, string password, string reason, out string detail)
        {
            detail = reason;
            CachedIdentity cached = null;
            if (config.OfflineEnabled)
            {
                try
                {
                    cached = OfflineCredentialStore.Find(config, username);
                }
                catch (Exception ex)
                {
                    detail += "; offline cache lookup failed: " + ex.GetType().Name;
                }
            }

            if (config.DirectoryConfigured)
            {
                bool usedCachedUpn = cached != null && !string.IsNullOrWhiteSpace(cached.Profile.UserPrincipalName);
                string bindName = usedCachedUpn ? cached.Profile.UserPrincipalName : username;
                DirectoryResult ad = DirectoryAuthenticator.Bind(config, bindName, password);
                detail += "; AD: " + ad.Outcome + " (" + ad.Detail + (usedCachedUpn ? ", cached UPN" : ", typed name") + ")";

                switch (ad.Outcome)
                {
                    case DirectoryOutcome.Success:
                        return Succeed(ad.Profile, true, "ad-fallback");
                    case DirectoryOutcome.LookupFailed:
                        return cached != null ? Succeed(cached.Profile, true, "ad-fallback") : Fail(LoginOutcome.Unavailable);
                    case DirectoryOutcome.Rejected:
                        if (usedCachedUpn)
                        {
                            TryRemoveOffline(username, cached.UserKey);
                            return Fail(LoginOutcome.InvalidCredentials);
                        }
                        // With a typed name AD answers 52e both for a wrong password and for an unknown name.
                        return ad.SubCode == null || ad.SubCode == "52e"
                            ? Fail(LoginOutcome.Unavailable)
                            : Fail(LoginOutcome.InvalidCredentials);
                }
            }

            if (!config.OfflineEnabled || cached == null)
            {
                return Fail(LoginOutcome.Unavailable);
            }

            AuthProfile offlineProfile;
            OfflineOutcome offline;
            try
            {
                offline = OfflineCredentialStore.Verify(config, username, password, out offlineProfile);
            }
            catch (Exception ex)
            {
                detail += "; offline verify failed: " + ex.GetType().Name;
                return Fail(LoginOutcome.Unavailable);
            }
            detail += "; offline: " + offline;

            if (offline == OfflineOutcome.Mismatch)
            {
                return Fail(LoginOutcome.InvalidCredentials);
            }
            if (offline != OfflineOutcome.Success)
            {
                return Fail(LoginOutcome.Unavailable);
            }

            if (AppAuth.ResolveRole(offlineProfile.UserKey, offlineProfile.Groups) == AppAuth.RoleAdmin)
            {
                // Admin accounts never sign in offline.
                detail += "; admin account refused offline";
                return Fail(LoginOutcome.Unavailable);
            }
            return Succeed(offlineProfile, true, "offline");
        }

        /// <summary>Transitional mode while no AuthenAPI key is configured: AD is the primary check.</summary>
        private static LoginResult DirectoryOnly(AuthenConfig config, string username, string password, out string detail)
        {
            if (!config.DirectoryConfigured)
            {
                detail = "Neither AuthenApiKey nor an AD server is configured";
                AlertTeam(detail);
                return Fail(LoginOutcome.ConfigurationError);
            }

            DirectoryResult ad = DirectoryAuthenticator.Bind(config, username, password);
            detail = "AD direct (AuthenApiKey not set): " + ad.Outcome + " (" + ad.Detail + ")";
            switch (ad.Outcome)
            {
                case DirectoryOutcome.Success:
                    return Succeed(ad.Profile, false, "ad-direct");
                case DirectoryOutcome.Rejected:
                    return Fail(LoginOutcome.InvalidCredentials);
                default:
                    return Fail(LoginOutcome.Unavailable);
            }
        }

        private static LoginResult Succeed(AuthProfile profile, bool degraded, string mode)
        {
            string role = AppAuth.ResolveRole(profile.UserKey, profile.Groups);
            if (role == AppAuth.RoleDisabled)
            {
                return Fail(LoginOutcome.AccountDisabled);
            }
            if (degraded && role == AppAuth.RoleAdmin)
            {
                // Groups may be stale during an outage: degraded sessions never get admin rights.
                role = AppAuth.RoleEditor;
            }
            return new LoginResult { Outcome = LoginOutcome.Success, Profile = profile, Role = role, Degraded = degraded, Mode = mode };
        }

        private static void RefreshOfflineCache(AuthenConfig config, string username, string password, LoginResult result)
        {
            try
            {
                if (result.Outcome == LoginOutcome.Success && result.Role != AppAuth.RoleAdmin)
                {
                    OfflineCredentialStore.Save(config, username, password, result.Profile);
                }
                else
                {
                    OfflineCredentialStore.Remove(username, result.Profile != null ? result.Profile.UserKey : null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Offline cache update failed: " + ex.GetType().Name);
            }
        }

        private static void TryRemoveOffline(string username, string userKey)
        {
            try
            {
                OfflineCredentialStore.Remove(username, userKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Offline cache removal failed: " + ex.GetType().Name);
            }
        }

        private static LoginResult Fail(LoginOutcome outcome)
        {
            return new LoginResult { Outcome = outcome };
        }

        private static void AlertTeam(string detail)
        {
            System.Diagnostics.Trace.TraceError("AuthenAPI configuration error: " + detail);
        }

        private static void Audit(string username, string clientIp, LoginResult result, string detail)
        {
            string task;
            switch (result.Outcome)
            {
                case LoginOutcome.Success: task = result.Degraded ? "LoginDegraded" : "Login"; break;
                case LoginOutcome.ConfigurationError: task = "AuthConfigError"; break;
                case LoginOutcome.Unavailable: task = "AuthUnavailable"; break;
                default: task = "LoginFailed"; break;
            }
            string data = "user=" + username + ";ip=" + clientIp + ";outcome=" + result.Outcome
                + (result.Mode != null ? ";mode=" + result.Mode + ";role=" + result.Role : string.Empty)
                + ";detail=" + detail;
            string by = result.Profile != null && result.Profile.UserKey != null ? result.Profile.UserKey : username.ToLowerInvariant();
            try
            {
                new Core().Logs(ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString,
                    "Auth", task, data.Length > 2000 ? data.Substring(0, 2000) : data, by);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Login audit failed: " + ex.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Local limit of 5 failed logins per 15 minutes per username and per IP, so one person guessing
    /// cannot use up AuthenAPI's shared rate limit for every user of this system.
    /// </summary>
    public static class LoginThrottle
    {
        private const int MaxFailures = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Counter> Counters = new Dictionary<string, Counter>(StringComparer.Ordinal);

        private sealed class Counter
        {
            public DateTime WindowStartUtc;
            public int Failures;
        }

        public static bool IsBlocked(string username, string ip)
        {
            lock (Sync)
            {
                return Count(UserKey(username)) >= MaxFailures || Count(IpKey(ip)) >= MaxFailures;
            }
        }

        public static void RecordFailure(string username, string ip)
        {
            lock (Sync)
            {
                if (Counters.Count > 20000)
                {
                    Prune();
                }
                Increment(UserKey(username));
                Increment(IpKey(ip));
            }
        }

        public static void Reset(string username)
        {
            lock (Sync)
            {
                Counters.Remove(UserKey(username));
            }
        }

        private static string UserKey(string username)
        {
            return "u:" + (username ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string IpKey(string ip)
        {
            return "ip:" + (ip ?? string.Empty);
        }

        private static int Count(string key)
        {
            Counter counter;
            if (!Counters.TryGetValue(key, out counter))
            {
                return 0;
            }
            if (DateTime.UtcNow - counter.WindowStartUtc > Window)
            {
                Counters.Remove(key);
                return 0;
            }
            return counter.Failures;
        }

        private static void Increment(string key)
        {
            Counter counter;
            if (!Counters.TryGetValue(key, out counter) || DateTime.UtcNow - counter.WindowStartUtc > Window)
            {
                counter = new Counter { WindowStartUtc = DateTime.UtcNow };
                Counters[key] = counter;
            }
            counter.Failures++;
        }

        private static void Prune()
        {
            var expired = new List<string>();
            foreach (KeyValuePair<string, Counter> pair in Counters)
            {
                if (DateTime.UtcNow - pair.Value.WindowStartUtc > Window)
                {
                    expired.Add(pair.Key);
                }
            }
            foreach (string key in expired)
            {
                Counters.Remove(key);
            }
        }
    }
}
