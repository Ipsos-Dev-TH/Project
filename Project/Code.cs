using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Configuration;
using System.Net;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Project
{
    public class Core
    {
        public string status_new = "NEW";
        public string status_approved = "APPROVED";
        public string status_declined = "DECLINED";
        public string status_preapproved = "PRE-APPROVED";
        public string status_predeclined = "PRE-DECLINED";
        public string status_deleted = "DELETED";

        public DataTable DatabaseQuery(string connStr, string cmd)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                try
                {
                    cmd = cmd.Replace("&amp;", "&");
                    cmd = cmd.Replace("&#39;", "''");
                    cmd = cmd.Replace("&nbsp;", "");

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd, con);
                    adapter.Fill(dt);
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL Error in DatabaseQuery: {ex.Message}");
                }
            }
            return dt;
        }

        public DataTable DatabaseQueryWithParams(string connStr, string cmd, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                try
                {
                    using (SqlCommand command = new SqlCommand(cmd, con))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        adapter.Fill(dt);
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL Error in DatabaseQueryWithParams: {ex.Message}");
                }
            }
            return dt;
        }

        public int CheckPermission(string conn, string columnName, string email)
        {
            int check = 0;

            // Validate column name against whitelist to prevent SQL injection
            string[] allowedColumns = { "CanView", "CanEdit", "CanDelete", "CanApprove", "IsAdmin" };
            if (!Array.Exists(allowedColumns, col => col.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
            {
                return -1;
            }

            string query = $"SELECT * FROM [Payment].[dbo].[User] INNER JOIN [Role] ON Role_ID = [Role].ID WHERE [User] = @Email";
            DataTable dt = DatabaseQueryWithParams(conn, query, new SqlParameter("@Email", email.ToLower()));

            try
            {
                if (dt.Rows.Count > 0)
                {
                    check = Convert.ToInt32(dt.Rows[0][columnName].ToString());
                }
                else
                {
                    check = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckPermission: {ex.Message}");
                check = -1;
            }
            return check;
        }

        public void DatabaseInsert(string connStr, string cmd)
        {
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    try
                    {
                        cmd = cmd.Replace("&nbsp;", "");
                        cmd = cmd.Replace("&amp;", "&");
                        command.CommandText = cmd;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SQL Error in DatabaseInsert: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public void DatabaseInsertWithParams(string connStr, string cmd, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    try
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SQL Error in DatabaseInsertWithParams: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public static int ExecuteScalar(string connStr, string commandText, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connStr))
            using (SqlCommand command = new SqlCommand(commandText + "; SELECT SCOPE_IDENTITY();", connection))
            {
                command.CommandType = commandType;
                command.Parameters.AddRange(parameters);
                
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
        string IPAddress = "";

        public string GetIPAddress()
        {
            try
            {
                IPHostEntry Host = default(IPHostEntry);
                string Hostname = null;
                Hostname = System.Environment.MachineName;
                Host = Dns.GetHostEntry(Hostname);
                foreach (IPAddress IP in Host.AddressList)
                {
                    if (IP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        IPAddress = Convert.ToString(IP);
                    }
                }
            }
            catch { }
            return IPAddress;
        }
        public void Logs(string connStr, string Action_on_Table, string Task,string Data, string Action_By)
        {
            string cmd = "INSERT INTO [dbo].[Logs] ([Action_on_Table],[Task],[Data],[Action_By],[Action_DateTime]) VALUES (@a,@b,@c,@d,@e)";
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    command.Parameters.AddWithValue("@a", Action_on_Table);
                    command.Parameters.AddWithValue("@b", Task);
                    command.Parameters.AddWithValue("@c", Data);
                    command.Parameters.AddWithValue("@d", Action_By);
                    command.Parameters.AddWithValue("@e", DateTime.Now);
                   
                    connection.Open();
                    int result = command.ExecuteNonQuery();
                    connection.Close();

                }
            }
        }

        public void SendEmail(string from, string to, string cc, string subject, string body, Attachment[] data)
        {
            MailMessage mail = new MailMessage(from, to);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "pmta.extranet.iext";
            try
            {
                mail.CC.Add(cc);
            }
            catch { }
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;
            try
            {
                for (int i = 0; i < data.Length; i++)
                {
                    mail.Attachments.Add(data[i]);
                }

            }
            catch { }
            client.Send(mail);
        }

        public string GetURIparameter(string parametername,string URI)
        {
            Uri myUri = new Uri(URI);
            string output = HttpUtility.ParseQueryString(myUri.Query).Get(parametername);

            return output;
        }

        public string Crypt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(text));
        }

        public string Decrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            try
            {
                return Encoding.Unicode.GetString(Convert.FromBase64String(text));
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        [Obsolete("Use Decrypt instead")]
        public string Derypt(string text)
        {
            return Decrypt(text);
        }

        public byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.Zeros;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        public string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.Zeros;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }

        public int CheckProjectCostQuantity(string conn, string projectCostId, DataTable dtInput, string quantityColumnName)
        {
            // Validate projectCostId is a valid integer to prevent SQL injection
            if (!int.TryParse(projectCostId, out int costId))
            {
                return 0;
            }

            string queryQuantityLimit = "SELECT * FROM [Payment].[dbo].[Project_Cost] WHERE ID = @CostId";
            DataTable dtQuantityLimit = DatabaseQueryWithParams(conn, queryQuantityLimit,
                new SqlParameter("@CostId", costId));

            string queryPayment = @"SELECT * FROM [Payment].[dbo].[Payment]
                                    WHERE Project_Cost_ID = @CostId
                                    AND Status NOT IN (@StatusDeleted, @StatusDeclined, @StatusPreDeclined)";
            DataTable dtPayment = DatabaseQueryWithParams(conn, queryPayment,
                new SqlParameter("@CostId", costId),
                new SqlParameter("@StatusDeleted", status_deleted),
                new SqlParameter("@StatusDeclined", status_declined),
                new SqlParameter("@StatusPreDeclined", status_predeclined));

            string queryWorkSheet = @"SELECT * FROM [Payment].[dbo].[TemporaryEmployee_Worksheet]
                                      WHERE Project_Cost_ID = @CostId
                                      AND Status NOT IN ('MAKE PAYMENT', @StatusDeleted, @StatusDeclined)";
            DataTable dtWorkSheet = DatabaseQueryWithParams(conn, queryWorkSheet,
                new SqlParameter("@CostId", costId),
                new SqlParameter("@StatusDeleted", status_deleted),
                new SqlParameter("@StatusDeclined", status_declined));

            int quantityUsed = 0;

            foreach (DataRow row in dtPayment.Rows)
            {
                if (int.TryParse(row["Payment_Quantity"]?.ToString(), out int qty))
                {
                    quantityUsed += qty;
                }
            }

            foreach (DataRow row in dtWorkSheet.Rows)
            {
                if (int.TryParse(row["Quantity"]?.ToString(), out int qty))
                {
                    quantityUsed += qty;
                }
            }

            if (dtInput != null && dtInput.Columns.Contains(quantityColumnName))
            {
                foreach (DataRow row in dtInput.Rows)
                {
                    if (int.TryParse(row[quantityColumnName]?.ToString(), out int qty))
                    {
                        quantityUsed += qty;
                    }
                }
            }

            return quantityUsed;
        }
    }
}