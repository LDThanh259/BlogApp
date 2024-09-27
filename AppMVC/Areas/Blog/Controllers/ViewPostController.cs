using AppMVC.Models;
using AppMVC.Models.Blog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppMVC.Areas.Blog.Controllers
{
    [Area("Blog")]
    public class ViewPostController : Controller
    {

        private readonly ILogger<ViewPostController> _logger;

        private readonly AppDbContext _appDbContext;

        public ViewPostController(ILogger<ViewPostController> logger, AppDbContext appDbContext)
        {
            _logger = logger;
            _appDbContext = appDbContext;
        }

        [Route("post/{categoryslug?}")]
        public IActionResult Index(string categoryslug, [FromQuery(Name ="p")]int currentPage, int pagesize)
        {
            var categories = GetCategory();
            ViewBag.Categories = categories;
            ViewBag.categoryslug = categoryslug;

            Category category = null;

            if (!string.IsNullOrEmpty(categoryslug))
            {
                category = _appDbContext.Categories
                    .Include(c => c.CategoryChildren)
                    .FirstOrDefault(c => c.Slug == categoryslug);

                if (category == null)
                {
                    return NotFound();
                }
            }

            var posts = _appDbContext.Posts
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(categoryslug))
            {
                posts = posts.Where(p => p.PostCategories.Any(pc => pc.Category.Slug == categoryslug));
            }

            posts = posts.OrderByDescending(p => p.DateUpdated);

            if (category != null) 
            {
                var ids = new List<int>();
                category.ChildCategoryIDs(null, ids);

                ids.Add(category.Id);

                posts = posts.Where(p => p.PostCategories.Any(pc => ids.Contains(pc.CategoryID)));
            }

            int totalPosts = posts.Count();

            if (pagesize <= 0) pagesize = 10;
            int countPages = (int)Math.Ceiling((double)totalPosts / pagesize);

            if (currentPage > countPages)
                currentPage = countPages;
            if (currentPage < 1)
                currentPage = 1;
            var pagingModel = new PagingModel()
            {
                countpages = countPages,
                currentpage = currentPage,
                generateUrl = (pageNumber) => Url.Action("Index", new
                {
                    p = pageNumber,
                    pagesize = pagesize
                })
            };

            ViewBag.pagingModel = pagingModel;
            ViewBag.totalPosts = totalPosts;

            var postsInPage = posts.Skip((currentPage - 1) * pagesize)
                            .Take(pagesize);

            //ViewBag.Posts = posts.ToList(); 
            ViewBag.category = category;
            return View(postsInPage.ToList());
        }


        [Route("post/{postslug}.html")]
        public IActionResult Details(string postslug)
        {
            var categories = GetCategory();
            ViewBag.Categories = categories;

            var post = _appDbContext.Posts.Where(p => p.Slug == postslug.Replace(".html", ""))
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
                .FirstOrDefault();

            if(post == null)
            {
                return NotFound();
            }
            Category category = post.PostCategories.FirstOrDefault()?.Category;
            ViewBag.category = category;

            var otherPosts = _appDbContext.Posts.Where(p => p.PostCategories.Any(c => c.Category.Id == category.Id))
                .Where(p => p.PostId != post.PostId)
                .OrderByDescending(p => p.DateUpdated)
                .Take(5);
            ViewBag.otherPosts = otherPosts;

            return View(post);
        }

        public List<Category> GetCategory()
        {
            var categories = _appDbContext.Categories
                             .Include(c => c.CategoryChildren)
                             .AsEnumerable()
                             .Where(c => c.ParentCategory == null)
                             .ToList();
            return categories;
        }
    }
}
