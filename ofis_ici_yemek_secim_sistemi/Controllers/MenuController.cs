using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class MenuController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;


        private List<MealType> GetActiveMealTypes(int companyId)
        {
            return _context.MealTypes
                .Where(m => m.CompanyID == companyId && m.IsActive)
                .OrderBy(m => m.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(m => m.DisplayOrder)
                .ToList();
        }


        public ActionResult WeeklyMenu(DateTime? weekOf = null)
        {
            int companyId = GetCurrentUserCompanyId();

            DateTime refDate = (weekOf ?? DateTime.Today).Date;
            int diff = (7 + (refDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = refDate.AddDays(-diff);
   
            DateTime sunday = monday.AddDays(6);

            var menuItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date >= monday && m.Date <= sunday)
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

            var mealOrderMap = _context.MealTypes
                .Where(m => m.CompanyID == companyId)
                .ToDictionary(m => m.Name, m => m.DisplayOrder ?? int.MaxValue - 1);

            ViewBag.FoodNames = foodsDict;
            ViewBag.FoodImages = foodImagesDict;
            ViewBag.MealOrderMap = mealOrderMap;
            ViewBag.WeekStart = monday;
            ViewBag.WeekEnd = sunday;
            ViewBag.PrevWeek = monday.AddDays(-7).ToString("yyyy-MM-dd");
            ViewBag.NextWeek = monday.AddDays(7).ToString("yyyy-MM-dd");
            ViewBag.Today = DateTime.Today;

            return View(menuItems);
        }

        public ActionResult MenuManagement(DateTime? dateFrom = null, DateTime? dateTo = null, string mealType = "", string search = "")
        {
            int companyId = GetCurrentUserCompanyId();

            var menus = _context.MenuItems
                .Where(m => m.CompanyID == companyId);

        
            if (dateFrom.HasValue)
            {
                menus = menus.Where(m => m.Date >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                menus = menus.Where(m => m.Date <= dateTo.Value);
            }

          
            if (!string.IsNullOrWhiteSpace(mealType))
            {
                menus = menus.Where(m => m.MealType == mealType);
            }

            var menuList = menus.OrderByDescending(m => m.Date).ToList();

          
            var foodIds = menuList.Select(m => m.FoodID).Distinct().ToList();
            var relatedFoods = _context.Foods
                .Where(f => foodIds.Contains(f.ID))
                .ToList();

            var categoryIds = relatedFoods
                .Where(f => f.CategoryID.HasValue)
                .Select(f => f.CategoryID.Value)
                .Distinct()
                .ToList();
            var categoryNames = _context.FoodCategories
                .Where(c => categoryIds.Contains(c.ID))
                .ToDictionary(c => c.ID, c => c.Name);

            var foodNames = relatedFoods.ToDictionary(f => f.ID, f => f.Name);
            var foodCategoryNames = relatedFoods.ToDictionary(
                f => f.ID,
                f => (f.CategoryID.HasValue && categoryNames.ContainsKey(f.CategoryID.Value))
                    ? categoryNames[f.CategoryID.Value]
                    : "—");

         
            ViewBag.FoodNames = foodNames;
            ViewBag.FoodCategoryNames = foodCategoryNames;

            var mealOrderMap = _context.MealTypes
                .Where(m => m.CompanyID == companyId)
                .ToDictionary(m => m.Name, m => m.DisplayOrder ?? int.MaxValue - 1);

           
            var groupedMenus = menuList
                .GroupBy(m => new { m.Date, m.MealType })
                .OrderByDescending(g => g.Key.Date)
                .ThenBy(g => mealOrderMap.ContainsKey(g.Key.MealType) ? mealOrderMap[g.Key.MealType] : int.MaxValue)
                .Select(g => new MenuGroupViewModel
                {
                    Date = g.Key.Date,
                    MealType = g.Key.MealType,
                    Items = g.Select(item =>
                    {
                        var foodName = foodNames.ContainsKey(item.FoodID) ? foodNames[item.FoodID] : "?";
                        var categoryName = foodCategoryNames.ContainsKey(item.FoodID) ? foodCategoryNames[item.FoodID] : "—";
                        var hasSelection = _context.Selections.Any(s => s.MenuItemID == item.ID);
                        return new MenuItemViewModel
                        {
                            ID = item.ID,
                            FoodID = item.FoodID,
                            FoodName = foodName,
                            CategoryName = categoryName,
                            IsActive = item.IsActive,
                            HasAnySelection = hasSelection
                        };
                    }).ToList()
                }).ToList();

           
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                groupedMenus = groupedMenus.Where(g =>
                    g.Items.Any(i => i.FoodName.ToLower().Contains(term) || i.CategoryName.ToLower().Contains(term))
                ).ToList();
            }

            ViewBag.GroupedMenus = groupedMenus;
            ViewBag.MealTypes = GetActiveMealTypes(companyId);
            ViewBag.HasFoodsGlobal = _context.Foods.Any(f => f.CompanyID == companyId && f.IsActive);
            ViewBag.HasMealTypes = ViewBag.MealTypes is List<MealType> mt && mt.Any();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_MenuList", groupedMenus);
            }

            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.MealType = mealType;
            ViewBag.Search = search;

            return View(groupedMenus);
        }

    
        private void LoadMenuFormLookups(int companyId)
        {
           
            var activeCategories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ToList();

            ViewBag.Categories = activeCategories;

            var activeCategoryIds = activeCategories.Select(c => c.ID).ToList();

   
            var foods = _context.Foods
                .Where(f => f.CompanyID == companyId
                            && f.IsActive
                            && (!f.CategoryID.HasValue || activeCategoryIds.Contains(f.CategoryID.Value)))
                .OrderBy(f => f.Name)
                .ToList();

            ViewBag.Foods = foods;
            ViewBag.HasFoods = foods.Any();

           
            var mealTypes = GetActiveMealTypes(companyId);
            ViewBag.MealTypes = mealTypes;
            ViewBag.HasMealTypes = mealTypes.Any();
        }

        [HttpGet]
        public ActionResult AddMenu()
        {
            int companyId = GetCurrentUserCompanyId();
            LoadMenuFormLookups(companyId);
            return PartialView("_AddMenuModal");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMenu(DateTime Date, string MealType, int[] FoodIDs, int?[] PlannedPortions)
        {
            int companyId = GetCurrentUserCompanyId();

            if (FoodIDs == null || FoodIDs.Length == 0)
            {
                return Json(new { success = false, message = "En az bir yemek seçmelisiniz." });
            }

           
            if (PlannedPortions != null && PlannedPortions.Any(p => p.HasValue && p.Value <= 0))
            {
                return Json(new { success = false, message = "Kontenjan (planlanan porsiyon) sıfırdan büyük bir sayı olmalıdır." });
            }

            
            var existingFoodIds = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == Date && m.MealType == MealType)
                .Select(m => m.FoodID)
                .ToList();

            var duplicateFoodIds = FoodIDs.Where(fid => existingFoodIds.Contains(fid)).ToList();
            if (duplicateFoodIds.Any())
            {
                var duplicateNames = _context.Foods
                    .Where(f => duplicateFoodIds.Contains(f.ID))
                    .Select(f => f.Name)
                    .ToList();
                return Json(new { success = false, message = "Şu yemekler zaten bu öğünde mevcut: " + string.Join(", ", duplicateNames) });
            }

            for (int i = 0; i < FoodIDs.Length; i++)
            {
             
                int? plannedPortion = (PlannedPortions != null && i < PlannedPortions.Length) ? PlannedPortions[i] : null;

                var menuItem = new MenuItem
                {
                    CompanyID = companyId,
                    Date = Date,
                    MealType = MealType,
                    FoodID = FoodIDs[i],
                    PlannedPortion = plannedPortion,
                    IsActive = true
                };
                _context.MenuItems.Add(menuItem);
            }

            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), $"Menü Eklendi ({Date:yyyy-MM-dd} {MealType})");

            return Json(new { success = true, message = "Menü başarıyla eklendi." });
        }

  
        [HttpGet]
        public ActionResult EditMenu(DateTime date, string mealType)
        {
            int companyId = GetCurrentUserCompanyId();

           
            if (date.Date < DateTime.Today)
            {
                return Content("<div class='alert alert-warning'>Geçmiş tarihli menüler düzenlenemez.</div>");
            }

            var existingItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == date && m.MealType == mealType)
                .ToList();

            if (!existingItems.Any())
                return HttpNotFound();

            LoadMenuFormLookups(companyId);

           
            var menuItemIds = existingItems.Select(mi => mi.ID).ToList();
            var selectedMenuItemIds = _context.Selections
                .Where(s => menuItemIds.Contains(s.MenuItemID))
                .Select(s => s.MenuItemID)
                .Distinct()
                .ToList();

            var lockedFoodIds = existingItems
                .Where(mi => selectedMenuItemIds.Contains(mi.ID))
                .Select(mi => mi.FoodID)
                .Distinct()
                .ToList();

            var model = new EditMenuViewModel
            {
                Date = date,
                MealType = mealType,
                ExistingItems = existingItems.Select(mi =>
                {
                    var food = _context.Foods.FirstOrDefault(f => f.ID == mi.FoodID);
                    var cat = food != null && food.CategoryID.HasValue
                        ? _context.FoodCategories.FirstOrDefault(c => c.ID == food.CategoryID.Value)
                        : null;
                    var foodName = food != null ? food.Name : "?";
                    var catName = cat != null ? cat.Name : "—";
                    return new MenuItemViewModel
                    {
                        ID = mi.ID,
                        FoodID = mi.FoodID,
                        FoodName = foodName,
                        CategoryName = catName,
                        IsActive = mi.IsActive,
                        HasAnySelection = selectedMenuItemIds.Contains(mi.ID),
                        IsLocked = lockedFoodIds.Contains(mi.FoodID) && selectedMenuItemIds.Contains(mi.ID),
                        PlannedPortion = mi.PlannedPortion
                    };
                }).ToList(),
                LockedFoodIds = lockedFoodIds
            };

            return PartialView("_EditMenuModal", model);
        }

     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMenu(DateTime Date, string MealType, int[] FoodIDs, int?[] PlannedPortions)
        {
            int companyId = GetCurrentUserCompanyId();

          
            if (Date.Date < DateTime.Today)
            {
                return Json(new { success = false, message = "Geçmiş tarihli menüler düzenlenemez." });
            }

            
            if (PlannedPortions != null && PlannedPortions.Any(p => p.HasValue && p.Value <= 0))
            {
                return Json(new { success = false, message = "Kontenjan (planlanan porsiyon) sıfırdan büyük bir sayı olmalıdır." });
            }

           
            var existingItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == Date && m.MealType == MealType)
                .ToList();

            var existingIds = existingItems.Select(mi => mi.ID).ToList();
            var selectedMenuItemIds = _context.Selections
                .Where(s => existingIds.Contains(s.MenuItemID))
                .Select(s => s.MenuItemID)
                .Distinct()
                .ToList();

            var lockedItems = existingItems.Where(mi => selectedMenuItemIds.Contains(mi.ID)).ToList();
            var lockedFoodIds = lockedItems.Select(mi => mi.FoodID).Distinct().ToList();

            if (lockedFoodIds.Any())
            {
                var missingLocked = lockedFoodIds.Except(FoodIDs).ToList();
                if (missingLocked.Any())
                {
                    var missingNames = _context.Foods.Where(f => missingLocked.Contains(f.ID)).Select(f => f.Name).ToList();
                    return Json(new { success = false, message = "Seçim yapılmış şu yemekler kaldırılamaz: " + string.Join(", ", missingNames) });
                }
            }

           
            var portionMap = new System.Collections.Generic.Dictionary<int, int?>();
            for (int i = 0; i < FoodIDs.Length; i++)
            {
                int? portion = (PlannedPortions != null && i < PlannedPortions.Length) ? PlannedPortions[i] : null;
                portionMap[FoodIDs[i]] = portion;
            }

            var toRemove = existingItems
                .Where(mi => !FoodIDs.Contains(mi.FoodID) && !lockedFoodIds.Contains(mi.FoodID))
                .ToList();

            foreach (var item in toRemove)
            {
                _context.MenuItems.Remove(item);
            }

           
            foreach (var item in existingItems.Except(toRemove))
            {
                if (portionMap.TryGetValue(item.FoodID, out int? updatedPortion))
                {
                    item.PlannedPortion = updatedPortion;
                }
            }

            var existingFoodIds = existingItems.Select(mi => mi.FoodID).ToList();
            var toAdd = FoodIDs.Where(fid => !existingFoodIds.Contains(fid)).ToList();

            foreach (var foodId in toAdd)
            {
                var menuItem = new MenuItem
                {
                    CompanyID = companyId,
                    Date = Date,
                    MealType = MealType,
                    FoodID = foodId,
                    PlannedPortion = portionMap.TryGetValue(foodId, out int? newPortion) ? newPortion : null,
                    IsActive = true
                };
                _context.MenuItems.Add(menuItem);
            }

            _context.SaveChanges();
            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), $"Menü Düzenlendi ({Date:yyyy-MM-dd} {MealType})");
            return Json(new { success = true, message = "Menü başarıyla güncellendi." });
        }

  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleMenuStatus(DateTime date, string mealType)
        {
            int companyId = GetCurrentUserCompanyId();

            var items = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == date && m.MealType == mealType)
                .ToList();

            if (!items.Any())
                return Json(new { success = false, message = "Menü bulunamadı." });

            if (date >= DateTime.Today)
            {
                var itemIds = items.Select(i => i.ID).ToList();
                bool hasSelection = _context.Selections.Any(s => itemIds.Contains(s.MenuItemID));

                if (hasSelection)
                {
                    return Json(new { success = false, message = "Bu menüde kullanıcı seçimi bulunduğu için durum değiştirilemez." });
                }
            }

            bool newStatus = !items.First().IsActive;

            foreach (var item in items)
            {
                item.IsActive = newStatus;
            }

            _context.SaveChanges();

            string status = newStatus ? "aktif" : "pasif";
            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), $"Menü {status} yapıldı ({date:yyyy-MM-dd} {mealType})");
            return Json(new { success = true, message = $"Menü {status} hale getirildi." });
        }

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMenu(DateTime date, string mealType)
        {
            int companyId = GetCurrentUserCompanyId();
            var items = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == date && m.MealType == mealType)
                .ToList();

            if (!items.Any())
                return Json(new { success = false, message = "Menü bulunamadı." });

            if (date >= DateTime.Today)
            {
                var itemIds = items.Select(i => i.ID).ToList();
                bool hasSelection = _context.Selections.Any(s => itemIds.Contains(s.MenuItemID));
                if (hasSelection)
                {
                    return Json(new { success = false, message = "Bu menüde kullanıcı seçimleri bulunduğu için silinemez." });
                }
            }

            _context.MenuItems.RemoveRange(items);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), $"Menü Silindi ({date:yyyy-MM-dd} {mealType})");

            return Json(new { success = true, message = "Menü başarıyla silindi." });
        }


        public class MenuGroupViewModel
        {
            public DateTime Date { get; set; }
            public string MealType { get; set; }
            public List<MenuItemViewModel> Items { get; set; }
        }

        public class MenuItemViewModel
        {
            public int ID { get; set; }
            public int FoodID { get; set; }
            public string FoodName { get; set; }
            public string CategoryName { get; set; }
            public bool IsActive { get; set; }
            public bool HasAnySelection { get; set; }
            public bool IsLocked { get; set; }
            
            public int? PlannedPortion { get; set; }
        }

        public class EditMenuViewModel
        {
            public DateTime Date { get; set; }
            public string MealType { get; set; }
            public List<MenuItemViewModel> ExistingItems { get; set; }
            public List<int> LockedFoodIds { get; set; }
        }
    }
}
