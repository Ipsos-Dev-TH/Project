using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Project.Security
{
    public enum ApiOutcome
    {
        /// <summary>200 JSON success.</summary>
        Success,
        /// <summary>401 JSON without reason, or 400. Never fall back.</summary>
        InvalidCredentials,
        /// <summary>429.</summary>
        RateLimited,
        /// <summary>401 with reason, 401 HTML, 403, 404, 405, 3xx, or no usable user key.</summary>
        ConfigurationError,
        /// <summary>Timeout, 500, 504, reset after sending, or 200 that is not AuthenAPI JSON. AuthenAPI may already have checked AD: never fall back.</summary>
        Indeterminate,
        /// <summary>Request never reached the app: connect/DNS/TLS failure, 502, 503 from IIS. Fallback allowed.</summary>
        Unreachable,
        /// <summary>503 JSON: AuthenAPI is up but cannot reach AD (password not checked). Fallback allowed.</summary>
        DirectoryDown
    }

    public sealed class ApiResult
    {
        public ApiOutcome Outcome { get; set; }
        public AuthProfile Profile { get; set; }
        /// <summary>For logs only; never contains the password.</summary>
        public string Detail { get; set; }
    }

    /// <summary>
    /// POST /api/ldap/authenticate. No automatic retry, no redirects, 45-second timeout,
    /// certificate validation left on, and no keep-alive so a dropped pooled connection is never re-sent.
    /// </summary>
    public static class AuthenApiClient
    {
        private const int TimeoutMs = 45000;
        private const int MaxResponseBytes = 256 * 1024;

        public static ApiResult Authenticate(AuthenConfig config, string username, string password)
        {
            var serializer = new JavaScriptSerializer();
            byte[] body = Encoding.UTF8.GetBytes(serializer.Serialize(new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            }));

            HttpWebRequest request;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(new Uri(config.ApiBaseUri, "/api/ldap/authenticate"));
            }
            catch (Exception ex)
            {
                return Result(ApiOutcome.ConfigurationError, "Invalid AuthenAPI URL: " + ex.GetType().Name);
            }

            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.UserAgent = config.UserAgent;
            request.Headers["X-API-Key"] = config.ApiKey;
            request.AllowAutoRedirect = false;
            request.KeepAlive = false;
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.ServicePoint.Expect100Continue = false;
            request.ContentLength = body.Length;

            try
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(body, 0, body.Length);
                }
            }
            catch (WebException ex)
            {
                return IsNotConnected(ex.Status)
                    ? Result(ApiOutcome.Unreachable, "Connect failed: " + ex.Status)
                    : Result(ApiOutcome.Indeterminate, "Send failed: " + ex.Status);
            }

            HttpWebResponse response = null;
            try
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                    if (response == null)
                    {
                        if (ex.Status == WebExceptionStatus.Timeout)
                        {
                            return Result(ApiOutcome.Indeterminate, "Timeout after " + (TimeoutMs / 1000) + "s");
                        }
                        return IsNotConnected(ex.Status)
                            ? Result(ApiOutcome.Unreachable, "Connect failed: " + ex.Status)
                            : Result(ApiOutcome.Indeterminate, "No response: " + ex.Status);
                    }
                }

                string text;
                try
                {
                    text = ReadBody(response);
                }
                catch (Exception ex)
                {
                    return Result(ApiOutcome.Indeterminate, "Response read failed (" + (int)response.StatusCode + "): " + ex.GetType().Name);
                }
                return Classify(response, text, serializer);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        private static ApiResult Classify(HttpWebResponse response, string text, JavaScriptSerializer serializer)
        {
            int status = (int)response.StatusCode;
            Dictionary<string, object> json = TryParseAuthenJson(response.ContentType, text, serializer);
            string where = "HTTP " + status + (json == null ? " (not AuthenAPI JSON)" : string.Empty);

            if (status == 200)
            {
                if (json == null || !Equals(json["success"], true))
                {
                    return Result(ApiOutcome.Indeterminate, where);
                }
                var data = json.ContainsKey("data") ? json["data"] as Dictionary<string, object> : null;
                if (data == null)
                {
                    return Result(ApiOutcome.Indeterminate, where + " without data");
                }
                if (data.ContainsKey("isEnabled") && Equals(data["isEnabled"], false))
                {
                    return Result(ApiOutcome.InvalidCredentials, where + " but account disabled");
                }
                AuthProfile profile = ToProfile(data);
                if (profile.UserKey == null)
                {
                    return Result(ApiOutcome.ConfigurationError, where + " without userPrincipalName or samAccountName");
                }
                return new ApiResult { Outcome = ApiOutcome.Success, Profile = profile, Detail = where };
            }

            if (status == 400)
            {
                return Result(ApiOutcome.InvalidCredentials, where);
            }
            if (status == 401)
            {
                if (json == null)
                {
                    return Result(ApiOutcome.ConfigurationError, where);
                }
                string reason = json.ContainsKey("reason") ? Convert.ToString(json["reason"]) : null;
                return string.IsNullOrWhiteSpace(reason)
                    ? Result(ApiOutcome.InvalidCredentials, where)
                    : Result(ApiOutcome.ConfigurationError, where + " API key: " + reason);
            }
            if (status == 429)
            {
                return Result(ApiOutcome.RateLimited, where + " Retry-After=" + response.Headers["Retry-After"]);
            }
            if (status == 502)
            {
                return Result(ApiOutcome.Unreachable, where);
            }
            if (status == 503)
            {
                return json != null
                    ? Result(ApiOutcome.DirectoryDown, where + " directory unavailable")
                    : Result(ApiOutcome.Unreachable, where);
            }
            if (status == 403 || status == 404 || status == 405 || (status >= 300 && status < 400))
            {
                return Result(ApiOutcome.ConfigurationError, where);
            }
            return Result(ApiOutcome.Indeterminate, where);
        }

        private static bool IsNotConnected(WebExceptionStatus status)
        {
            return status == WebExceptionStatus.ConnectFailure
                || status == WebExceptionStatus.NameResolutionFailure
                || status == WebExceptionStatus.ProxyNameResolutionFailure
                || status == WebExceptionStatus.SecureChannelFailure
                || status == WebExceptionStatus.TrustFailure;
        }

        private static string ReadBody(HttpWebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    return string.Empty;
                }
                var buffer = new MemoryStream();
                var chunk = new byte[8192];
                int read;
                while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (buffer.Length + read > MaxResponseBytes)
                    {
                        throw new InvalidDataException("Response too large");
                    }
                    buffer.Write(chunk, 0, read);
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private static Dictionary<string, object> TryParseAuthenJson(string contentType, string text, JavaScriptSerializer serializer)
        {
            if (string.IsNullOrEmpty(contentType) || contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }
            try
            {
                var json = serializer.DeserializeObject(text) as Dictionary<string, object>;
                return json != null && json.ContainsKey("success") ? json : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static AuthProfile ToProfile(Dictionary<string, object> data)
        {
            string upn = Text(data, "userPrincipalName");
            string sam = Text(data, "samAccountName");
            var profile = new AuthProfile
            {
                UserPrincipalName = upn,
                SamAccountName = sam,
                DisplayName = Text(data, "displayName"),
                Email = Text(data, "email"),
                UserKey = AuthProfile.BuildUserKey(upn, sam)
            };
            object groups;
            if (data.TryGetValue("groups", out groups) && groups is IEnumerable && !(groups is string))
            {
                foreach (object g in (IEnumerable)groups)
                {
                    string name = Convert.ToString(g);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        profile.Groups.Add(name.Trim());
                    }
                }
            }
            return profile;
        }

        private static string Text(Dictionary<string, object> data, string key)
        {
            object value;
            if (!data.TryGetValue(key, out value) || value == null)
            {
                return string.Empty;
            }
            return Convert.ToString(value).Trim();
        }

        private static ApiResult Result(ApiOutcome outcome, string detail)
        {
            return new ApiResult { Outcome = outcome, Detail = detail };
        }
    }

    /// <summary>
    /// One breaker for the whole app. Opens after 3 consecutive "unreachable" / 503-JSON results,
    /// skips the API for 30 seconds, then lets exactly one trial request through (half-open).
    /// 401, timeout and 500 are not evidence of an outage and do not count.
    /// </summary>
    public static class AuthenCircuitBreaker
    {
        private const int Threshold = 3;
        private static readonly TimeSpan OpenFor = TimeSpan.FromSeconds(30);
        private static readonly object Sync = new object();
        private static int _consecutiveOutages;
        private static DateTime _openUntilUtc = DateTime.MinValue;
        private static bool _trialInFlight;

        public static bool TryAcquire()
        {
            lock (Sync)
            {
                if (_consecutiveOutages < Threshold)
                {
                    return true;
                }
                if (DateTime.UtcNow < _openUntilUtc || _trialInFlight)
                {
                    return false;
                }
                _trialInFlight = true;
                return true;
            }
        }

        public static void RecordReached()
        {
            lock (Sync)
            {
                _consecutiveOutages = 0;
                _trialInFlight = false;
                _openUntilUtc = DateTime.MinValue;
            }
        }

        public static void RecordOutage()
        {
            lock (Sync)
            {
                _trialInFlight = false;
                _consecutiveOutages++;
                if (_consecutiveOutages >= Threshold)
                {
                    _openUntilUtc = DateTime.UtcNow.Add(OpenFor);
                }
            }
        }

        public static void RecordInconclusive()
        {
            lock (Sync)
            {
                _trialInFlight = false;
            }
        }

        public static bool IsOpen
        {
            get
            {
                lock (Sync)
                {
                    return _consecutiveOutages >= Threshold;
                }
            }
        }
    }
}
