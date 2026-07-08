using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Filters;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        protected readonly AppDbContext _context = new AppDbContext();


        protected int GetCurrentUserCompanyId()
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.CompanyID ?? 0;
        }

        public virtual ActionResult Index()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
