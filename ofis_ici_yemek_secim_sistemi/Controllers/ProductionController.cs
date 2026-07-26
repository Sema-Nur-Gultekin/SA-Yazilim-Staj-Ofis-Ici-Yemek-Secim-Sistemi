/*
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using ofis_ici_yemek_secim_sistemi.Models;
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
                    string msg = "";

                    if (existingRecord != null)
                    {
                        existingRecord.PlannedDemandQuantity = (existingRecord.PlannedDemandQuantity ?? 0) + (model.PlannedDemandQuantity ?? 0);
                        existingRecord.ProducedQuantity = (existingRecord.ProducedQuantity ?? 0) + producedQty;
                        existingRecord.ActualConsumedQuantity = (existingRecord.ActualConsumedQuantity ?? 0) + (model.ActualConsumedQuantity ?? 0);
                        existingRecord.Note = model.Note;
                        _context.SaveChanges();
                        msg = "Mevcut üretim kaydı güncellendi. ";
                    }
                    else
                    {
                        model.CompanyID = companyId;
                        _context.ProductionRecords.Add(model);
                        _context.SaveChanges();
                        msg = "Yeni üretim kaydı eklendi. ";
                    }

                    if (producedQty > 0)
                    {
                        var recipes = _context.RecipeIngredients
                            .Where(r => r.CompanyID == companyId && r.FoodID == model.FoodID)
                            .ToList();

                        foreach (var recipe in recipes)
                        {
                            var stockItem = _context.StockItems.FirstOrDefault(s => s.ID == recipe.StockItemID && s.CompanyID == companyId);
                            if (stockItem == null)
                                throw new Exception($"Reçetedeki malzeme (ID:{recipe.StockItemID}) stokta bulunamadı.");

                            decimal requiredPerPortion = recipe.RequiredQuantity;
                            decimal totalRequired = requiredPerPortion * producedQty;
                            decimal amountToDeduct = ConvertToStockUnit(totalRequired, recipe.Unit, stockItem.Unit);

                            decimal currentStock = stockItem.CurrentQuantity ?? 0;
                            if (currentStock < amountToDeduct)
                                throw new Exception($"Yetersiz stok! '{stockItem.Name}' için gereken: {amountToDeduct} {stockItem.Unit}, mevcut: {currentStock} {stockItem.Unit}.");

                            stockItem.CurrentQuantity = currentStock - amountToDeduct;
                        }
                        _context.SaveChanges();
                    }

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

            var record = _context.ProductionRecords.FirstOrDefault(p => p.ID == id && p.CompanyID == companyId);
            if (record == null)
                return Json(new { success = false, message = "Kayıt bulunamadı." });

            record.Date = model.Date;
            record.MealType = model.MealType;
            record.FoodID = model.FoodID;
            record.PlannedDemandQuantity = model.PlannedDemandQuantity;
            record.ProducedQuantity = model.ProducedQuantity;
            record.ActualConsumedQuantity = model.ActualConsumedQuantity;
            record.Note = model.Note;

            _context.SaveChanges();
            return Json(new { success = true, message = "Üretim kaydı güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduction(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var record = _context.ProductionRecords.FirstOrDefault(p => p.ID == id && p.CompanyID == companyId);
            if (record != null)
            {
                _context.ProductionRecords.Remove(record);
                _context.SaveChanges();
            }
            return RedirectToAction("ProductionManagement");
        }

        private decimal ConvertToStockUnit(decimal amount, string recipeUnit, string stockUnit)
        {
            if (string.IsNullOrEmpty(recipeUnit) || string.IsNullOrEmpty(stockUnit))
                throw new Exception("Birim bilgisi eksik.");

            string NormalizeUnit(string unit)
            {
                if (string.IsNullOrWhiteSpace(unit)) return "";
                string u = unit.Trim().ToLower();
                if (u == "g" || u == "gr" || u == "gram" || u == "grams") return "g";
                if (u == "kg" || u == "kilogram" || u == "kilograms") return "kg";
                if (u == "ml" || u == "mililitre" || u == "milliliter" || u == "millilitres") return "ml";
                if (u == "l" || u == "lt" || u == "litre" || u == "liter" || u == "litres") return "lt";
                return u;
            }

            string from = NormalizeUnit(recipeUnit);
            string to = NormalizeUnit(stockUnit);

            if (from == to) return amount;
            if (from == "kg" && to == "g") return amount * 1000;
            if (from == "g" && to == "kg") return amount / 1000;
            if (from == "lt" && to == "ml") return amount * 1000;
            if (from == "ml" && to == "lt") return amount / 1000;

            throw new Exception($"'{recipeUnit}' biriminden '{stockUnit}' birimine dönüşüm desteklenmemektedir.");
        }
    }
}
*/