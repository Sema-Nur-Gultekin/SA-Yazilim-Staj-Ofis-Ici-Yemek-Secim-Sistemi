using ofis_ici_yemek_secim_sistemi.Models;
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
                .OrderBy(c => c.DisplayOrder)
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

            model.CompanyID = companyId;
            model.IsActive = true;

            _context.FoodCategories.Add(model);
            _context.SaveChanges();

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

            category.Name = model.Name;
            category.Description = model.Description;
            category.DisplayOrder = model.DisplayOrder;

          
            bool isActive = Request.Form["IsActive"] != null && Request.Form["IsActive"] == "true";
            category.IsActive = isActive;

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
            _context.SaveChanges();

            string status = category.IsActive ? "aktif" : "pasif";
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
            _context.SaveChanges();

            return Json(new { success = true, message = "Kategori başarıyla silindi (pasife alındı)." });
        }
    }
}