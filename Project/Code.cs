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
    public class Code
    {
    }
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
                catch { }
            }
            return dt;
        }



        public int CheckPermission (string conn, string colmnname , string Email)
        {
            int check = 0;

            DataTable dt = DatabaseQuery(conn, "SELECT * FROM [Payment].[dbo].[User] inner join [Role] on Role_ID = [Role].ID Where [User] = '"+Email.ToLower()+"'");
            try
            {
                check = Convert.ToInt32(dt.Rows[0][colmnname].ToString());
            }
            catch { check = -1; }
            return check;

        }

        public void DatabaseInsert(string connStr, string cmd)
        {
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    cmd = cmd.Replace("&nbsp;", "");
                    cmd = cmd.Replace("&amp;", "&");
                    connection.Open(); 
                    int result = command.ExecuteNonQuery();
                    connection.Close();
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
            return Convert.ToBase64String((Encoding.Unicode.GetBytes(text)));
        }

        public string Derypt(string text)
        {
            return Encoding.Unicode.GetString((Convert.FromBase64String(text)));
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

        public int CheckProjectCostQuantity(string conn, string Project_Cost_ID,DataTable dtInput,string quantitycolumnname)
        {

            DataTable dtQuantityLimit = DatabaseQuery(conn, "SELECT * FROM [Payment].[dbo].[Project_Cost] Where ID = " + Project_Cost_ID);

            DataTable dtPayment = DatabaseQuery(conn, "SELECT * FROM [Payment].[dbo].[Payment] Where Project_Cost_ID = " + Project_Cost_ID + " AND (Status != '" + status_deleted + "' AND Status != '" + status_declined + "' AND Status != '" + status_predeclined + "')");
            DataTable dtWorkSheet = DatabaseQuery(conn, "SELECT *  FROM [Payment].[dbo].[TemporaryEmployee_Worksheet] Where Project_Cost_ID = " + Project_Cost_ID + " AND (Status != 'MAKE PAYMENT' AND Status != '" + status_deleted + "' AND Status != '" + status_declined + "'  AND Status != '" + status_declined + "')");

            int QuantityUsed = 0;
            for(int i = 0;i<dtPayment.Rows.Count;i++)
            {
                QuantityUsed += Int32.Parse(dtPayment.Rows[i]["Payment_Quantity"].ToString());
            }
            for (int i = 0; i < dtWorkSheet.Rows.Count; i++)
            {
                QuantityUsed += Int32.Parse(dtWorkSheet.Rows[i]["Quantity"].ToString());
            }
            for (int i = 0; i < dtInput.Rows.Count; i++)
            {
                QuantityUsed += Int32.Parse(dtInput.Rows[i][quantitycolumnname].ToString());
            }

            return QuantityUsed;
        }
    }
}