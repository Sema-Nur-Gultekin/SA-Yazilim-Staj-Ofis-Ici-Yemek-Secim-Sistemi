using ofis_ici_yemek_secim_sistemi.Models;
using ofis_ici_yemek_secim_sistemi.Services;
using System.Linq;
using System.Web.Mvc;

namespace ofis_ici_yemek_secim_sistemi.Controllers
{
    public class RecipeController : AdminController
    {
        [NonAction]
        public override ActionResult Index() => null;

        public ActionResult RecipeManagement()
        {
            int companyId = GetCurrentUserCompanyId();

            var recipes = _context.RecipeIngredients
                .Where(r => r.CompanyID == companyId)
                .OrderBy(r => r.FoodID)
                .ToList();

            var foodIds = recipes.Select(r => r.FoodID).Distinct().ToList();
            var stockIds = recipes.Select(r => r.StockItemID).Distinct().ToList();

            ViewBag.FoodNames = _context.Foods.Where(f => foodIds.Contains(f.ID)).ToDictionary(f => f.ID, f => f.Name);
            ViewBag.StockNames = _context.StockItems.Where(s => stockIds.Contains(s.ID)).ToDictionary(s => s.ID, s => s.Name);

            if (Request.IsAjaxRequest())
                return PartialView("_RecipeList", recipes);

            return View(recipes);
        }

        [HttpGet]
        public ActionResult AddRecipe()
        {
            int companyId = GetCurrentUserCompanyId();
            ViewBag.Foods = _context.Foods.Where(f => f.CompanyID == companyId && f.IsActive).OrderBy(f => f.Name).ToList();
            ViewBag.StockItems = _context.StockItems.Where(s => s.CompanyID == companyId && s.IsActive).OrderBy(s => s.Name).ToList();
            return PartialView("_AddRecipeModal", new RecipeIngredient());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRecipe(RecipeIngredient model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            if (model.RequiredQuantity <= 0)
            {
                return Json(new { success = false, message = "Gereken miktar sıfırdan büyük olmalıdır." });
            }


            var targetStockItem = _context.StockItems.FirstOrDefault(s => s.ID == model.StockItemID && s.CompanyID == companyId);
            if (targetStockItem == null)
            {
                return Json(new { success = false, message = "Seçilen stok kalemi bulunamadı." });
            }
            if (!UnitHelper.SameCategory(model.Unit, targetStockItem.Unit))
            {
                string recipeCat = UnitHelper.GetCategory(model.Unit) ?? "bilinmeyen";
                string stockCat = UnitHelper.GetCategory(targetStockItem.Unit) ?? "bilinmeyen";
                return Json(new { success = false, message = $"Birim uyuşmazlığı: reçete birimi '{model.Unit}' ({recipeCat}) ile '{targetStockItem.Name}' malzemesinin birimi '{targetStockItem.Unit}' ({stockCat}) farklı ölçü kategorilerinde. Aynı kategoriden bir birim seçin." });
            }


            bool duplicate = _context.RecipeIngredients.Any(r =>
                r.CompanyID == companyId && r.FoodID == model.FoodID && r.StockItemID == model.StockItemID);
            if (duplicate)
            {
                return Json(new { success = false, message = "Bu yemek için bu malzeme zaten reçetede tanımlı. Lütfen mevcut kaydı düzenleyin." });
            }

            model.CompanyID = companyId;
            _context.RecipeIngredients.Add(model);
            _context.SaveChanges();

            ActivityLogger.LogAndSave(_context, companyId, GetCurrentUserId(), "Reçete Eklendi", model.ID);

            return Json(new { success = true, message = "Reçete eklendi." });
        }

        [HttpGet]
        public ActionResult EditRecipe(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var recipe = _context.RecipeIngredients.FirstOrDefault(r => r.ID == id && r.CompanyID == companyId);
            if (recipe == null) return HttpNotFound();

            ViewBag.Foods = _context.Foods.Where(f => f.CompanyID == companyId && f.IsActive).OrderBy(f => f.Name).ToList();
            ViewBag.StockItems = _context.StockItems.Where(s => s.CompanyID == companyId && s.IsActive).OrderBy(s => s.Name).ToList();
            return PartialView("_EditRecipeModal", recipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRecipe(int id, RecipeIngredient model)
        {
            int companyId = GetCurrentUserCompanyId();
            ModelState.Remove("CompanyID");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            if (model.RequiredQuantity <= 0)
            {
                return Json(new { success = false, message = "Gereken miktar sıfırdan büyük olmalıdır." });
            }

            var recipe = _context.RecipeIngredients.FirstOrDefault(r => r.ID == id && r.CompanyID == companyId);
            if (recipe == null)
                return Json(new { success = false, message = "Reçete bulunamadı." });


            var targetStockItem = _context.StockItems.FirstOrDefault(s => s.ID == model.StockItemID && s.CompanyID == companyId);
            if (targetStockItem == null)
            {
                return Json(new { success = false, message = "Seçilen stok kalemi bulunamadı." });
            }
            if (!UnitHelper.SameCategory(model.Unit, targetStockItem.Unit))
            {
                string recipeCat = UnitHelper.GetCategory(model.Unit) ?? "bilinmeyen";
                string stockCat = UnitHelper.GetCategory(targetStockItem.Unit) ?? "bilinmeyen";
                return Json(new { success = false, message = $"Birim uyuşmazlığı: reçete birimi '{model.Unit}' ({recipeCat}) ile '{targetStockItem.Name}' malzemesinin birimi '{targetStockItem.Unit}' ({stockCat}) farklı ölçü kategorilerinde. Aynı kategoriden bir birim seçin." });
            }

   
            bool duplicate = _context.RecipeIngredients.Any(r =>
                r.CompanyID == companyId && r.ID != id && r.FoodID == model.FoodID && r.StockItemID == model.StockItemID);
            if (duplicate)
            {
                return Json(new { success = false, message = "Bu yemek için bu malzeme zaten başka bir reçete satırında tanımlı." });
            }

            recipe.FoodID = model.FoodID;
            recipe.StockItemID = model.StockItemID;
            recipe.RequiredQuantity = model.RequiredQuantity;
            recipe.Unit = model.Unit;

            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Reçete Düzenlendi", recipe.ID);
            _context.SaveChanges();
            return Json(new { success = true, message = "Reçete güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRecipe(int id)
        {
            int companyId = GetCurrentUserCompanyId();
            var recipe = _context.RecipeIngredients.FirstOrDefault(r => r.ID == id && r.CompanyID == companyId);
            if (recipe == null)
                return Json(new { success = false, message = "Reçete bulunamadı." });

            _context.RecipeIngredients.Remove(recipe);
            ActivityLogger.Log(_context, companyId, GetCurrentUserId(), "Reçete Silindi", id);
            _context.SaveChanges();

            return Json(new { success = true, message = "Reçete kaydı silindi." });
        }
    }
}
