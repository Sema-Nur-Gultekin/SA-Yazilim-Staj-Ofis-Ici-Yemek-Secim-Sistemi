/*
using ofis_ici_yemek_secim_sistemi.Models;
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

            if (Request.IsAjaxRequest())
                return PartialView("_StockList", stockItems);

            return View(stockItems);
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

            model.CompanyID = companyId;
            model.IsActive = true;

            _context.StockItems.Add(model);
            _context.SaveChanges();
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

            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item == null)
                return Json(new { success = false, message = "Stok kalemi bulunamadı." });

            item.Name = model.Name;
            item.Unit = model.Unit;
            item.CurrentQuantity = model.CurrentQuantity;
            item.MinimumQuantity = model.MinimumQuantity;
            item.IsActive = model.IsActive;

            _context.SaveChanges();
            return Json(new { success = true, message = "Stok kalemi güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStockStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                _context.SaveChanges();
            }
            return RedirectToAction("StockManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStock(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var item = _context.StockItems.FirstOrDefault(s => s.ID == id && s.CompanyID == companyId);
            if (item != null)
            {
                item.IsActive = false;
                _context.SaveChanges();
            }
            return RedirectToAction("StockManagement");
        }
    }
}
*/