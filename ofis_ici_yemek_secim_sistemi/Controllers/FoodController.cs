using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class FoodController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

        
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const int MaxImageSizeBytes = 5 * 1024 * 1024; 


        private string SaveFoodImage(HttpPostedFileBase image, out string errorMessage)
        {
            errorMessage = null;
            if (image == null || image.ContentLength <= 0)
                return null;

            string ext = Path.GetExtension(image.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            {
                errorMessage = "Yalnızca JPG, PNG veya WEBP formatında görsel yükleyebilirsiniz.";
                return null;
            }

            if (image.ContentLength > MaxImageSizeBytes)
            {
                errorMessage = "Görsel boyutu 5 MB'ı geçemez.";
                return null;
            }

  
            if (!IsValidImageSignature(image))
            {
                errorMessage = "Yüklenen dosya geçerli bir görsel dosyası değil.";
                return null;
            }

            string folderPath = Server.MapPath("~/Content/uploads/foods");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = Guid.NewGuid().ToString("N") + ext;
            string fullPath = Path.Combine(folderPath, fileName);
            image.SaveAs(fullPath);

            return "/Content/uploads/foods/" + fileName;
        }

   
        private bool IsValidImageSignature(HttpPostedFileBase image)
        {
            try
            {
                byte[] header = new byte[12];
                int bytesRead = image.InputStream.Read(header, 0, header.Length);
                image.InputStream.Position = 0; 

                if (bytesRead < 4) return false;

              
                bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
              
                bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
             
                bool isWebp = bytesRead >= 12
                    && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

                return isJpeg || isPng || isWebp;
            }
            catch
            {
                return false;
            }
        }

    
        private void DeleteFoodImageFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            try
            {
                string fullPath = Server.MapPath("~" + relativePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                
            }
        }

  
        public ActionResult FoodManagement(string search = "", int? categoryId = null, int page = 1, int pageSize = 20)
        {
            int companyId = GetCurrentUserCompanyId();


            var activeCategoryIds = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .Select(c => c.ID)
                .ToList();

            var foods = _context.Foods
                .Where(f => f.CompanyID == companyId
                            && (!f.CategoryID.HasValue || activeCategoryIds.Contains(f.CategoryID.Value)));

     
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

   
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 200) pageSize = 200;

            int totalCount = foods.Count();
            int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var orderedFoods = foods
                .OrderBy(f => f.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            var categories = _context.FoodCategories
                .Where(c => c.CompanyID == companyId && c.IsActive)
                .ToDictionary(c => c.ID, c => c.Name);
            ViewBag.Categories = categories;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

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
                .OrderBy(c => c.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ToList();
            return PartialView("_AddFoodModal", new Food());
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFood(Food model, HttpPostedFileBase Image)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");
            ModelState.Remove("ImagePath");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

          
            string imagePath = SaveFoodImage(Image, out string imageError);
            if (imageError != null)
                return Json(new { success = false, message = imageError });

           
            model.Name = model.Name?.Trim();
            model.Allergens = string.IsNullOrWhiteSpace(model.Allergens) ? null : model.Allergens.Trim();
            model.Ingredients = string.IsNullOrWhiteSpace(model.Ingredients) ? null : model.Ingredients.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            model.ImagePath = imagePath;

            model.CompanyID = companyId;
            model.IsActive = true;
            model.CreatedDate = DateTime.Now;

            _context.Foods.Add(model);
            _context.SaveChanges();

       
            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Yemek Eklendi", model.ID);

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
                .OrderBy(c => c.DisplayOrder.HasValue ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ToList();

            return PartialView("_EditFoodModal", food);
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditFood(int id, Food model, HttpPostedFileBase Image)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ImagePath");

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

       
            string imagePath = SaveFoodImage(Image, out string imageError);
            if (imageError != null)
                return Json(new { success = false, message = imageError });

            bool removeImage = Request.Form["RemoveImage"] == "true";

            if (imagePath != null)
            {
                DeleteFoodImageFile(food.ImagePath);
                food.ImagePath = imagePath;
            }
            else if (removeImage)
            {
                DeleteFoodImageFile(food.ImagePath);
                food.ImagePath = null;
            }


            food.Name = model.Name?.Trim();
            food.CategoryID = model.CategoryID;
            food.Allergens = string.IsNullOrWhiteSpace(model.Allergens) ? null : model.Allergens.Trim();
            food.Ingredients = string.IsNullOrWhiteSpace(model.Ingredients) ? null : model.Ingredients.Trim();
            food.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

 
            bool isActive = Request.Form["IsActive"] != null && Request.Form["IsActive"] == "true";
            food.IsActive = isActive;

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Yemek Düzenlendi", food.ID);
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
            string status = food.IsActive ? "aktif" : "pasif";
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), $"Yemek {status} yapıldı", food.ID);
            _context.SaveChanges();

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

     
            var relatedMenuItemIds = _context.MenuItems
                .Where(m => m.FoodID == id)
                .Select(m => m.ID)
                .ToList();

            if (relatedMenuItemIds.Any())
            {
                bool hasSelection = _context.Selections.Any(s => relatedMenuItemIds.Contains(s.MenuItemID));
                if (hasSelection)
                {
                    return Json(new { success = false, message = "Bu yemek personel tarafından seçildiği için silinemez. Önce ilgili seçimlerin kaldırılması gerekir." });
                }

                return Json(new { success = false, message = "Bu yemek bir veya daha fazla menüye dahil edildiği için silinemez. Önce menülerden kaldırılması gerekir." });
            }

            food.IsActive = false;
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Yemek Silindi (Pasife Alındı)", food.ID);
            _context.SaveChanges();

            return Json(new { success = true, message = "Yemek başarıyla silindi (pasife alındı)." });
        }
    }
}
