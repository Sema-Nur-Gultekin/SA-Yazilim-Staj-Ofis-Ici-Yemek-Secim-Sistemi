using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    [Authorize(Roles = "User")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context = new AppDbContext();

        public ActionResult Index()
        {
            string email = User.Identity.Name;
            var currentUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            int companyId = currentUser.CompanyID;
            int userId = currentUser.ID;

            DateTime today = DateTime.Now.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = today.AddDays(-diff).Date;
           
            DateTime sunday = monday.AddDays(6);

            var menuItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId
                            && m.IsActive
                            && m.Date >= monday
                            && m.Date <= sunday)
                .OrderBy(m => m.Date)
                .ThenBy(m => m.MealType)
                .ToList();

            var foodIds = menuItems.Select(m => m.FoodID).Distinct().ToList();
            var foodsDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID))
                .ToDictionary(f => f.ID, f => f.Name);
            var foodImagesDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID) && f.ImagePath != null)
                .ToDictionary(f => f.ID, f => f.ImagePath);
         
            var foodAllergensDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID) && f.Allergens != null && f.Allergens != "")
                .ToDictionary(f => f.ID, f => f.Allergens);
            var foodCategoryDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID) && f.CategoryID.HasValue)
                .ToDictionary(f => f.ID, f => f.CategoryID.Value);

            var categoryIds = foodCategoryDict.Values.Distinct().ToList();
            var categoryNames = _context.FoodCategories
                .Where(c => categoryIds.Contains(c.ID))
                .ToDictionary(c => c.ID, c => c.Name);

            var orderedCategories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ToList();


            var mealOrderMap = _context.MealTypes
                .Where(m => m.CompanyID == companyId)
                .ToDictionary(m => m.Name, m => m.DisplayOrder ?? int.MaxValue - 1);
            ViewBag.MealOrderMap = mealOrderMap;

            var menuItemIds = menuItems.Select(m => m.ID).ToList();
            var selectedMenuItemIds = _context.Selections
                .Where(s => s.UserID == userId
                            && s.CompanyID == companyId
                            && menuItemIds.Contains(s.MenuItemID))
                .Select(s => s.MenuItemID)
                .ToHashSet();

            var ratings = _context.FoodRatings
                .Where(r => r.UserID == userId
                            && r.CompanyID == companyId
                            && menuItemIds.Contains(r.MenuItemID))
                .ToList();
            var ratingsDict = ratings.ToDictionary(r => r.MenuItemID, r => r);


            var capacityLimitedIds = menuItems.Where(m => m.PlannedPortion.HasValue).Select(m => m.ID).ToList();
            var selectionCountsDict = capacityLimitedIds.Any()
                ? _context.Selections
                    .Where(s => capacityLimitedIds.Contains(s.MenuItemID))
                    .GroupBy(s => s.MenuItemID)
                    .ToDictionary(g => g.Key, g => g.Count())
                : new Dictionary<int, int>();

            ViewBag.FoodNames = foodsDict;
            ViewBag.FoodImages = foodImagesDict;
            ViewBag.FoodAllergens = foodAllergensDict;
            ViewBag.FoodCategories = foodCategoryDict;
            ViewBag.CategoryNames = categoryNames;
            ViewBag.OrderedCategories = orderedCategories;
            ViewBag.SelectedIds = selectedMenuItemIds;
            ViewBag.Ratings = ratingsDict;
            ViewBag.SelectionCounts = selectionCountsDict;
            ViewBag.Today = today;


            var todayMenuItemIds = menuItems.Where(m => m.Date.Date == today).Select(m => m.ID).ToList();
            bool hasMenuToday = todayMenuItemIds.Any();
            bool hasSelectedToday = todayMenuItemIds.Any(id => selectedMenuItemIds.Contains(id));
            ViewBag.HasMenuToday = hasMenuToday;
            ViewBag.HasSelectedToday = hasSelectedToday;

            return View(menuItems);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SelectMeal(int menuItemId)
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            int companyId = user.CompanyID;
            int userId = user.ID;

            var menuItem = _context.MenuItems.FirstOrDefault(m => m.ID == menuItemId && m.CompanyID == companyId);
            if (menuItem == null) return Json(new { success = false, message = "Menü öğesi bulunamadı." });

            if (menuItem.Date < DateTime.Now.Date)
            {
                return Json(new { success = false, message = "Geçmiş bir tarihe seçim yapamazsınız." });
            }

            var food = _context.Foods.FirstOrDefault(f => f.ID == menuItem.FoodID);
            int? categoryId = food?.CategoryID;

            var existingSelection = _context.Selections
                .FirstOrDefault(s => s.UserID == userId && s.CompanyID == companyId && s.MenuItemID == menuItemId);

            if (existingSelection != null)
            {
              
                _context.Selections.Remove(existingSelection);
                _context.SaveChanges();
                ActivityLogger.LogAndSave(_context, companyId, userId, "Yemek Seçimi İptal Edildi", menuItemId);
                return Json(new { success = true, message = "Seçiminiz iptal edildi.", action = "deselected", menuItemId = menuItemId });
            }


            if (menuItem.PlannedPortion.HasValue)
            {
                int currentSelectionCount = _context.Selections.Count(s => s.MenuItemID == menuItemId);
                if (currentSelectionCount >= menuItem.PlannedPortion.Value)
                {
                    return Json(new { success = false, message = "Bu yemek için ayrılan kontenjan dolmuştur. Lütfen başka bir seçenek belirleyin." });
                }
            }

     
            if (categoryId.HasValue)
            {
                var sameDayMealCategorySelections = _context.Selections
                    .Where(s => s.UserID == userId && s.CompanyID == companyId)
                    .ToList()
                    .Where(s =>
                    {
                        var mi = _context.MenuItems.FirstOrDefault(m => m.ID == s.MenuItemID);
                        if (mi == null) return false;
               
                        if (mi.Date != menuItem.Date) return false;
                        if (mi.MealType != menuItem.MealType) return false;
                        var f = _context.Foods.FirstOrDefault(x => x.ID == mi.FoodID);
                        return f != null && f.CategoryID == categoryId;
                    })
                    .ToList();

                foreach (var sel in sameDayMealCategorySelections)
                {
                    _context.Selections.Remove(sel);
                }
            }

            var newSelection = new Selection
            {
                CompanyID = companyId,
                UserID = userId,
                MenuItemID = menuItemId,
                SelectionDate = DateTime.Now,
                Note = null
            };
            _context.Selections.Add(newSelection);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, userId, "Yemek Seçimi Yapıldı", menuItemId);

            return Json(new { success = true, message = "Seçiminiz kaydedildi.", action = "selected", menuItemId = menuItemId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RateMealDetail(int menuItemId, int tasteRating, int presentationRating, int satietyRating, string comment)
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            int companyId = user.CompanyID;
            int userId = user.ID;

            var menuItem = _context.MenuItems.FirstOrDefault(m => m.ID == menuItemId && m.CompanyID == companyId);
            if (menuItem == null) return Json(new { success = false, message = "Menü öğesi bulunamadı." });


            if (menuItem.Date >= DateTime.Now.Date)
            {
                return Json(new { success = false, message = "Bir yemeği değerlendirebilmeniz için o günün tamamlanmış olması (en az 1 gün geçmesi) gerekir." });
            }

            int overallRating = (int)Math.Round((tasteRating + presentationRating + satietyRating) / 3.0);

            var existingRating = _context.FoodRatings
                .FirstOrDefault(r => r.UserID == userId && r.CompanyID == companyId && r.MenuItemID == menuItemId);

       
            string trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

            if (existingRating != null)
            {
                existingRating.OverallRating = overallRating;
                existingRating.TasteRating = tasteRating;
                existingRating.PresentationRating = presentationRating;
                existingRating.SatietyRating = satietyRating;
                existingRating.Comment = trimmedComment;
                existingRating.RatingDate = DateTime.Now;
            }
            else
            {
                var rating = new FoodRating
                {
                    CompanyID = companyId,
                    UserID = userId,
                    MenuItemID = menuItemId,
                    FoodID = menuItem.FoodID,
                    OverallRating = overallRating,
                    TasteRating = tasteRating,
                    PresentationRating = presentationRating,
                    SatietyRating = satietyRating,
                    Comment = trimmedComment,
                    RatingDate = DateTime.Now
                };
                _context.FoodRatings.Add(rating);
            }

            _context.SaveChanges();
            ActivityLogger.LogAndSave(_context, companyId, userId, "Yemek Değerlendirildi", menuItemId);
            return Json(new { success = true, message = "Değerlendirmeniz kaydedildi.", overallRating = overallRating });
        }


        [HttpPost]
        public JsonResult GetRating(int menuItemId)
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { success = false });

            int companyId = user.CompanyID;
            int userId = user.ID;

            var rating = _context.FoodRatings
                .FirstOrDefault(r => r.UserID == userId && r.CompanyID == companyId && r.MenuItemID == menuItemId);

            if (rating != null)
            {
                return Json(new
                {
                    success = true,
                    tasteRating = rating.TasteRating ?? 0,
                    presentationRating = rating.PresentationRating ?? 0,
                    satietyRating = rating.SatietyRating ?? 0,
                    comment = rating.Comment ?? ""
                });
            }

            return Json(new { success = true, tasteRating = 0, presentationRating = 0, satietyRating = 0, comment = "" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
