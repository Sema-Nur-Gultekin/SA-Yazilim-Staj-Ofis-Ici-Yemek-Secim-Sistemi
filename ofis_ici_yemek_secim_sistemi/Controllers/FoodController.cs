using ofis_ici_yemek_secim_sistemi.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class FoodController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

   
        public ActionResult FoodManagement(string search = "", int? categoryId = null)
        {
            int companyId = GetCurrentUserCompanyId();

            var foods = _context.Foods
                .Where(f => f.CompanyID == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                foods = foods.Where(f =>
                    f.Name.ToLower().Contains(term) ||
                    (f.Ingredients != null && f.Ingredients.ToLower().Contains(term)) ||
                    (f.Description != null && f.Description.ToLower().Contains(term)) ||
                    (f.Allergens != null && f.Allergens.ToLower().Contains(term))
                );
            }

   
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                foods = foods.Where(f => f.CategoryID == categoryId.Value);
            }

            var orderedFoods = foods.OrderBy(f => f.Name).ToList();

            var categories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId)
                .ToDictionary(c => c.ID, c => c.Name);
            ViewBag.Categories = categories;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_FoodList", orderedFoods);
            }

            ViewBag.Search = search;
            ViewBag.SelectedCategoryId = categoryId;

            return View(orderedFoods);
        }

        [HttpGet]
        public ActionResult AddFood()
        {
            int companyId = GetCurrentUserCompanyId();
            ViewBag.Categories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();
            return PartialView("_AddFoodModal", new Food());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFood(Food model)
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
            model.CreatedDate = DateTime.Now;

            _context.Foods.Add(model);
            _context.SaveChanges();

            return Json(new { success = true, message = "Yemek başarıyla eklendi." });
        }


        [HttpGet]
        public ActionResult EditFood(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var food = _context.Foods
                .FirstOrDefault(f => f.ID == id && f.CompanyID == companyId);

            if (food == null) return HttpNotFound();

            ViewBag.Categories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            return PartialView("_EditFoodModal", food);
        }

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditFood(int id, Food model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");
            ModelState.Remove("CreatedDate");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var food = _context.Foods
                .FirstOrDefault(f => f.ID == id && f.CompanyID == companyId);

            if (food == null)
                return Json(new { success = false, message = "Yemek bulunamadı." });

            food.Name = model.Name;
            food.CategoryID = model.CategoryID;
            food.Allergens = model.Allergens;
            food.Ingredients = model.Ingredients;
            food.Description = model.Description;

            bool isActive = Request.Form["IsActive"] != null && Request.Form["IsActive"] == "true";
            food.IsActive = isActive;

            _context.SaveChanges();
            return Json(new { success = true, message = "Yemek başarıyla güncellendi." });
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleFoodStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var food = _context.Foods
                .FirstOrDefault(f => f.ID == id && f.CompanyID == companyId);

            if (food == null)
                return Json(new { success = false, message = "Yemek bulunamadı." });

            food.IsActive = !food.IsActive;
            _context.SaveChanges();

            string status = food.IsActive ? "aktif" : "pasif";
            return Json(new { success = true, message = $"Yemek {status} hale getirildi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFood(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var food = _context.Foods
                .FirstOrDefault(f => f.ID == id && f.CompanyID == companyId);

            if (food == null)
                return Json(new { success = false, message = "Yemek bulunamadı." });

            food.IsActive = false;
            _context.SaveChanges();

            return Json(new { success = true, message = "Yemek başarıyla silindi (pasife alındı)." });
        }
    }
}