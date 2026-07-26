using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class CategoryController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;


        public ActionResult CategoryManagement(string search = "")
        {
            int companyId = GetCurrentUserCompanyId();
            var categories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId);

   
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                categories = categories.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term))
                );
            }


            var orderedCategories = categories
                .OrderBy(c => c.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_CategoryList", orderedCategories);
            }

            ViewBag.Search = search;
            return View(orderedCategories);
        }

        [HttpGet]
        public ActionResult AddCategory()
        {
            return PartialView("_AddCategoryModal", new FoodCategory());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCategory(FoodCategory model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            model.Name = model.Name?.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

     
            if (model.DisplayOrder.HasValue)
            {
                bool orderTaken = _context.FoodCategories
                    .Any(c => c.CompanyID == companyId && c.DisplayOrder == model.DisplayOrder.Value);

                if (orderTaken)
                {
                    return Json(new { success = false, message = $"'{model.DisplayOrder}' sıra numarası zaten başka bir kategoriye atanmış. Lütfen farklı bir sıra numarası seçin." });
                }
            }

            model.CompanyID = companyId;
            model.IsActive = true;

            _context.FoodCategories.Add(model);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Kategori Eklendi", model.ID);

            return Json(new { success = true, message = "Kategori başarıyla eklendi." });
        }

 
        [HttpGet]
        public ActionResult EditCategory(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var category = _context.FoodCategories
                .FirstOrDefault(c => c.ID == id && c.CompanyID == companyId);

            if (category == null) return HttpNotFound();
            return PartialView("_EditCategoryModal", category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(int id, FoodCategory model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var category = _context.FoodCategories
                .FirstOrDefault(c => c.ID == id && c.CompanyID == companyId);

            if (category == null)
                return Json(new { success = false, message = "Kategori bulunamadı." });

    
            string trimmedName = model.Name?.Trim();
            string trimmedDescription = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

     
            if (model.DisplayOrder.HasValue)
            {
                bool orderTaken = _context.FoodCategories
                    .Any(c => c.CompanyID == companyId && c.ID != id && c.DisplayOrder == model.DisplayOrder.Value);

                if (orderTaken)
                {
                    return Json(new { success = false, message = $"'{model.DisplayOrder}' sıra numarası zaten başka bir kategoriye atanmış. Lütfen farklı bir sıra numarası seçin." });
                }
            }

            category.Name = trimmedName;
            category.Description = trimmedDescription;
            category.DisplayOrder = model.DisplayOrder;

            bool isActive = Request.Form["IsActive"] != null && Request.Form["IsActive"] == "true";
            category.IsActive = isActive;

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Kategori Düzenlendi", category.ID);
            _context.SaveChanges();
            return Json(new { success = true, message = "Kategori başarıyla güncellendi." });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleCategoryStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var category = _context.FoodCategories
                .FirstOrDefault(c => c.ID == id && c.CompanyID == companyId);

            if (category == null)
                return Json(new { success = false, message = "Kategori bulunamadı." });

            category.IsActive = !category.IsActive;
            string status = category.IsActive ? "aktif" : "pasif";
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Kategori {status} yapıldı", category.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Kategori {status} hale getirildi." });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var category = _context.FoodCategories
                .FirstOrDefault(c => c.ID == id && c.CompanyID == companyId);

            if (category == null)
                return Json(new { success = false, message = "Kategori bulunamadı." });

            category.IsActive = false;
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Kategori Silindi (Pasife Alındı)", category.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = "Kategori başarıyla silindi (pasife alındı)." });
        }
    }
}
