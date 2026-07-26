using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Filters;
using ofis_ici_yemek_secim_sistemi.Models;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context = new AppDbContext();

     
        public ActionResult DailyReport(string date = null, string mealType = "", int? categoryId = null)
        {
           
            DateTime reportDate;
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reportDate))
            {
                reportDate = DateTime.Today;
            }

            string email = User.Identity.Name;
            var currentUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            int companyId = currentUser.CompanyID;

           
            var menuItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId
                            && m.Date == reportDate
                            && m.IsActive);

            if (!string.IsNullOrWhiteSpace(mealType))
            {
                menuItems = menuItems.Where(m => m.MealType == mealType);
            }

            var menuList = menuItems.ToList();
            var menuItemIds = menuList.Select(m => m.ID).ToList();

        
            var selectionCounts = _context.Selections
                .Where(s => s.CompanyID == companyId && menuItemIds.Contains(s.MenuItemID))
                .GroupBy(s => s.MenuItemID)
                .Select(g => new { MenuItemId = g.Key, Count = g.Count() })
                .ToList();

            
            var foodIds = menuList.Select(m => m.FoodID).Distinct().ToList();
            var foodsDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID))
                .ToDictionary(f => f.ID, f => f.Name);

            var foodCategoryDict = _context.Foods
                .Where(f => foodIds.Contains(f.ID) && f.CategoryID.HasValue)
                .ToDictionary(f => f.ID, f => f.CategoryID.Value);

            var categoryIds = foodCategoryDict.Values.Distinct().ToList();
            var categoryNames = _context.FoodCategories
                .Where(c => categoryIds.Contains(c.ID))
                .ToDictionary(c => c.ID, c => c.Name);

          
            var reportData = new List<ReportRow>();
            foreach (var item in menuList)
            {
                int count = selectionCounts
                    .Where(sc => sc.MenuItemId == item.ID)
                    .Select(sc => sc.Count)
                    .FirstOrDefault();

                if (count == 0) continue;

                string foodName = foodsDict.ContainsKey(item.FoodID) ? foodsDict[item.FoodID] : "Bilinmeyen";
                string categoryName = "—";
                int catId = 0;
                if (foodCategoryDict.ContainsKey(item.FoodID))
                {
                    catId = foodCategoryDict[item.FoodID];
                    if (categoryNames.ContainsKey(catId)) categoryName = categoryNames[catId];
                }

                
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    if (catId != categoryId.Value) continue;
                }

                reportData.Add(new ReportRow
                {
                    MenuItemId = item.ID,
                    Category = categoryName,
                    FoodName = foodName,
                    MealType = item.MealType,
                    SelectionCount = count
                });
            }

          
            var allCategories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();
            ViewBag.Categories = allCategories;

            ViewBag.ReportDate = reportDate;
            ViewBag.MealType = mealType;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.ReportData = reportData;
            ViewBag.DateString = date;

            return View();
        }

   
        public ActionResult ExportDailyReport(string date = null, string mealType = "", int? categoryId = null)
        {
            DateTime reportDate;
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reportDate))
            {
                reportDate = DateTime.Today;
            }

            string email = User.Identity.Name;
            var currentUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            int companyId = currentUser.CompanyID;

            var menuItems = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.Date == reportDate && m.IsActive);

            if (!string.IsNullOrWhiteSpace(mealType))
            {
                menuItems = menuItems.Where(m => m.MealType == mealType);
            }

            var menuList = menuItems.ToList();
            var menuItemIds = menuList.Select(m => m.ID).ToList();

            var selectionCounts = _context.Selections
                .Where(s => s.CompanyID == companyId && menuItemIds.Contains(s.MenuItemID))
                .GroupBy(s => s.MenuItemID)
                .Select(g => new { MenuItemId = g.Key, Count = g.Count() })
                .ToList();

            var foodIds = menuList.Select(m => m.FoodID).Distinct().ToList();
            var foodsDict = _context.Foods.Where(f => foodIds.Contains(f.ID)).ToDictionary(f => f.ID, f => f.Name);
            var foodCategoryDict = _context.Foods.Where(f => foodIds.Contains(f.ID) && f.CategoryID.HasValue).ToDictionary(f => f.ID, f => f.CategoryID.Value);
            var categoryIds = foodCategoryDict.Values.Distinct().ToList();
            var categoryNames = _context.FoodCategories.Where(c => categoryIds.Contains(c.ID)).ToDictionary(c => c.ID, c => c.Name);

            using (var workbook = new XLWorkbook())
            {
            
                var summarySheet = workbook.Worksheets.Add("Özet");
                summarySheet.Cell(1, 1).Value = "Kategori";
                summarySheet.Cell(1, 2).Value = "Yemek Adı";
                summarySheet.Cell(1, 3).Value = "Öğün";
                summarySheet.Cell(1, 4).Value = "Seçim Sayısı";
                summarySheet.Range(1, 1, 1, 4).Style.Font.Bold = true;

                int row = 2;
                foreach (var item in menuList)
                {
                    int count = selectionCounts
                        .Where(sc => sc.MenuItemId == item.ID)
                        .Select(sc => sc.Count)
                        .FirstOrDefault();

                    if (count == 0) continue;

                    string foodName = foodsDict.ContainsKey(item.FoodID) ? foodsDict[item.FoodID] : "Bilinmeyen";
                    string catName = "—";
                    int catId = 0;
                    if (foodCategoryDict.ContainsKey(item.FoodID))
                    {
                        catId = foodCategoryDict[item.FoodID];
                        if (categoryNames.ContainsKey(catId)) catName = categoryNames[catId];
                    }

                   
                    if (categoryId.HasValue && categoryId.Value > 0 && catId != categoryId.Value) continue;

                    summarySheet.Cell(row, 1).Value = catName;
                    summarySheet.Cell(row, 2).Value = foodName;
                    summarySheet.Cell(row, 3).Value = item.MealType;
                    summarySheet.Cell(row, 4).Value = count;
                    row++;
                }

                summarySheet.Columns().AdjustToContents();

              
                var selectionsDetail = _context.Selections
                    .Where(s => s.CompanyID == companyId && menuItemIds.Contains(s.MenuItemID))
                    .ToList();

                if (selectionsDetail.Any())
                {
                    var userIds = selectionsDetail.Select(s => s.UserID).Distinct().ToList();
                    var userNames = _context.Users.Where(u => userIds.Contains(u.ID)).ToDictionary(u => u.ID, u => u.Name);

                    var detailSheet = workbook.Worksheets.Add("Personel Listesi");
                    detailSheet.Cell(1, 1).Value = "Personel";
                    detailSheet.Cell(1, 2).Value = "Kategori";
                    detailSheet.Cell(1, 3).Value = "Yemek";
                    detailSheet.Cell(1, 4).Value = "Öğün";
                    detailSheet.Range(1, 1, 1, 4).Style.Font.Bold = true;

                    row = 2;
                    foreach (var sel in selectionsDetail)
                    {
                        var menuItem = menuList.FirstOrDefault(m => m.ID == sel.MenuItemID);
                        if (menuItem == null) continue;

                        string personName = userNames.ContainsKey(sel.UserID) ? userNames[sel.UserID] : "Bilinmeyen";
                        string foodName = foodsDict.ContainsKey(menuItem.FoodID) ? foodsDict[menuItem.FoodID] : "Bilinmeyen";
                        string catName = "—";
                        int catId = 0;
                        if (foodCategoryDict.ContainsKey(menuItem.FoodID))
                        {
                            catId = foodCategoryDict[menuItem.FoodID];
                            if (categoryNames.ContainsKey(catId)) catName = categoryNames[catId];
                        }

                        if (categoryId.HasValue && categoryId.Value > 0 && catId != categoryId.Value) continue;

                        detailSheet.Cell(row, 1).Value = personName;
                        detailSheet.Cell(row, 2).Value = catName;
                        detailSheet.Cell(row, 3).Value = foodName;
                        detailSheet.Cell(row, 4).Value = menuItem.MealType;
                        row++;
                    }

                    detailSheet.Columns().AdjustToContents();
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"GunSonuRaporu_{reportDate:yyyyMMdd}{(string.IsNullOrWhiteSpace(mealType) ? "" : "_" + mealType)}.xlsx";
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ReportRow
    {
        public int MenuItemId { get; set; }
        public string Category { get; set; }
        public string FoodName { get; set; }
        public string MealType { get; set; }
        public int SelectionCount { get; set; }
    }
}