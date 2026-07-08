using System.Web;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
