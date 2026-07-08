using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security; 
using ofis_ici_yemek_secim_sistemi.Models; 
using ofis_ici_yemek_secim_sistemi.Data;  
using BCrypt.Net;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context = new AppDbContext();


        [HttpGet]
        public ActionResult Login()
        {
    
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Lütfen tüm alanları eksiksiz doldurunuz.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
          
      
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,                          
                    user.Email,                 
                    DateTime.Now,             
                    DateTime.Now.AddMinutes(2880),
                    false,  
                    user.Role  
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

  
                HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true 
                };
                Response.Cookies.Add(authCookie);

           
                Session["FullName"] = user.Name;

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "E-posta adresi veya şifre hatalı!";
            return View();
        }

        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Companies = _context.Companies.Where(c => c.IsActive != false).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string name, string email, string password, string confirmPassword,
                                     string department, string location, string role,
                                     string companyName, int? selectedCompanyId)
        {
            ViewBag.Companies = _context.Companies.Where(c => c.IsActive != false).ToList();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) ||
                string.IsNullOrEmpty(department) || string.IsNullOrEmpty(location) ||
                string.IsNullOrEmpty(role))
            {
                ViewBag.Error = "Lütfen tüm zorunlu alanları eksiksiz doldurunuz.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Girdiğiniz şifreler birbiriyle uyuşmuyor!";
                return View();
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Bu e-posta adresi sistemde zaten kayıtlı!";
                return View();
            }

            int finalCompanyId = 0;

            if (role == "Admin")
            {
                if (string.IsNullOrEmpty(companyName))
                {
                    ViewBag.Error = "Şirket Admini kaydı için Şirket Adı alanı boş bırakılamaz.";
                    return View();
                }

                try
                {
                    var newCompany = new Company
                    {
                        Name = companyName.Trim(),
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.Companies.Add(newCompany);
                    _context.SaveChanges();

                    finalCompanyId = newCompany.ID;
                }
                catch (Exception)
                {
                    ViewBag.Error = "Yeni şirket kaydı oluşturulurken teknik bir hata meydana geldi.";
                    return View();
                }
            }
            else if (role == "User")
            {
                if (!selectedCompanyId.HasValue || selectedCompanyId.Value <= 0)
                {
                    ViewBag.Error = "Standart personel kaydı için lütfen listeden şirketinizi seçiniz.";
                    return View();
                }

                finalCompanyId = selectedCompanyId.Value;
            }
            else
            {
                ViewBag.Error = "Sistem tarafından desteklenmeyen bir yetki rolü seçildi.";
                return View();
            }

            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var newUser = new User
                {
                    Name = name.Trim(),
                    Email = email.Trim(),
                    PasswordHash = hashedPassword,
                    Role = role,
                    CompanyID = finalCompanyId,
                    Department = department.Trim(),
                    Location = location.Trim()
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {
                ViewBag.Error = "Kullanıcı kaydı tamamlanırken veritabanı yazma hatası oluştu.";
                return View();
            }
        }

        public ActionResult Logout()
        {
          
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _context != null)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        [AllowAnonymous] 
        public ActionResult AccessDenied()
        {
            return View();
        }
    }
}