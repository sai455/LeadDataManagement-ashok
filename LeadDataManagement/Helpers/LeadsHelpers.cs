using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LeadDataManagement.Helpers
{
    public static class LeadsHelpers
    {
        public static string ToUsNumberFormat(long inputVal)
        {
            try
            {
                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                return inputVal.ToString("N", nfi).Split('.')[0];
            }
            catch(Exception ex)
            {
                return inputVal.ToString();
            }
        }
        public static string ProcessNumber(string inputVal)
        {
            try
            {

               var str=Regex.Replace(inputVal, @"[^0-9a-zA-Z:,]+", "");
                long number;
                bool isSuccess = Int64.TryParse(str, out number);
                if (isSuccess)
                {
                    if (str.Length > 10)
                    {
                        var trimPos = str.Length - 10;
                        str = str.Substring(trimPos);
                    }
                    return str;
                }
                else
                {
                    return inputVal;
                }
            }
            catch (Exception ex)
            {
                return inputVal.ToString();
            }
        }
        public static string Encrypt(string clearText)
        {
            string EncryptionKey = "LEADS2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = "LEADS2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
   
        public static void SendEmail(string userName, string Email, string NickName)
        {
            string fromEmail = ConfigurationManager.AppSettings["emailFrom"];
            string fromPassword = ConfigurationManager.AppSettings["emailFromPassword"];
            string toEmail = ConfigurationManager.AppSettings["toEmail"];
            var fromAddress = new MailAddress(fromEmail, "SNK Scrub Tools");
            var toAddress = new MailAddress(toEmail, "SNK Scrub Tools");
            string subject = "Approval Request For SNK Scrub Tools User -" + userName;
            StringBuilder sb = new StringBuilder();
            sb.Append("Hi Team,");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("New User Registration with below details is awaiting for your approval.");
            sb.Append(Environment.NewLine);
            sb.Append("User Name: " + userName);
            sb.Append(Environment.NewLine);
            sb.Append("Email: " + Email);
            sb.Append(Environment.NewLine);

            string body = sb.ToString();
            MailMessage msg = new MailMessage();
            msg.To.Add(new MailAddress(toEmail));
            msg.From = new MailAddress(fromEmail);
            msg.Subject = subject;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.Body = sb.ToString();
            msg.BodyEncoding = Encoding.UTF8;
            msg.IsBodyHtml = false;
            msg.Priority = MailPriority.High;

            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential(fromEmail, fromPassword);
            client.Port = 587;//or use 587            
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            try
            {

                client.Send(msg);

            }
            catch (SmtpException ex)
            {
                throw ex;
            }
            if (msg != null) { msg.Dispose(); }
            client = null;
            msg = null;
        }
    }
}