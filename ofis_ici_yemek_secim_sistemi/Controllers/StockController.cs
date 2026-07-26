using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class StockController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

        public ActionResult StockManagement()
        {
            int companyId = GetCurrentUserCompanyId();
            var stockItems = _context.StockItems
                .Where(s => s.CompanyID == companyId)
                .OrderBy(s => s.Name)
                .ToList();

          
            var recentMovements = (from m in _context.StockMovements
                                    join s in _context.StockItems on m.StockItemID equals s.ID
                                    join u in _context.Users on m.UserID equals u.ID into userJoin
                                    from u in userJoin.DefaultIfEmpty()
                                    where m.CompanyID == companyId
                                    orderby m.CreatedAt descending
                                    select new
                                    {
                                        m.ChangeAmount,
                                        m.ResultingQuantity,
                                        m.Reason,
                                        m.CreatedAt,
                                        StockName = s.Name,
                                        StockUnit = s.Unit,
                                        UserName = u != null ? u.Name : "Bilinmeyen"
                                    }).Take(10).ToList();

            ViewBag.RecentMovements = recentMovements
                .Select(m => new StockMovementView
                {
                    ChangeAmount = m.ChangeAmount,
                    ResultingQuantity = m.ResultingQuantity,
                    Reason = m.Reason,
                    CreatedAt = m.CreatedAt,
                    StockName = m.StockName,
                    StockUnit = m.StockUnit,
                    UserName = m.UserName
                }).ToList();

            if (Request.IsAjaxRequest())
                return PartialView("_StockList", stockItems);

            return View(stockItems);
        }

        [HttpGet]
        public ActionResult RecentMovements()
        {
            int companyId = GetCurrentUserCompanyId();
            var recentMovements = (from m in _context.StockMovements
                                    join s in _context.StockItems on m.StockItemID equals s.ID
                                    join u in _context.Users on m.UserID equals u.ID into userJoin
                                    from u in userJoin.DefaultIfEmpty()
                                    where m.CompanyID == companyId
                                    orderby m.CreatedAt descending
                                    select new StockMovementView
                                    {
                                        ChangeAmount = m.ChangeAmount,
                                        ResultingQuantity = m.ResultingQuantity,
                                        Reason = m.Reason,
                                        CreatedAt = m.CreatedAt,
                                        StockName = s.Name,
                                        StockUnit = s.Unit,
                                        UserName = u != null ? u.Name : "Bilinmeyen"
                                    }).Take(10).ToList();

            return PartialView("_RecentMovements", recentMovements);
        }

        [HttpGet]
        public ActionResult AddStock()
        {
            return PartialView("_AddStockModal", new StockItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddStock(StockItem model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            if (model.CurrentQuantity.HasValue && model.CurrentQuantity.Value < 0)
                return Json(new { success = false, message = "Başlangıç stok miktarı negatif olamaz." });
            if (model.MinimumQuantity.HasValue && model.MinimumQuantity.Value < 0)
                return Json(new { success = false, message = "Minimum stok seviyesi negatif olamaz." });

            model.CompanyID = companyId;
            model.IsActive = true;
            model.Name = model.Name?.Trim();
            model.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim();

            _context.StockItems.Add(model);
            _context.SaveChanges();

         
            if (model.CurrentQuantity.HasValue && model.CurrentQuantity.Value > 0)
            {
                LogStockMovement(companyId, model.ID, model.CurrentQuantity.Value, model.CurrentQuantity.Value, "Stok Girişi (Başlangıç)", null);
            }

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Stok Kalemi Eklendi", model.ID);

            return Json(new { success = true, message = "Stok kalemi eklendi." });
        }

        [HttpGet]
        public ActionResult EditStock(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null) return HttpNotFound();
            return PartialView("_EditStockModal", item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStock(int id, StockItem model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            if (model.MinimumQuantity.HasValue && model.MinimumQuantity.Value < 0)
                return Json(new { success = false, message = "Minimum stok seviyesi negatif olamaz." });

            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null)
                return Json(new { success = false, message = "Stok kalemi bulunamadı." });

        
            if (!string.Equals(item.Unit, model.Unit, StringComparison.OrdinalIgnoreCase))
            {
                bool usedInRecipe = _context.RecipeIngredients.Any(r => r.CompanyID == companyId && r.StockItemID == id);
                if (usedInRecipe)
                {
                    return Json(new { success = false, message = "Bu malzeme reçetelerde kullanıldığı için birimi değiştirilemez. Önce ilgili reçete satırlarını güncelleyin." });
                }
            }

            item.Name = model.Name?.Trim();
            item.Unit = model.Unit;
            item.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim();
            item.MinimumQuantity = model.MinimumQuantity;
            item.IsActive = model.IsActive;
    

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Stok Kalemi Düzenlendi", item.ID);
            _context.SaveChanges();
            return Json(new { success = true, message = "Stok kalemi güncellendi." });
        }


        [HttpGet]
        public ActionResult RestockItem(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null) return HttpNotFound();
            return PartialView("_RestockModal", item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestockItem(int id, decimal amount, string reason)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null)
                return Json(new { success = false, message = "Stok kalemi bulunamadı." });

            if (amount == 0)
                return Json(new { success = false, message = "Miktar sıfır olamaz." });

            decimal newQuantity = (item.CurrentQuantity ?? 0) + amount;
            if (newQuantity < 0)
                return Json(new { success = false, message = $"Bu işlem stok miktarını negatife düşürür (mevcut: {item.CurrentQuantity ?? 0} {item.Unit})." });

            item.CurrentQuantity = newQuantity;
            string reasonText = string.IsNullOrWhiteSpace(reason) ? (amount > 0 ? "Stok Girişi" : "Manuel Düzeltme") : reason.Trim();
            LogStockMovement(companyId, item.ID, amount, newQuantity, reasonText, null);

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Stok Girişi/Düzeltmesi: {(amount > 0 ? "+" : "")}{amount} {item.Unit}", item.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Stok güncellendi. Yeni miktar: {newQuantity} {item.Unit}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStockStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null)
                return Json(new { success = false, message = "Stok kalemi bulunamadı." });

      
            if (item.IsActive)
            {
                bool usedInRecipe = _context.RecipeIngredients.Any(r => r.CompanyID == companyId && r.StockItemID == id);
                if (usedInRecipe)
                {
                    return Json(new { success = false, message = "Bu malzeme bir veya daha fazla reçetede kullanıldığı için pasife alınamaz. Önce ilgili reçetelerden kaldırın." });
                }
            }

            item.IsActive = !item.IsActive;
            string status = item.IsActive ? "aktif" : "pasif";
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Stok Kalemi {status} yapıldı", item.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Stok kalemi {status} hale getirildi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStock(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null)
                return Json(new { success = false, message = "Stok kalemi bulunamadı." });

          
            bool usedInRecipe = _context.RecipeIngredients.Any(r => r.CompanyID == companyId && r.StockItemID == id);
            if (usedInRecipe)
            {
                return Json(new { success = false, message = "Bu malzeme bir veya daha fazla reçetede kullanıldığı için silinemez. Önce ilgili reçetelerden kaldırın." });
            }

            item.IsActive = false;
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Stok Kalemi Silindi (Pasife Alındı)", item.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = "Stok kalemi başarıyla silindi (pasife alındı)." });
        }

    
        internal void LogStockMovement(int companyId, int stockItemId, decimal changeAmount, decimal resultingQuantity, string reason, int? relatedProductionRecordId)
        {
            _context.StockMovements.Add(new StockMovement
            {
                CompanyID = companyId,
                StockItemID = stockItemId,
                ChangeAmount = changeAmount,
                ResultingQuantity = resultingQuantity,
                Reason = reason,
                RelatedProductionRecordID = relatedProductionRecordId,
                UserID = GetCurrentUserId(),
                CreatedAt = DateTime.Now
            });
        }
    }


    public class StockMovementView
    {
        public decimal ChangeAmount { get; set; }
        public decimal ResultingQuantity { get; set; }
        public string Reason { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string StockName { get; set; }
        public string StockUnit { get; set; }
        public string UserName { get; set; }
    }
}
