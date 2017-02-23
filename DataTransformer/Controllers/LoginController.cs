using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DataTransformer.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.SessionId = Request["SessionId"];

            return View();
        }

    }
}
