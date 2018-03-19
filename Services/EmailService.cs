using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using WebApiTokenAuth.Models;

namespace WebApiTokenAuth.Services
{
    public class EmailService
    {
        string _emailSender;
        string _emailReceiver;
        string _emailSubject;
        string _emailMessage;
        List<rmsPlanObj> _listPlans;

        public EmailService(rmsPlanShareObj obj)
        {
            this._emailSubject = obj.emailSubject;
            this._emailSender = obj.emailFrom;
            this._emailReceiver = obj.emailTo;
            this._emailMessage = obj.message;
            this._listPlans = obj.plansArray;
        }
        public string sendPlans()
        {
            string message = "Not performed yet", Receipt = "";
            if (this._listPlans.Count > 0)
            {
                message = sendEmail();
            }
            if(message == "OK")
            {
                if (sendReceiptNotice())
                    Receipt = "OK";
            }
            return Receipt;
        }

        private bool sendReceiptNotice()
        {
            string orderConfirmationID = GenerateRandomID();
            string rootPath = ConfigurationManager.AppSettings["RMS_Plans"];
            string sharedList = "", recipientsList = "";
            double _totalSize = 0;
            int incr = 0;
            if (this._listPlans.Count > 0)
            {
                foreach (rmsPlanObj plan in this._listPlans)
                {
                    incr += 1;
                    string attachmentFilename = $"{rootPath}\\{plan.FileName}.{plan.FileExt}";
                    double _fileSize = 0;
                    if (File.Exists(attachmentFilename))
                    {
                        FileInfo fInfo = new FileInfo(attachmentFilename);
                        _totalSize = _totalSize + ConvertBytesToMegabytes(fInfo.Length);
                        _fileSize = ConvertBytesToMegabytes(fInfo.Length);
                    }

                    // [0] Get the list of files sent 
                    sharedList = sharedList + 
                        $"<tr><td style ='width:80%'><span style ='font-size:1.1em'> {incr}. <b>{plan.FileName}.{plan.FileExt}</b> </ span > </ td >"+
                        $"<td style='text-align:right; width:14%'><span style='text-align:right; font-size:1.2em;'> {_fileSize.ToString("N3")} MB </ span ></ td ></ tr >";
                        
                    // [1] Get the total file size
                               
                }
                if (this._emailReceiver.Trim().IndexOf(',') > -1)
                {
                    string[] emails = this._emailReceiver.Split(',');
                    for (int i = 0; i < emails.Length; i++)
                    {
                        recipientsList = recipientsList + $"<tr><td colspan='2'><span style='font-size:1.2em'>{i+1}. {emails[i]}</span></td></tr>";
                    }
                }
                else
                {
                    recipientsList = recipientsList + $"<tr><td colspan='2'><span style='font-size:1.2em'>{this._emailReceiver.Trim()}</span></td></tr>";
                }
            }

            StreamReader sr = new StreamReader(HttpContext.Current.Request.MapPath("~/Email_Templates/planSentReceipt.txt"));
            string emailBody = sr.ReadToEnd();
            sr.Dispose();

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("support.rms@emht.com");
            mailMessage.To.Add(this._emailSender);

            mailMessage.Subject = "RMS Shared Files Receipt";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = emailBody;

            mailMessage.Body = mailMessage.Body.Replace("<%orderConfirmationID%>", orderConfirmationID);
            mailMessage.Body = mailMessage.Body.Replace("<%fileNameRows%>", sharedList);
            mailMessage.Body = mailMessage.Body.Replace("<%totalFiles%>", $"{_totalSize.ToString("N3")} MB");
            mailMessage.Body = mailMessage.Body.Replace("<%orderDateTime%>", $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToLongTimeString()}");
            mailMessage.Body = mailMessage.Body.Replace("<%emailReceiverRows%>", recipientsList);

            return Send(mailMessage);
        }

        private static bool Send(MailMessage message)
        {
            bool isMessageSent = false;
            SmtpClient smtpClient = new SmtpClient();
            try
            {
                smtpClient.Send(message);
                isMessageSent = true;
            }
            catch (Exception ex)
            {
                isMessageSent = false;
                //throw new Exception("Email not sent", ex.InnerException);
            }

            return isMessageSent;
        }

        private static string GenerateRandomID()
        {
            Random rnd = new Random();
            int orderConfirmationID = rnd.Next(10000000, 99999999);
            return orderConfirmationID.ToString();
        }
        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        private string sendEmail()
        {
            string rootPath = ConfigurationManager.AppSettings["RMS_Plans"];   //HostingEnvironment.MapPath("~/PlansTest");
            string message = null;
            StreamReader sr = new StreamReader(HttpContext.Current.Request.MapPath("~/Email_Templates/planSentHtml.txt"));
            string emailBody = sr.ReadToEnd();
            sr.Dispose();

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(this._emailSender);
            if (this._emailReceiver.Trim().IndexOf(',') > -1)
            {
                string[] emails = this._emailReceiver.Split(',');
                for (int i = 0; i < emails.Length; i++)
                {
                    mailMessage.To.Add(emails[i]);
                }
            }
            else
            {
                mailMessage.To.Add(this._emailReceiver.Trim());
            }

            mailMessage.IsBodyHtml = true;
            //mailMessage.Body = strHTML;

            mailMessage.IsBodyHtml = true;
            mailMessage.Body = emailBody;
            mailMessage.Body = mailMessage.Body.Replace("<%userMessage%>", this._emailMessage);
            mailMessage.Body = mailMessage.Body.Replace("<%emailSender%>", this._emailSender);
            mailMessage.Body = mailMessage.Body.Replace("<%newsubject%>", this._emailSubject);

            mailMessage.Subject = this._emailSubject;
            if (this._listPlans.Count > 0)
            {
                foreach (rmsPlanObj plan in this._listPlans)
                {
                    string attachmentFilename = $"{rootPath}\\{plan.FileName}";
                    if (File.Exists(attachmentFilename))
                    {
                        Attachment attachment = new Attachment(attachmentFilename, MediaTypeNames.Application.Octet);
                        ContentDisposition disposition = attachment.ContentDisposition;
                        disposition.CreationDate = File.GetCreationTime(attachmentFilename);
                        disposition.ModificationDate = File.GetLastWriteTime(attachmentFilename);
                        disposition.ReadDate = File.GetLastAccessTime(attachmentFilename);
                        disposition.FileName = Path.GetFileName(attachmentFilename);
                        disposition.Size = new FileInfo(attachmentFilename).Length;
                        disposition.DispositionType = DispositionTypeNames.Attachment;
                        mailMessage.Attachments.Add(attachment);
                    }
                    else
                        mailMessage.Body = mailMessage.Body + $" <br/> The file <b>{plan.FileName}</b> was not found.";
                }
            }

            SmtpClient smtpClient = new SmtpClient();
            try
            {
                smtpClient.Send(mailMessage);
                message = "OK";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return message;
        }
    }
}