using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class ProductionController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

        public ActionResult ProductionManagement()
        {
            int companyId = GetCurrentUserCompanyId();
            var records = _context.ProductionRecords
                .Where(p => p.CompanyID == companyId)
                .OrderByDescending(p => p.Date)
                .ToList();

            var foodIds = records.Select(r => r.FoodID).Distinct().ToList();
            ViewBag.FoodNames = _context.Foods.Where(f => foodIds.Contains(f.ID)).ToDictionary(f => f.ID, f => f.Name);

            if (Request.IsAjaxRequest())
                return PartialView("_ProductionList", records);

            return View(records);
        }

        [HttpGet]
        public ActionResult AddProduction()
        {
            int companyId = GetCurrentUserCompanyId();
            ViewBag.Foods = _context.Foods.Where(f => f.CompanyID == companyId && f.IsActive).OrderBy(f => f.Name).ToList();
          
            ViewBag.MealTypes = _context.MealTypes
                .Where(m => m.CompanyID == companyId && m.IsActive)
                .OrderBy(m => m.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(m => m.DisplayOrder)
                .ToList();
            return PartialView("_AddProductionModal", new ProductionRecord { Date = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProduction(ProductionRecord model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

           
            if ((model.PlannedDemandQuantity ?? 0) < 0 || (model.ProducedQuantity ?? 0) < 0 || (model.ActualConsumedQuantity ?? 0) < 0)
            {
                return Json(new { success = false, message = "Planlanan, üretilen ve tüketilen miktarlar negatif olamaz." });
            }

            var existingRecord = _context.ProductionRecords
                .FirstOrDefault(p => p.CompanyID == companyId
                                    && p.Date == model.Date
                                    && p.FoodID == model.FoodID
                                    && p.MealType == model.MealType);

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int producedQty = model.ProducedQuantity ?? 0;
                    string msg;
                    int affectedId;

                    if (existingRecord != null)
                    {
                        existingRecord.PlannedDemandQuantity = (existingRecord.PlannedDemandQuantity ?? 0) + (model.PlannedDemandQuantity ?? 0);
                        existingRecord.ProducedQuantity = (existingRecord.ProducedQuantity ?? 0) + producedQty;
                        existingRecord.ActualConsumedQuantity = (existingRecord.ActualConsumedQuantity ?? 0) + (model.ActualConsumedQuantity ?? 0);
                        existingRecord.Note = model.Note;
                        _context.SaveChanges();
                        msg = "Mevcut üretim kaydı güncellendi. ";
                        affectedId = existingRecord.ID;
                    }
                    else
                    {
                        model.CompanyID = companyId;
                        _context.ProductionRecords.Add(model);
                        _context.SaveChanges();
                        msg = "Yeni üretim kaydı eklendi. ";
                        affectedId = model.ID;
                    }

                    if (producedQty > 0)
                    {
                  
                        DeductStockForProduction(companyId, model.FoodID, producedQty, affectedId);
                    }

                    ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Üretim Kaydı Eklendi/Güncellendi", affectedId);
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true, message = msg + "Stok güncellendi." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Üretim kaydedilirken hata: " + ex.Message });
                }
            }
        }

        [HttpGet]
        public ActionResult EditProduction(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var record = _context.ProductionRecords.FirstOrDefault(p => p.ID == id && p.CompanyID == companyId);
            if (record == null) return HttpNotFound();

            ViewBag.Foods = _context.Foods.Where(f => f.CompanyID == companyId && f.IsActive).OrderBy(f => f.Name).ToList();
            ViewBag.MealTypes = _context.MealTypes
                .Where(m => m.CompanyID == companyId && m.IsActive)
                .OrderBy(m => m.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(m => m.DisplayOrder)
                .ToList();
            return PartialView("_EditProductionModal", record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduction(int id, ProductionRecord model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            if ((model.PlannedDemandQuantity ?? 0) < 0 || (model.ProducedQuantity ?? 0) < 0 || (model.ActualConsumedQuantity ?? 0) < 0)
            {
                return Json(new { success = false, message = "Planlanan, üretilen ve tüketilen miktarlar negatif olamaz." });
            }

            var record = _context.ProductionRecords.FirstOrDefault(p => p.ID == id && p.CompanyID == companyId);
            if (record == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int oldProducedQty = record.ProducedQuantity ?? 0;
                    int oldFoodId = record.FoodID;
                    int newProducedQty = model.ProducedQuantity ?? 0;

                    record.Date = model.Date;
                    record.MealType = model.MealType;
                    record.FoodID = model.FoodID;
                    record.PlannedDemandQuantity = model.PlannedDemandQuantity;
                    record.ProducedQuantity = model.ProducedQuantity;
                    record.ActualConsumedQuantity = model.ActualConsumedQuantity;
                    record.Note = model.Note;
                    _context.SaveChanges();

                
                    if (oldFoodId != model.FoodID)
                    {
                        if (oldProducedQty > 0)
                            RestoreStockForProduction(companyId, oldFoodId, oldProducedQty, record.ID);
                        if (newProducedQty > 0)
                            DeductStockForProduction(companyId, model.FoodID, newProducedQty, record.ID);
                    }
                    else
                    {
                        int delta = newProducedQty - oldProducedQty;
                        if (delta > 0)
                            DeductStockForProduction(companyId, model.FoodID, delta, record.ID);
                        else if (delta < 0)
                            RestoreStockForProduction(companyId, model.FoodID, -delta, record.ID);
                    }

                    ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Üretim Kaydı Düzenlendi", record.ID);
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true, message = "Üretim kaydı güncellendi. Stok farkı işlendi." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Güncelleme sırasında hata: " + ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduction(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var record = _context.ProductionRecords.FirstOrDefault(p => p.ID == id && p.CompanyID == companyId);
            if (record == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                 
                    int producedQty = record.ProducedQuantity ?? 0;
                    if (producedQty > 0)
                    {
                        RestoreStockForProduction(companyId, record.FoodID, producedQty, record.ID);
                    }

                    _context.ProductionRecords.Remove(record);
                    ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Üretim Kaydı Silindi (stok iade edildi)", id);
                    _context.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true, message = "Üretim kaydı silindi ve ilgili stok iade edildi." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Silme sırasında hata: " + ex.Message });
                }
            }
        }

   
        private void DeductStockForProduction(int companyId, int foodId, int portions, int? productionRecordId)
        {
            var recipes = _context.RecipeIngredients
                .Where(r => r.CompanyID == companyId && r.FoodID == foodId)
                .ToList();

            foreach (var recipe in recipes)
            {
                var stockItem = _context.StockItems.FirstOrDefault(s => s.ID == recipe.StockItemID && s.CompanyID == companyId);
                if (stockItem == null)
                    throw new Exception($"Reçetedeki malzeme (ID:{recipe.StockItemID}) stokta bulunamadı.");

                decimal totalRequired = recipe.RequiredQuantity * portions;
                decimal amountToDeduct = UnitHelper.Convert(totalRequired, recipe.Unit, stockItem.Unit);

                decimal currentStock = stockItem.CurrentQuantity ?? 0;
                if (currentStock < amountToDeduct)
                    throw new Exception($"Yetersiz stok! '{stockItem.Name}' için gereken: {amountToDeduct} {stockItem.Unit}, mevcut: {currentStock} {stockItem.Unit}.");

                decimal newQuantity = currentStock - amountToDeduct;
                stockItem.CurrentQuantity = newQuantity;

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyID = companyId,
                    StockItemID = stockItem.ID,
                    ChangeAmount = -amountToDeduct,
                    ResultingQuantity = newQuantity,
                    Reason = "Üretim Tüketimi",
                    RelatedProductionRecordID = productionRecordId,
                    UserID = GetCurrentUserId(),
                    CreatedAt = DateTime.Now
                });
            }
            _context.SaveChanges();
        }

        private void RestoreStockForProduction(int companyId, int foodId, int portions, int? productionRecordId)
        {
            var recipes = _context.RecipeIngredients
                .Where(r => r.CompanyID == companyId && r.FoodID == foodId)
                .ToList();

            foreach (var recipe in recipes)
            {
                var stockItem = _context.StockItems.FirstOrDefault(s => s.ID == recipe.StockItemID && s.CompanyID == companyId);
                if (stockItem == null)
                    continue; 

                decimal totalToRestore = recipe.RequiredQuantity * portions;
                decimal amountToRestore = UnitHelper.Convert(totalToRestore, recipe.Unit, stockItem.Unit);

                decimal newQuantity = (stockItem.CurrentQuantity ?? 0) + amountToRestore;
                stockItem.CurrentQuantity = newQuantity;

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyID = companyId,
                    StockItemID = stockItem.ID,
                    ChangeAmount = amountToRestore,
                    ResultingQuantity = newQuantity,
                    Reason = "Üretim İptali (İade)",
                    RelatedProductionRecordID = productionRecordId,
                    UserID = GetCurrentUserId(),
                    CreatedAt = DateTime.Now
                });
            }
            _context.SaveChanges();
        }
    }
}
