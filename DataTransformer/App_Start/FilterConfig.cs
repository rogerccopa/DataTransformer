using System.Web;
using System.Web.Mvc;

namespace DataTransformer
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // add HandleErrorAttribute globally.
            // It will apply to all controllers and actions (will go to ~/Views/Shared/Error.cshtml on any error/exception)
            filters.Add(new HandleErrorAttribute());
        }
    }
}