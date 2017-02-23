using DataTransformer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace DataTransformer
{
    public class MyCustomGlobalErrHandler : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
            {
                return;
            }

            // 500: Server internal error. Means our code has thrown an error.
            if (new HttpException(null, filterContext.Exception).GetHttpCode() != 500) { return; }

            // if the request is AJAX return JSON else View.
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new { error = true, message = filterContext.Exception.Message }
                };
            }
            else
            {
                var controllerName = (string)filterContext.RouteData.Values["controller"];
                var actionName = (string)filterContext.RouteData.Values["action"];
                var model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);

                filterContext.Result = new ViewResult
                {
                    ViewName = View,
                    MasterName = Master,
                    ViewData = new ViewDataDictionary(model),
                    TempData = filterContext.Controller.TempData
                };
            }

            // log the error by using your own method
            LogError(filterContext.Exception.Message, filterContext.Exception, filterContext.Controller.TempData);

            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = 500;

            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }

        private void LogError(string errorMessage, Exception exception, TempDataDictionary tempData)
        {
            int sessionId = tempData["SessionId"] != null ? int.Parse(tempData["SessionId"].ToString()) : 0;

            string query = "INSERT INTO ErrorLogs(SessionId, ErrorMsg) " +
                            "VALUES(" + sessionId + ",'" + errorMessage.Replace("'", "''") + " " + exception.StackTrace.Replace("'", "''") + "')";
            DatabaseManager custDbMgr = new DatabaseManager(WebConfigurationManager.ConnectionStrings["DataTransfDB"].ConnectionString);
            custDbMgr.UpdateRecords(query);
        }
    }
}