using AppMVC.Models.Blog;
using AppMVC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AppMVC.Data;
using Microsoft.Extensions.Logging;

namespace AppMVC.Areas.Blog.Controllers
{
    [Area("Blog")]
    [Authorize(Roles = RoleName.Administrator + "," + RoleName.Editor)]
    [Route("admin/blog/category/{action}/{id?}")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AppDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //GET: Blog/Category
        public async Task<IActionResult> Index()
        {
            var allCategories = await _context.Categories
                .Include(c => c.CategoryChildren)
                .Include(c => c.ParentCategory)
                .ToListAsync();

            // Xây dựng cấu trúc phân cấp
            var categories = BuildCategoryTree(allCategories);

            return View(categories);
        }

        // Hàm để xây dựng cấu trúc phân cấp từ danh sách danh mục
        private IEnumerable<Category> BuildCategoryTree(IEnumerable<Category> categories)
        {
            var categoryDictionary = categories.ToDictionary(c => c.Id);
            var rootCategories = new List<Category>();

            foreach (var category in categories)
            {
                if (category.ParentCategoryId == null)
                {
                    rootCategories.Add(category);
                }
                else
                {
                    if (categoryDictionary.TryGetValue(category.ParentCategoryId.Value, out var parentCategory))
                    {
                        if (parentCategory.CategoryChildren == null)
                        {
                            parentCategory.CategoryChildren = new List<Category>();
                        }
                        if (!parentCategory.CategoryChildren.Contains(category))
                        {
                            parentCategory.CategoryChildren.Add(category);
                        }
                    }
                }
            }

            return rootCategories;
        }


        // GET: Blog/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Blog/Category/Create
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesForParentSelectList();
            return View();
        }

        // POST: Blog/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ParentCategoryId,Title,Content,Slug")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Nếu danh mục không có danh mục cha
                if (category.ParentCategoryId == -1)
                {
                    category.ParentCategoryId = null;
                }

                _context.Add(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            LogModelErrors();
            await LoadCategoriesForParentSelectList();
            return View(category);
        }


        // GET: Blog/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            await LoadCategoriesForParentSelectList();
            return View(category);
        }

        // POST: Blog/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ParentCategoryId,Title,Content,Slug")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (category.ParentCategoryId == category.Id)
            {
                ModelState.AddModelError(string.Empty, "Phải chọn danh mục cha khác chính nó");
            }

            // Kiểm tra nếu ParentCategoryId nằm trong nhánh con của chính nó
            if (category.ParentCategoryId.HasValue && await IsCircularReference(category.Id, category.ParentCategoryId.Value))
            {
                ModelState.AddModelError(string.Empty, "Không thể chọn danh mục cha là một danh mục con của chính nó.");
            }

            if (ModelState.IsValid && category.ParentCategoryId != category.Id)
            {
                try
                {
                    if (category.ParentCategoryId == -1)
                    {
                        category.ParentCategoryId = null;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesForParentSelectList();
            return View(category);
        }

        // GET: Blog/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Blog/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.CategoryChildren)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            foreach (var childCategory in category.CategoryChildren)
            {
                childCategory.ParentCategoryId = category.ParentCategoryId;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        // Helper method to load categories for dropdown
        private async Task LoadCategoriesForParentSelectList()
        {
            var allCategories = await _context.Categories
                .Include(c => c.CategoryChildren)
                .Include(c => c.ParentCategory)
                .ToListAsync();

            // Xây dựng cấu trúc phân cấp
            var categories = BuildCategoryTree(allCategories).ToList();

            categories.Insert(0, new Category()
            {
                Title = "Không có danh mục cha",
                Id = -1
            });

            var items = new List<Category>();
            CreateSelectItems(categories, items, 0);

            ViewData["ParentCategoryId"] = new SelectList(items, "Id", "Title");
        }

        // Recursively create select list items
        private void CreateSelectItems(List<Category> source, List<Category> des, int level)
        {
            string prefix = string.Concat(Enumerable.Repeat("--", level));

            foreach (var category in source)
            {
                des.Add(new Category()
                {
                    Id = category.Id,
                    Title = prefix + " " + category.Title,
                });

                if (category.CategoryChildren?.Count() > 0)
                {
                    CreateSelectItems(category.CategoryChildren.ToList(), des, level + 1);
                }
            }
        }

        // Log errors from model validation
        private void LogModelErrors()
        {
            foreach (var modelState in ModelState)
            {
                var key = modelState.Key;
                var value = modelState.Value;

                foreach (var error in value.Errors)
                {
                    _logger.LogError("Lỗi ở trường {Field}: {ErrorMessage}", key, error.ErrorMessage);
                }
            }
        }

        private async Task<bool> IsCircularReference(int categoryId, int parentCategoryId)
        {
            var parentCategory = await _context.Categories.FindAsync(parentCategoryId);

            while (parentCategory != null)
            {
                if (parentCategory.Id == categoryId)
                {
                    return true; 
                }

                if (!parentCategory.ParentCategoryId.HasValue)
                {
                    break;
                }

                parentCategory = await _context.Categories.FindAsync(parentCategory.ParentCategoryId);
            }

            return false; 
        }
    }
}
