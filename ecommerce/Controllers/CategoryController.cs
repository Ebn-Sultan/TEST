using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.Services;
using ecommerce.ViewModels.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Controllers
{
    public class CategoryController : BaseController
    {
        private readonly ICategoryService categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ICategoryService categoryService, IWebHostEnvironment web)
        {
            this.categoryService = categoryService;
            this._webHostEnvironment = web;
        }

        [HttpGet]
        public IActionResult GetAll(string? include = null)
        {
            List<Category> categories = categoryService.GetAll(include);
            return View(categories);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            Category category = categoryService.Get(id);
            if (category != null)
            {
                return View("Get", category);
            }
            return RedirectToAction("GetAll");
        }

        public IActionResult ShowProducts(Category category)
        {
            CategoryWithProducts CategoryVM = new CategoryWithProducts()
            {
                SelectedCategoryId = category.Id,
                Products = categoryService.GetAllProductsInCategory(category.Id),
                Categories = categoryService.GetAll()
            };
            return View(CategoryVM);
        }

        [HttpGet]
        public IActionResult Get(Func<Category, bool> where)
        {
            List<Category> categories = categoryService.Get(where);
            return View(categories);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert()
        {
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert(Category category)
        {
            if (category.image != null && category.image.Length > 0)
            {
                try
                {
                    string uploadpath = Path.Combine(_webHostEnvironment.WebRootPath, "img");
                    string imagename = Guid.NewGuid().ToString() + "_" + category.image.FileName;
                    string filepath = Path.Combine(uploadpath, imagename);
                    using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
                    {
                        category.image.CopyTo(fileStream);
                    }
                    category.ImageUrl = imagename;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to upload image: " + ex.Message);
                    return View(category);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please upload an image.");
                return View(category);
            }

            if (ModelState.IsValid)
            {
                categoryService.Insert(category);
                categoryService.Save();
                return RedirectToAction("categories", "dashbourd");
            }
            return View(category);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            var category = categoryService.Get(id, "Products");
            if (category != null)
            {
                return View(category);
            }

            return RedirectToAction("categories", "dashbourd");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Category category)
        {
            var originalCategory = categoryService.Get(category.Id);
            if (originalCategory == null)
            {
                ModelState.AddModelError("", "Category not found.");
                return View(category);
            }

            if (category.image != null && category.image.Length > 0)
            {
                try
                {
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string imageName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(category.image.FileName);
                    string filePath = Path.Combine(uploadPath, imageName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        category.image.CopyTo(fileStream);
                    }

                    category.ImageUrl = "~/images/" + imageName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Image upload failed: " + ex.Message);
                    return View(category);
                }
            }
            else
            {
                category.ImageUrl = originalCategory.ImageUrl; // احتفظ بالصورة القديمة
            }

            if (ModelState.IsValid)
            {
                categoryService.Update(category);
                categoryService.Save();
                return RedirectToAction("categories", "dashbourd");
            }

            return View(category);
        }

        //[HttpGet]
        //[Authorize(Roles = "Admin")]
        //public IActionResult Update(int id)
        //{
        //    Category category = categoryService.Get(id, "Products");
        //    if (category != null)
        //    {
        //        return View(category);
        //    }
        //    return RedirectToAction("categories", "dashbourd");
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public IActionResult Update(Category category)
        //{
        //    var originalCategory = categoryService.Get(category.Id);
        //    if (originalCategory == null)
        //    {
        //        ModelState.AddModelError("", "Category not found.");
        //        return View(category);
        //    }

        //    // إذا لم يتم رفع صورة جديدة، احتفظ بالصورة القديمة
        //    if (category.image != null && category.image.Length > 0)
        //    {
        //        try
        //        {
        //            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
        //            string imageName = Guid.NewGuid().ToString() + "_" + category.image.FileName;
        //            string filePath = Path.Combine(uploadPath, imageName);

        //            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        //            {
        //                category.image.CopyTo(fileStream);
        //            }

        //            category.ImageUrl = "~/images/" + imageName; // حدد المسار النسبي للصورة
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", "Failed to upload image: " + ex.Message);
        //            return View(category);
        //        }
        //    }
        //    else
        //    {
        //        // إذا لم يتم رفع صورة جديدة، استخدم الصورة القديمة
        //        category.ImageUrl = originalCategory.ImageUrl;
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        categoryService.Update(category);
        //        categoryService.Save();
        //        return RedirectToAction("categories", "dashbourd");
        //    }

        //    return View(category);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public IActionResult Update(Category category)
        //{
        //    if (category.image != null && category.image.Length > 0)
        //    {
        //        try
        //        {
        //            string uploadpath = Path.Combine(_webHostEnvironment.WebRootPath, "img");
        //            string imagename = Guid.NewGuid().ToString() + "_" + category.image.FileName;
        //            string filepath = Path.Combine(uploadpath, imagename);
        //            using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
        //            {
        //                category.image.CopyTo(fileStream);
        //            }
        //            category.ImageUrl = imagename;
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", "Failed to upload image: " + ex.Message);
        //            return View(category);
        //        }
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        categoryService.Update(category);
        //        categoryService.Save();
        //        return RedirectToAction("categories", "dashbourd");
        //    }
        //    return View(category);
        //}

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Category category = categoryService.Get(id);
            if (category != null)
            {
                return View(category);
            }
            return RedirectToAction("categories", "Dashbourd");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Category category)
        {
            try
            {
                categoryService.Delete(category);
                categoryService.Save();
                return RedirectToAction("categories", "Dashbourd");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to delete category: " + ex.Message);
                return View(category);
            }
        }
    }
}