using System;
using System.Linq;
using System.Web.Mvc;
using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Data;
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
                existingUser.Name = updatedUser.Name;
                existingUser.Department = updatedUser.Department;
                existingUser.Location = updatedUser.Location;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

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
                _context.Users.Remove(user);
                _context.SaveChanges();

                FormsAuthentication.SignOut();
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("UserSettings");
        }

        /*
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
                    existingCompany.Name = updatedCompany.Name;
                    existingCompany.Address = updatedCompany.Address;
                    existingCompany.ContactEmail = updatedCompany.ContactEmail;
                    existingCompany.ContactPhone = updatedCompany.ContactPhone;
                    existingCompany.TaxNumber = updatedCompany.TaxNumber;

                    _context.SaveChanges();
                    ViewBag.SuccessMessage = "Şirket bilgileri başarıyla güncellendi.";
                }

                return View(existingCompany);
            }

            return RedirectToAction("Index", "Home");
        }
        */

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