using System;
using System.Web;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Filters
{
 
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
       
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            
            filterContext.Result = new RedirectResult("/Account/AccessDenied");
        }
    }
}