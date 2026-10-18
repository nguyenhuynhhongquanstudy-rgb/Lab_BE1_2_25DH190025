using System.Web;
using System.Web.Mvc;

namespace Lab_BE1_2_25DH190025
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
