using ofis_ici_yemek_secim_sistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class RatingController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

  
        public ActionResult RatingManagement(DateTime? date = null, string mealType = "", string search = "")
        {
            int companyId = GetCurrentUserCompanyId();

            var ratings = _context.FoodRatings
                .Where(r => r.CompanyID == companyId)
                .OrderByDescending(r => r.RatingDate)
                .ToList();

            var menuItemIds = ratings.Select(r => r.MenuItemID).Distinct().ToList();
            var menuItems = _context.MenuItems
                .Where(m => menuItemIds.Contains(m.ID))
                .ToDictionary(m => m.ID, m => (m.Date, m.MealType, m.FoodID));

            var userIds = ratings.Select(r => r.UserID).Distinct().ToList();
            var userNames = _context.Users
                .Where(u => userIds.Contains(u.ID))
                .ToDictionary(u => u.ID, u => u.Name);

            var foodIds = menuItems.Values.Select(m => m.FoodID).Distinct().ToList();
            var foodNames = _context.Foods
                .Where(f => foodIds.Contains(f.ID))
                .ToDictionary(f => f.ID, f => f.Name);

         
            if (date.HasValue)
            {
                ratings = ratings.Where(r =>
                    menuItems.ContainsKey(r.MenuItemID) &&
                    menuItems[r.MenuItemID].Date == date.Value
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(mealType))
            {
                ratings = ratings.Where(r =>
                    menuItems.ContainsKey(r.MenuItemID) &&
                    menuItems[r.MenuItemID].MealType == mealType
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                ratings = ratings.Where(r =>
                {
                    string userName = userNames.ContainsKey(r.UserID) ? userNames[r.UserID].ToLower() : "";
                    string foodName = menuItems.ContainsKey(r.MenuItemID) && foodNames.ContainsKey(menuItems[r.MenuItemID].FoodID)
                                      ? foodNames[menuItems[r.MenuItemID].FoodID].ToLower() : "";
                    return userName.Contains(term) || foodName.Contains(term);
                }).ToList();
            }

            ViewBag.UserNames = userNames;
            ViewBag.FoodNames = foodNames;
            ViewBag.MenuDict = menuItems;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_RatingList", ratings);
            }

            ViewBag.Date = date;
            ViewBag.MealType = mealType;
            ViewBag.Search = search;

            return View(ratings);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRating(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var rating = _context.FoodRatings
                .FirstOrDefault(r => r.ID == id && r.CompanyID == companyId);

            if (rating == null)
                return Json(new { success = false, message = "Değerlendirme bulunamadı." });

            _context.FoodRatings.Remove(rating);
            _context.SaveChanges();

            return Json(new { success = true, message = "Değerlendirme başarıyla silindi." });
        }
    }
}