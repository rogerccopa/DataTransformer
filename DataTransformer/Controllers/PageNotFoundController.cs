using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DataTransformer.Controllers
{
    public class PageNotFoundController : Controller
    {
        public ActionResult NotFound()
        {
            // This will return a view with the same name. (/Views/Shared/NotFound)
            return View();
        }
    }
}
