using DataTransformer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace DataTransformer.Controllers
{
    public class ContactController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.SessionId = Request["SessionId"];

            return View();
        }

        public ActionResult Posted()
        {
            string userEmail = Request.Form["email"];
            string userMsg = Request.Form["message"];
            string userSessionId = Request.Form["sessionId"];

            string query =  "INSERT INTO UserMsgs(SessionId, UserEmail, UserMsg) " +
                            "VALUES(" + userSessionId + ",'" + userEmail + "','" + userMsg.Replace("'","''") + "'); ";
            string dbConnStr = WebConfigurationManager.ConnectionStrings["DataTransfDB"].ConnectionString;
            DatabaseManager custDbMgr = new DatabaseManager(dbConnStr);
            string dbErrorMsg = "";
            try
            {
                custDbMgr.UpdateRecords(query);
            }
            catch(Exception ex)
            {
                dbErrorMsg = "Exception saving user message to DB:" + ex.Message + " Query:" + query;
            }

            MailMessage mailMsg = new MailMessage();
            mailMsg.To.Add("rogerccopa@gmail.com");
            mailMsg.From = new MailAddress("rogerccopa@gmail.com");
            mailMsg.Subject = "Data Transformer - Visitor Contact";
            mailMsg.Body = "Visitor Email: " + userEmail + Environment.NewLine + Environment.NewLine +
                           "Visitor Message: " + userMsg;

            if (dbErrorMsg.Length > 0) { mailMsg.Body += Environment.NewLine + dbErrorMsg; }

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587; // 465;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("rogerccopa@gmail.com", "2395d3161G!");
            smtp.EnableSsl = true;
            smtp.Send(mailMsg);

            ViewBag.SessionId = userSessionId;
            return View();
        }
    }
}
