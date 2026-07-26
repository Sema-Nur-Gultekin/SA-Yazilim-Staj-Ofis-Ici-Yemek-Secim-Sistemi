using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{

    public class MealController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;


        public ActionResult MealManagement(string search = "")
        {
            int companyId = GetCurrentUserCompanyId();
            var meals = _context.MealTypes
                .Where(m => m.CompanyID == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                meals = meals.Where(m => m.Name.ToLower().Contains(term));
            }

  
            var orderedMeals = meals
                .OrderBy(m => m.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(m => m.DisplayOrder)
                .ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_MealList", orderedMeals);
            }

            ViewBag.Search = search;
            return View(orderedMeals);
        }


        [HttpGet]
        public ActionResult AddMeal()
        {
            return PartialView("_AddMealModal", new MealType());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMeal(MealType model)
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

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Öğün adı boş olamaz." });
            }

            bool nameTaken = _context.MealTypes
                .Any(m => m.CompanyID == companyId && m.Name.ToLower() == model.Name.ToLower());
            if (nameTaken)
            {
                return Json(new { success = false, message = $"'{model.Name}' adında bir öğün zaten mevcut." });
            }

      
            if (model.DisplayOrder.HasValue)
            {
                bool orderTaken = _context.MealTypes
                    .Any(m => m.CompanyID == companyId && m.DisplayOrder == model.DisplayOrder.Value);
                if (orderTaken)
                {
                    return Json(new { success = false, message = $"'{model.DisplayOrder}' sıra numarası zaten başka bir öğüne atanmış." });
                }
            }

            model.CompanyID = companyId;
            model.IsActive = true;

            _context.MealTypes.Add(model);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Öğün Eklendi", model.ID);

            return Json(new { success = true, message = "Öğün başarıyla eklendi." });
        }

     
        [HttpGet]
        public ActionResult EditMeal(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var meal = _context.MealTypes.FirstOrDefault(m => m.ID == id && m.CompanyID == companyId);
            if (meal == null) return HttpNotFound();
            return PartialView("_EditMealModal", meal);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMeal(int id, MealType model)
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

            var meal = _context.MealTypes.FirstOrDefault(m => m.ID == id && m.CompanyID == companyId);
            if (meal == null)
                return Json(new { success = false, message = "Öğün bulunamadı." });

            string trimmedName = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return Json(new { success = false, message = "Öğün adı boş olamaz." });
            }

            bool nameTaken = _context.MealTypes
                .Any(m => m.CompanyID == companyId && m.ID != id && m.Name.ToLower() == trimmedName.ToLower());
            if (nameTaken)
            {
                return Json(new { success = false, message = $"'{trimmedName}' adında bir öğün zaten mevcut." });
            }

            if (model.DisplayOrder.HasValue)
            {
                bool orderTaken = _context.MealTypes
                    .Any(m => m.CompanyID == companyId && m.ID != id && m.DisplayOrder == model.DisplayOrder.Value);
                if (orderTaken)
                {
                    return Json(new { success = false, message = $"'{model.DisplayOrder}' sıra numarası zaten başka bir öğüne atanmış." });
                }
            }

 
            meal.Name = trimmedName;
            meal.DisplayOrder = model.DisplayOrder;

            bool isActive = Request.Form["IsActive"] != null && Request.Form["IsActive"] == "true";
            meal.IsActive = isActive;

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Öğün Düzenlendi", meal.ID);
            _context.SaveChanges();
            return Json(new { success = true, message = "Öğün başarıyla güncellendi." });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleMealStatus(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var meal = _context.MealTypes.FirstOrDefault(m => m.ID == id && m.CompanyID == companyId);
            if (meal == null)
                return Json(new { success = false, message = "Öğün bulunamadı." });

            meal.IsActive = !meal.IsActive;
            string status = meal.IsActive ? "aktif" : "pasif";
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Öğün {status} yapıldı", meal.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = $"Öğün {status} hale getirildi." });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMeal(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var meal = _context.MealTypes.FirstOrDefault(m => m.ID == id && m.CompanyID == companyId);
            if (meal == null)
                return Json(new { success = false, message = "Öğün bulunamadı." });

            meal.IsActive = false;
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Öğün Silindi (Pasife Alındı)", meal.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = "Öğün başarıyla silindi (pasife alındı)." });
        }
    }
}
