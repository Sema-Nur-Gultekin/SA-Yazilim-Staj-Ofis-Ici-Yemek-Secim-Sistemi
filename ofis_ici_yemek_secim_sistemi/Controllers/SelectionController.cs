using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class SelectionController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

    
        public ActionResult SelectionManagement(DateTime? dateFrom = null, DateTime? dateTo = null, string mealType = "", string search = "")
        {
            int companyId = GetCurrentUserCompanyId();

            var menuItemsQuery = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive);

            if (dateFrom.HasValue)
                menuItemsQuery = menuItemsQuery.Where(m => m.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                menuItemsQuery = menuItemsQuery.Where(m => m.Date <= dateTo.Value);
            if (!string.IsNullOrWhiteSpace(mealType))
                menuItemsQuery = menuItemsQuery.Where(m => m.MealType == mealType);

            var menuItems = menuItemsQuery.ToList();
            var menuItemIds = menuItems.Select(m => m.ID).ToList();

            var selections = _context.Selections
                .Where(s => s.CompanyID == companyId && menuItemIds.Contains(s.MenuItemID))
                .ToList();

            var userIds = selections.Select(s => s.UserID).Distinct().ToList();
            var userNames = _context.Users
                .Where(u => userIds.Contains(u.ID))
                .ToDictionary(u => u.ID, u => u.Name);

            var foodIds = menuItems.Select(m => m.FoodID).Distinct().ToList();
            var foodNamesDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID))
                .ToDictionary(f => f.ID, f => f.Name);

            var menuDict = menuItems.ToDictionary(
                m => m.ID,
                m => (Date: m.Date, MealType: m.MealType, FoodID: m.FoodID)
            );

            ViewBag.UserNames = userNames;
            ViewBag.FoodNames = foodNamesDict;
            ViewBag.MenuDict = menuDict;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                selections = selections.Where(s =>
                {
                    string userName = userNames.ContainsKey(s.UserID) ? userNames[s.UserID].ToLower() : "";
                    string foodName = menuDict.ContainsKey(s.MenuItemID) && foodNamesDict.ContainsKey(menuDict[s.MenuItemID].FoodID)
                                      ? foodNamesDict[menuDict[s.MenuItemID].FoodID].ToLower() : "";
                    return userName.Contains(term) || foodName.Contains(term);
                }).ToList();
            }

            ViewBag.MealTypes = _context.MealTypes
                .Where(m => m.CompanyID == companyId && m.IsActive)
                .OrderBy(m => m.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(m => m.DisplayOrder)
                .ToList();

            DateTime nonSelectorsFrom = dateFrom ?? DateTime.Today;
            DateTime nonSelectorsTo = dateTo ?? nonSelectorsFrom;

            
            bool hasMealTypeFilter = !string.IsNullOrWhiteSpace(mealType);

            var rangeMenuItemIds = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive
                            && m.Date >= nonSelectorsFrom && m.Date <= nonSelectorsTo
                            && (!hasMealTypeFilter || m.MealType == mealType))
                .Select(m => m.ID)
                .ToList();

            bool hasMenuInRange = rangeMenuItemIds.Any();

            List<User> nonSelectors = new List<User>();
            if (hasMenuInRange)
            {
                var selectedUserIds = _context.Selections
                    .Where(s => s.CompanyID == companyId && rangeMenuItemIds.Contains(s.MenuItemID))
                    .Select(s => s.UserID)
                    .Distinct()
                    .ToList();

                nonSelectors = _context.Users
                    .Where(u => u.CompanyID == companyId && u.Role == "User" && u.IsActive && !selectedUserIds.Contains(u.ID))
                    .OrderBy(u => u.Name)
                    .ToList();
            }

            ViewBag.NonSelectors = nonSelectors;
            ViewBag.NonSelectorsFrom = nonSelectorsFrom;
            ViewBag.NonSelectorsTo = nonSelectorsTo;
            ViewBag.HasMenuInRange = hasMenuInRange;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SelectionList", selections);
            }

            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.MealType = mealType;
            ViewBag.Search = search;

            return View(selections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSelectionGroup(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return Json(new { success = false, message = "Geçersiz istek." });

            var idList = ids.Split(',')
                .Select(id => int.TryParse(id, out int parsed) ? parsed : 0)
                .Where(id => id > 0)
                .ToList();

            if (!idList.Any())
                return Json(new { success = false, message = "Silinecek seçim bulunamadı." });

            int companyId = GetCurrentUserCompanyId();
            var selectionsToDelete = _context.Selections
                .Where(s => s.CompanyID == companyId && idList.Contains(s.ID))
                .ToList();

            if (!selectionsToDelete.Any())
                return Json(new { success = false, message = "Seçimler bulunamadı." });

            _context.Selections.RemoveRange(selectionsToDelete);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), $"Personel Seçimleri Silindi ({selectionsToDelete.Count} kayıt)");

            return Json(new { success = true, message = $"{selectionsToDelete.Count} seçim başarıyla silindi." });
        }
    }
}
