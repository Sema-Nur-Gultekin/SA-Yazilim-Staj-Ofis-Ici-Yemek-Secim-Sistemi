using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Filters;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{

    public class ActionStat
    {
        public string ActionName { get; set; }
        public int Count { get; set; }
    }


    public class DailyActivityStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }


    public class RecentLogEntry
    {
        public string UserName { get; set; }
        public string ActionName { get; set; }
        public DateTime ActionTime { get; set; }
    }


    public class SetupGap
    {
        public string Label { get; set; }
        public string ActionUrl { get; set; }
        public string ActionText { get; set; }
    }

    [CustomAuthorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        protected readonly AppDbContext _context = new AppDbContext();


        protected int GetCurrentUserCompanyId()
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.CompanyID ?? 0;
        }


        protected int GetCurrentUserId()
        {
            string email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.ID ?? 0;
        }

        public virtual ActionResult Index()
        {
            int companyId = GetCurrentUserCompanyId();

            DateTime todayStart = DateTime.Today;
            DateTime tomorrowStart = todayStart.AddDays(1);
            int todayActionCount = _context.ActivityLogs
                .Count(l => l.CompanyID == companyId && l.ActionTime >= todayStart && l.ActionTime < tomorrowStart);

      
            DateTime last30Start = todayStart.AddDays(-30);
            var topActions = _context.ActivityLogs
                .Where(l => l.CompanyID == companyId && l.ActionTime >= last30Start)
                .GroupBy(l => l.ActionName)
                .Select(g => new ActionStat { ActionName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToList();

            ViewBag.TodayActionCount = todayActionCount;
            ViewBag.TopActions = topActions;

      
            ViewBag.TotalFoodCount = _context.Foods.Count(f => f.CompanyID == companyId && f.IsActive);
            ViewBag.TotalUserCount = _context.Users.Count(u => u.CompanyID == companyId && u.Role == "User" && u.IsActive);
            ViewBag.TotalMenuTodayCount = _context.MenuItems.Count(m => m.CompanyID == companyId && m.IsActive && m.Date == todayStart);

            DateTime last7Start = todayStart.AddDays(-6);
            var last7Logs = _context.ActivityLogs
                .Where(l => l.CompanyID == companyId && l.ActionTime >= last7Start && l.ActionTime < tomorrowStart)
                .Select(l => l.ActionTime)
                .ToList();
            var dailyStats = new System.Collections.Generic.List<DailyActivityStat>();
            for (int i = 0; i < 7; i++)
            {
                DateTime day = last7Start.AddDays(i);
                int count = last7Logs.Count(t => t.Date == day);
                dailyStats.Add(new DailyActivityStat { Date = day, Count = count });
            }
            ViewBag.DailyStats = dailyStats;

            var recentLogs = (from l in _context.ActivityLogs
                               join u in _context.Users on l.UserID equals u.ID into userJoin
                               from u in userJoin.DefaultIfEmpty()
                               where l.CompanyID == companyId
                               orderby l.ActionTime descending
                               select new RecentLogEntry
                               {
                                   UserName = u != null ? u.Name : "Silinmiş Kullanıcı",
                                   ActionName = l.ActionName,
                                   ActionTime = l.ActionTime
                               }).Take(8).ToList();
            ViewBag.RecentLogs = recentLogs;


            var todayMenuItemIdsForRate = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive && m.Date == todayStart)
                .Select(m => m.ID)
                .ToList();
            int selectedTodayCount = todayMenuItemIdsForRate.Any()
                ? _context.Selections
                    .Where(s => s.CompanyID == companyId && todayMenuItemIdsForRate.Contains(s.MenuItemID))
                    .Select(s => s.UserID)
                    .Distinct()
                    .Count()
                : 0;
            ViewBag.SelectedTodayCount = selectedTodayCount;

    
            var setupGaps = new System.Collections.Generic.List<SetupGap>();

            var activeMealTypeNames = _context.MealTypes
                .Where(m => m.CompanyID == companyId && m.IsActive)
                .Select(m => m.Name)
                .ToList();
            var mealTypesWithMenuToday = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive && m.Date == todayStart)
                .Select(m => m.MealType)
                .Distinct()
                .ToList();
            foreach (var mealName in activeMealTypeNames.Except(mealTypesWithMenuToday))
            {
                setupGaps.Add(new SetupGap
                {
                    Label = $"\"{mealName}\" öğünü için bugün henüz hiç yemek eklenmedi.",
                    ActionUrl = Url.Action("MenuManagement", "Menu"),
                    ActionText = "Menüye Git"
                });
            }

            var todayFoodIds = _context.MenuItems
                .Where(m => m.CompanyID == companyId && m.IsActive && m.Date == todayStart)
                .Select(m => m.FoodID)
                .Distinct()
                .ToList();
            var todayCategoryIds = _context.Foods
                .Where(f => todayFoodIds.Contains(f.ID) && f.CategoryID.HasValue)
                .Select(f => f.CategoryID.Value)
                .Distinct()
                .ToList();
            var activeCategories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .ToList();
            foreach (var cat in activeCategories.Where(c => !todayCategoryIds.Contains(c.ID)))
            {
                setupGaps.Add(new SetupGap
                {
                    Label = $"\"{cat.Name}\" kategorisinden bugünün menüsünde hiç yemek yok.",
                    ActionUrl = Url.Action("MenuManagement", "Menu"),
                    ActionText = "Menüye Git"
                });
            }

  
            var lowStockItems = _context.StockItems
                .Where(s => s.CompanyID == companyId && s.IsActive
                            && s.CurrentQuantity.HasValue && s.MinimumQuantity.HasValue
                            && s.CurrentQuantity.Value < s.MinimumQuantity.Value)
                .ToList();
            foreach (var stock in lowStockItems)
            {
                setupGaps.Add(new SetupGap
                {
                    Label = $"'{stock.Name}' stok seviyesi kritik (mevcut: {stock.CurrentQuantity:N2} {stock.Unit}, min: {stock.MinimumQuantity:N2} {stock.Unit}).",
                    ActionUrl = Url.Action("StockManagement", "Stock"),
                    ActionText = "Stok Girişi Yap"
                });
            }

            ViewBag.SetupGaps = setupGaps;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
