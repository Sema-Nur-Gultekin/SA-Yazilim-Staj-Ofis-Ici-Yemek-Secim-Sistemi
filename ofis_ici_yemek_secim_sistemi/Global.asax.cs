using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security; 
using System.Security.Principal; 

namespace ofis_ici_yemek_secim_sistemi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
           
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                   
                    FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                    if (authTicket != null && !authTicket.Expired)
                    {
                        
                        string[] roles = new string[] { authTicket.UserData };

                      
                        GenericIdentity identity = new GenericIdentity(authTicket.Name);
                        GenericPrincipal principal = new GenericPrincipal(identity, roles);

                   
                        Context.User = principal;
                    }
                }
                catch (Exception)
                {
                    
                }
            }
        }
    }
}