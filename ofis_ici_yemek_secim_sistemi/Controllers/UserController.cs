using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class UserController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

   
        public ActionResult UserManagement(string search = "")
        {
            int companyId = GetCurrentUserCompanyId();

            var users = _context.Users
                .Where(u => u.CompanyID == companyId);

      
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                users = users.Where(u =>
                    u.Name.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    (u.Department != null && u.Department.ToLower().Contains(term)) ||
                    (u.Location != null && u.Location.ToLower().Contains(term))
                );
            }

            var orderedUsers = users.OrderBy(u => u.Name).ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_UserList", orderedUsers);
            }

            ViewBag.Search = search;
            return View(orderedUsers);
        }

        [HttpGet]
        public ActionResult AddUser()
        {
            return PartialView("_AddUserModal", new User());
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUser(User model, string password)
        {
            int companyId = GetCurrentUserCompanyId();

            if (string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(model.Role))
            {
                return Json(new { success = false, message = "Lütfen ad, e-posta, şifre ve rol alanlarını doldurun." });
            }


            try
            {
                var _ = new System.Net.Mail.MailAddress(model.Email.Trim());
            }
            catch (FormatException)
            {
                return Json(new { success = false, message = "Geçerli bir e-posta adresi giriniz." });
            }


            if (password.Length < 6)
            {
                return Json(new { success = false, message = "Şifre en az 6 karakter olmalıdır." });
            }

            if (_context.Users.Any(u => u.Email == model.Email.Trim()))
            {
                return Json(new { success = false, message = "Bu e-posta adresi zaten kayıtlı." });
            }

            var newUser = new User
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = model.Role,
                CompanyID = companyId,
                Department = model.Department?.Trim(),
                Location = model.Location?.Trim(),
                IsActive = true
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();


            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Kullanıcı Eklendi", newUser.ID);

            return Json(new { success = true, message = $"{newUser.Name} adlı kullanıcı başarıyla eklendi." });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUserStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            string currentUserEmail = User.Identity.Name;

            var targetUser = _context.Users.FirstOrDefault(u => u.ID == id && u.CompanyID == companyId);
            if (targetUser == null)
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            if (targetUser.Email == currentUserEmail)
                return Json(new { success = false, message = "Kendi hesabınızın durumunu buradan değiştiremezsiniz." });

            targetUser.IsActive = !targetUser.IsActive;
            string status = targetUser.IsActive ? "aktif" : "pasif";
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Kullanıcı {status} yapıldı", targetUser.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Kullanıcı {status} hale getirildi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            string currentUserEmail = User.Identity.Name;

            var targetUser = _context.Users
                .FirstOrDefault(u => u.ID == id && u.CompanyID == companyId);

            if (targetUser == null)
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            if (targetUser.Email == currentUserEmail)
                return Json(new { success = false, message = "Kendi hesabınızı silemezsiniz!" });

            targetUser.IsActive = false;
            string deletedName = targetUser.Name;
            int deletedId = targetUser.ID;
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Kullanıcı Silindi (Pasife Alındı)", deletedId);
            _context.SaveChanges();

            return Json(new { success = true, message = $"{deletedName} adlı kullanıcı başarıyla silindi (pasife alındı)." });
        }
    }
}