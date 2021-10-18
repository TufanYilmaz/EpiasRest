using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Telemeter.Extensions;

namespace EpiasRest
{
    public class Mailer
    {
        private SmtpClient Smtp = null;
        private NetworkCredential Credential = null;
        public Mailer()
        {
            Smtp = new SmtpClient();
            Credential = new NetworkCredential();
            Smtp.Credentials = Credential;
            Smtp.Port = 587;
        }
        public static Mailer Instance = getMailer("abysis");

        public static Mailer getMailer(string UserName)
        {
            Mailer result = new Mailer();


            result.Code = UserName;
            var table = new EpiasDataManager().GetMailTable(UserName);
            bool ServerIsSet = false;
            bool PortIsSet = false;
            bool UserIsSet = false;
            bool PasswordIsSet = false;
            bool UseSSLIsSet = false;
            foreach (DataRow r in table.Rows)
            {
                string pname = r["PROPERTYNAME"].ToString().Trim();
                string pvalue = r["PROPERTYVALUE"].ToString().Trim();
                bool isEncode = Convert.ToBoolean(r["ISENCODE"].ToString());
                if (isEncode) pvalue = pvalue.fromBase64();
                switch (pname)
                {
                    case "MailSMTPServer":
                        if (!ServerIsSet)
                        {
                            result.Host = pvalue;
                            ServerIsSet = true;
                        }
                        break;
                    case "MailSMTPPort":
                        if (!PortIsSet)
                        {
                            result.Port = Convert.ToInt32(pvalue);
                            PortIsSet = true;
                        }
                        break;
                    case "MailUser":
                        if (!UserIsSet)
                        {
                            result.UserName = pvalue;
                            UserIsSet = true;
                        }
                        break;
                    case "MailPassword":
                        if (!PasswordIsSet)
                        {
                            result.Password = pvalue;
                            PasswordIsSet = true;
                        }
                        break;
                    case "MailUseSSLSecurity":
                        if (!UseSSLIsSet)
                        {
                            result.UseSSL = Convert.ToBoolean(pvalue);
                            UseSSLIsSet = true;
                        }
                        break;
                }
            }
            table.Dispose();
            table = null;
            if (!ServerIsSet) throw new Exception("SMTP Server name not defined, please define in Abysis general parameters for user.");
            if (!UserIsSet) throw new Exception("Email UserName not defined, please define in Abysis general parameters for user.");
            if (!PasswordIsSet) throw new Exception("EMail Password not defined, please define in Abysis general parameters for user.");
            if (!PortIsSet) throw new Exception("EMail SMTP port number not defined, please define in Abysis general parameters for user.");

            return result;
        }


        public string Code { get; set; }
        public string Host
        {
            get
            {
                return Smtp.Host;
            }
            set
            {
                Smtp.Host = value;
            }
        }
        public int Port
        {
            get
            {
                return Smtp.Port;
            }
            set
            {
                Smtp.Port = value;
            }
        }

        public string UserName
        {
            get
            {
                return Credential.UserName;
            }
            set
            {
                Credential.UserName = value;
            }
        }
        public string Password
        {
            get
            {
                return Credential.Password;
            }
            set
            {
                Credential.Password = value;
            }
        }
        public bool UseSSL
        {
            get
            {
                bool result = false;
                if (Smtp != null) result = Smtp.EnableSsl;
                return result;
            }
            set
            {
                if (Smtp != null)
                    Smtp.EnableSsl = value;
            }
        }

        public bool Send(string MailTo, string Subject, string Body, DataTable EpiasAttachment = null, string Cc = "", string BCc = "")
        {
            bool result = false;
            string[] Recipients = MailTo.Split(new string[] { ",", ";", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] CcRecipients = Cc.Split(new string[] { ",", ";", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] BCcRecipients = BCc.Split(new string[] { ",", ";", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            Stream epiasStream = new MemoryStream();
            if (Recipients.Count() > 0)
            {
                using (var message = new MailMessage
                {
                    From = new MailAddress(UserName),
                    Subject = Subject,
                    Body = Body
                })
                {
                    for (int i = 0; i < Recipients.Count(); i++)
                        message.To.Add(new MailAddress(Recipients[i]));
                    for (int i = 0; i < CcRecipients.Count(); i++)
                        message.CC.Add(new MailAddress(CcRecipients[i]));
                    for (int i = 0; i < BCcRecipients.Count(); i++)
                    {
                        if (!string.IsNullOrEmpty(BCcRecipients[i]) && !string.IsNullOrWhiteSpace(BCcRecipients[i]))
                        {
                            try
                            {
                                message.Bcc.Add(new MailAddress(BCcRecipients[i].Trim()));
                            }
                            catch (Exception ex)
                            {
                                Helper.log.WriteLogLine(ex.ToString());
                            }
                        }
                    }
                    if (EpiasAttachment != null)
                    {
                        XLWorkbook wb = new XLWorkbook();
                        wb.Worksheets.Add(EpiasAttachment, "EpiasSentData");
                        //epiasStream = new MemoryStream();
                        wb.SaveAs(epiasStream);
                        epiasStream.Position = 0;
                        var attach = new Attachment(epiasStream, "EpiasSentData.xlsx");
                        //attach.ContentDisposition
                        message.Attachments.Add(attach);
                        
                    }
                    if (Send(message))
                        result = true;
                    epiasStream.Close();
                    epiasStream.Dispose();
                }
            }
            else Helper.log.WriteLogLine("Alıcı yok...!");
            return result;
        }

        public bool Send(MailMessage message)
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            bool result = true;
            try
            {
                message.SubjectEncoding = Encoding.UTF8;
                message.HeadersEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;

                Helper.log.WriteLogLine("Mail sending with subject '" + message.Subject + "'");
                Helper.log.WriteLogLine("       To: " + string.Join("; ", message.To.Select(p => p.Address).ToList()));
                if (message.CC.Count > 0)
                    Helper.log.WriteLogLine("       CC: " + string.Join("; ", message.CC.Select(p => p.Address).ToList()));
                if (message.Bcc.Count > 0)
                    Helper.log.WriteLogLine("      BCC: " + string.Join("; ", message.Bcc.Select(p => p.Address).ToList()));
                Smtp.Send(message);
                Helper.log.WriteLogLine("Mail sent!");
                result = true;
            }
            catch (Exception ex)
            {
                Helper.log.WriteLogLine(ex.ToString(), false);
                result = false;
            }
            return result;
        }
    }

}
