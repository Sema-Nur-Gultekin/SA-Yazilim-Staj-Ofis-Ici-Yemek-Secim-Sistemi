using System;
using System.Linq;
using System.Web.Mvc;
using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Services;
using System.Web.Security;
using System.IO;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context = new AppDbContext();

        [HttpGet]
        public ActionResult UserSettings()
        {
            string currentUserEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserSettings(User updatedUser, string newPassword)
        {
            string currentUserEmail = User.Identity.Name;
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            if (existingUser != null)
            {
                if (string.IsNullOrWhiteSpace(updatedUser.Name))
                {
                    ViewBag.ErrorMessage = "Ad Soyad boş bırakılamaz.";
                    return View(existingUser);
                }

                if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
                {
                    ViewBag.ErrorMessage = "Yeni şifreniz en az 6 karakter olmalıdır.";
                    return View(existingUser);
                }

                
                existingUser.Name = updatedUser.Name.Trim();
                existingUser.Department = string.IsNullOrWhiteSpace(updatedUser.Department) ? null : updatedUser.Department.Trim();
                existingUser.Location = string.IsNullOrWhiteSpace(updatedUser.Location) ? null : updatedUser.Location.Trim();

                if (!string.IsNullOrEmpty(newPassword))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                ActivityLogger.Log(_context, existingUser.CompanyID, existingUser.ID, "Profil Bilgileri Güncellendi", existingUser.ID);
                _context.SaveChanges();

                Session["FullName"] = existingUser.Name;

                ViewBag.SuccessMessage = "Kişisel bilgileriniz başarıyla güncellendi.";
            }

            return View(existingUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public ActionResult DeleteUserAccount()
        {
            string currentUserEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            if (user != null)
            {
                
                user.IsActive = false;
                ActivityLogger.Log(_context, user.CompanyID, user.ID, "Kullanıcı Kendi Hesabını Sildi (Pasife Alındı)", user.ID);
                _context.SaveChanges();

                FormsAuthentication.SignOut();
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("UserSettings");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult CompanySettings()
        {
            string currentUserEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            if (user != null)
            {
                var company = _context.Companies.FirstOrDefault(c => c.ID == user.CompanyID);
                return View(company);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CompanySettings(Company updatedCompany)
        {
            string currentUserEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            if (user != null)
            {
                var existingCompany = _context.Companies.FirstOrDefault(c => c.ID == user.CompanyID);

                if (existingCompany != null)
                {
                    if (string.IsNullOrWhiteSpace(updatedCompany.Name))
                    {
                        ViewBag.ErrorMessage = "Şirket unvanı boş bırakılamaz.";
                        return View(existingCompany);
                    }

                    
                    existingCompany.Name = updatedCompany.Name.Trim();
                    existingCompany.TaxNumber = string.IsNullOrWhiteSpace(updatedCompany.TaxNumber) ? null : updatedCompany.TaxNumber.Trim();
                    existingCompany.ContactEmail = string.IsNullOrWhiteSpace(updatedCompany.ContactEmail) ? null : updatedCompany.ContactEmail.Trim();
                    existingCompany.ContactPhone = string.IsNullOrWhiteSpace(updatedCompany.ContactPhone) ? null : updatedCompany.ContactPhone.Trim();
                    existingCompany.Address = string.IsNullOrWhiteSpace(updatedCompany.Address) ? null : updatedCompany.Address.Trim();

                   
                    ActivityLogger.Log(_context, existingCompany.ID, user.ID, "Şirket Ayarları Güncellendi", existingCompany.ID);

                    _context.SaveChanges();
                    ViewBag.SuccessMessage = "Şirket bilgileri başarıyla güncellendi.";
                }

                return View(existingCompany);
            }

            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _context != null)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
