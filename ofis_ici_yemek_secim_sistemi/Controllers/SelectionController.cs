using ofis_ici_yemek_secim_sistemi.Models;
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

   
        public ActionResult SelectionManagement(DateTime? date = null, string mealType = "", string search = "")
        {
            int companyId = GetCurrentUserCompanyId();

           
            var menuItemsQuery = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive);

            if (date.HasValue)
                menuItemsQuery = menuItemsQuery.Where(m => m.Date == date.Value);
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

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SelectionList", selections);
            }

            ViewBag.Date = date;
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

            return Json(new { success = true, message = $"{selectionsToDelete.Count} seçim başarıyla silindi." });
        }
    }
}