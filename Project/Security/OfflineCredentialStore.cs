using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Project.Security
{
    public enum OfflineOutcome
    {
        Success,
        Mismatch,
        /// <summary>No entry, expired, too many failures, rate limit hit, or store not reachable.</summary>
        NotAvailable
    }

    public sealed class CachedIdentity
    {
        public string UserKey { get; set; }
        public AuthProfile Profile { get; set; }
    }

    /// <summary>
    /// Credentials of users who logged in online within the window, used only when AuthenAPI and AD
    /// are both unreachable. Written only after AuthenAPI answers 200. Off unless
    /// AuthenOfflineWindowHours &gt; 0 and AuthenOfflinePepper is set.
    /// </summary>
    public static class OfflineCredentialStore
    {
        private const int MaxFailuresPerUser = 5;
        private const int MaxFailuresPerMinute = 20;

        private static readonly object RateSync = new object();
        private static DateTime _rateWindowStartUtc = DateTime.UtcNow;
        private static int _rateWindowFailures;

        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

        public static void Save(AuthenConfig config, string typedName, string password, AuthProfile profile)
        {
            string hash = OfflinePasswordHasher.Hash(password, config.OfflinePepper);
            string profileJson = new JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                { "upn", profile.UserPrincipalName },
                { "sam", profile.SamAccountName },
                { "displayName", profile.DisplayName },
                { "email", profile.Email },
                { "groups", profile.Groups.ToArray() }
            });

            const string sql = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DELETE FROM dbo.AuthOfflineCache WHERE LastOnlineVerifiedUtc < DATEADD(hour, -@WindowHours, SYSUTCDATETIME());
UPDATE dbo.AuthOfflineCache WITH (UPDLOCK, SERIALIZABLE)
   SET PasswordHash = @Hash, ProfileJson = @Profile, LastOnlineVerifiedUtc = SYSUTCDATETIME(), OfflineFailures = 0
 WHERE UserKey = @UserKey;
IF @@ROWCOUNT = 0
    INSERT INTO dbo.AuthOfflineCache (UserKey, PasswordHash, ProfileJson, LastOnlineVerifiedUtc, OfflineFailures)
    VALUES (@UserKey, @Hash, @Profile, SYSUTCDATETIME(), 0);
UPDATE dbo.AuthOfflineAlias WITH (UPDLOCK, SERIALIZABLE) SET UserKey = @UserKey WHERE LoginName = @LoginName;
IF @@ROWCOUNT = 0
    INSERT INTO dbo.AuthOfflineAlias (LoginName, UserKey) VALUES (@LoginName, @UserKey);
COMMIT TRANSACTION;";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@WindowHours", config.OfflineWindowHours);
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Profile", profileJson);
                cmd.Parameters.AddWithValue("@UserKey", profile.UserKey);
                cmd.Parameters.AddWithValue("@LoginName", NormalizeLogin(typedName));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Deletes every cached row for this person (found by the typed name's alias or by user key).</summary>
        public static void Remove(string typedName, string userKey)
        {
            const string sql = @"
DELETE FROM dbo.AuthOfflineCache
 WHERE UserKey = @UserKey
    OR UserKey IN (SELECT UserKey FROM dbo.AuthOfflineAlias WHERE LoginName = @LoginName);";
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserKey", (object)userKey ?? string.Empty);
                cmd.Parameters.AddWithValue("@LoginName", NormalizeLogin(typedName));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static CachedIdentity Find(AuthenConfig config, string typedName)
        {
            const string sql = @"
SELECT c.UserKey, c.ProfileJson
  FROM dbo.AuthOfflineAlias a
  JOIN dbo.AuthOfflineCache c ON c.UserKey = a.UserKey
 WHERE a.LoginName = @LoginName
   AND c.LastOnlineVerifiedUtc >= DATEADD(hour, -@WindowHours, SYSUTCDATETIME());";
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@LoginName", NormalizeLogin(typedName));
                cmd.Parameters.AddWithValue("@WindowHours", config.OfflineWindowHours);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }
                    string userKey = reader.GetString(0);
                    return new CachedIdentity { UserKey = userKey, Profile = ParseProfile(userKey, reader.GetString(1)) };
                }
            }
        }

        public static OfflineOutcome Verify(AuthenConfig config, string typedName, string password, out AuthProfile profile)
        {
            profile = null;
            if (!TryTakeGlobalAttempt())
            {
                return OfflineOutcome.NotAvailable;
            }

            // Reserve one attempt atomically before checking the hash, so parallel requests cannot exceed the limit.
            const string reserveSql = @"
UPDATE c SET OfflineFailures = c.OfflineFailures + 1
OUTPUT inserted.UserKey, inserted.PasswordHash, inserted.ProfileJson
  FROM dbo.AuthOfflineCache c
  JOIN dbo.AuthOfflineAlias a ON a.UserKey = c.UserKey
 WHERE a.LoginName = @LoginName
   AND c.OfflineFailures < @MaxFailures
   AND c.LastOnlineVerifiedUtc >= DATEADD(hour, -@WindowHours, SYSUTCDATETIME());";

            string userKey, storedHash, profileJson;
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(reserveSql, conn))
                {
                    cmd.Parameters.AddWithValue("@LoginName", NormalizeLogin(typedName));
                    cmd.Parameters.AddWithValue("@MaxFailures", MaxFailuresPerUser);
                    cmd.Parameters.AddWithValue("@WindowHours", config.OfflineWindowHours);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return OfflineOutcome.NotAvailable;
                        }
                        userKey = reader.GetString(0);
                        storedHash = reader.GetString(1);
                        profileJson = reader.GetString(2);
                    }
                }

                if (!OfflinePasswordHasher.Verify(password, config.OfflinePepper, storedHash))
                {
                    RecordGlobalFailure();
                    return OfflineOutcome.Mismatch;
                }

                using (var reset = new SqlCommand("UPDATE dbo.AuthOfflineCache SET OfflineFailures = 0 WHERE UserKey = @UserKey", conn))
                {
                    reset.Parameters.AddWithValue("@UserKey", userKey);
                    reset.ExecuteNonQuery();
                }
            }

            profile = ParseProfile(userKey, profileJson);
            return OfflineOutcome.Success;
        }

        private static AuthProfile ParseProfile(string userKey, string json)
        {
            var profile = new AuthProfile { UserKey = userKey };
            var data = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (data == null)
            {
                return profile;
            }
            profile.UserPrincipalName = Value(data, "upn");
            profile.SamAccountName = Value(data, "sam");
            profile.DisplayName = Value(data, "displayName");
            profile.Email = Value(data, "email");
            object groups;
            if (data.TryGetValue("groups", out groups) && groups is object[])
            {
                foreach (object g in (object[])groups)
                {
                    string name = Convert.ToString(g);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        profile.Groups.Add(name);
                    }
                }
            }
            return profile;
        }

        private static string Value(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty;
        }

        private static string NormalizeLogin(string typedName)
        {
            return (typedName ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool TryTakeGlobalAttempt()
        {
            lock (RateSync)
            {
                RollWindow();
                return _rateWindowFailures < MaxFailuresPerMinute;
            }
        }

        private static void RecordGlobalFailure()
        {
            lock (RateSync)
            {
                RollWindow();
                _rateWindowFailures++;
            }
        }

        private static void RollWindow()
        {
            if (DateTime.UtcNow - _rateWindowStartUtc >= TimeSpan.FromMinutes(1))
            {
                _rateWindowStartUtc = DateTime.UtcNow;
                _rateWindowFailures = 0;
            }
        }
    }

    /// <summary>HMAC-SHA256 with a pepper kept outside the database, then PBKDF2-SHA256 (600,000 rounds, per-user salt).</summary>
    public static class OfflinePasswordHasher
    {
        private const int Iterations = 600000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const string Version = "v1";

        public static string Hash(string password, byte[] pepper)
        {
            byte[] salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] derived = Derive(password, pepper, salt, Iterations);
            return Version + "$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derived);
        }

        public static bool Verify(string password, byte[] pepper, string stored)
        {
            string[] parts = (stored ?? string.Empty).Split('$');
            int iterations;
            if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out iterations) || iterations < 100000)
            {
                return false;
            }
            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }
            byte[] actual = Derive(password, pepper, salt, iterations);
            return FixedTimeEquals(actual, expected);
        }

        private static byte[] Derive(string password, byte[] pepper, byte[] salt, int iterations)
        {
            byte[] peppered;
            using (var hmac = new HMACSHA256(pepper))
            {
                peppered = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
            using (var kdf = new Rfc2898DeriveBytes(peppered, salt, iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(HashBytes);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
