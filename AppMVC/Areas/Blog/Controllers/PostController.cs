using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppMVC.Areas.Blog.Models;
using AppMVC.Areas.Identity.Models.UserViewModels;
using AppMVC.Data;
using AppMVC.Models;
using AppMVC.Models.Blog;
using AppMVC.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;

namespace AppMVC.Areas.Blog.Controllers
{
    [Area("Blog")]
    [Route("admin/blog/post/{action}/{id?}")]
    //[Authorize(Roles = RoleName.Administrator + "," + RoleName.Editor)]
    [Authorize]
    public class PostController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserManager<AppUser> _userManager;
        private readonly IAuthorizationService _authorizationService;

        [TempData]
        public string StatusMessage { get; set; }

        public PostController(AppDbContext context, UserManager<AppUser> userManager, IAuthorizationService authorizationService)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }

        // GET: Blog/Post
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, int pagesize)
        {
            IOrderedQueryable<Post> posts = null;

            posts = _context.Posts
                .Include(p => p.Author)
                .OrderByDescending(p => p.DateUpdated);

            var user = await _userManager.GetUserAsync(this.User);
            if (user != null)
            {
                posts = _context.Posts
                        .Where(p => p.Author == user)
                        .Include(p => p.Author)
                        .OrderByDescending(p => p.DateUpdated);

                var role = await _userManager.GetRolesAsync(user);
                if (role.Contains(RoleName.Administrator))
                {
                    posts = _context.Posts
                    .Include(p => p.Author)
                    .OrderByDescending(p => p.DateUpdated);

                }
            }

            int totalPosts = await posts.CountAsync();

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

            ViewBag.postIndex = (currentPage - 1) * pagesize;

            var postsInPage = await posts.Skip((currentPage - 1) * pagesize)
                            .Take(pagesize)
                            .Include(p => p.PostCategories)
                            .ThenInclude(pc => pc.Category)
                            .ToListAsync();


            return View(postsInPage);

        }

        // GET: Blog/Post/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Blog/Post/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            return View();
        }

        // POST: Blog/Post/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Slug,Content,Published,CategoryIds")] CreatePostModel post)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");


            if (post.Slug == null)
            {
                post.Slug = AppUtilities.GenerateSlug(post.Title);
            }
            if (await _context.Posts.AnyAsync(p => p.Slug == post.Slug))
            {
                ModelState.AddModelError("Slug", "Slug da ton tai");
                return View(post);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(this.User);
                post.DateCreated = post.DateUpdated = DateTime.Now;
                post.AuthorId = user.Id;


                if (post.CategoryIds != null)
                {
                    foreach (var cateId in post.CategoryIds)
                    {
                        _context.Add(new PostCategory()
                        {
                            CategoryID = cateId,
                            Post = post,
                        });
                    }
                }

                _context.Add(post);
                await _context.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id", post.AuthorId);
            return View(post);
        }

        // GET: Blog/Post/Edit/5
        //[Authorize(Policy = "AuthorPolicy")]  //khong dung attrubute vì nó k truyền được postid nếu muon dung phai tạo attribute mới
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.Include(p => p.PostCategories).FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            // Gọi dịch vụ ủy quyền để kiểm tra quyền của người dùng
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, id, "AuthorPolicy");

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            var postEdit = new CreatePostModel()
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Description = post.Description,
                Slug = post.Slug,
                Published = post.Published,
                CategoryIds = post.PostCategories.Select(pc => pc.CategoryID).ToArray()
            };

            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");
            return View(postEdit);
        }

        // POST: Blog/Post/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[Authorize(Policy = "AuthorPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Title,Description,Slug,Content,Published,CategoryIds")] CreatePostModel post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }

            // Gọi dịch vụ ủy quyền để kiểm tra quyền của người dùng
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, id, "AuthorPolicy");

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            var categories = await _context.Categories.ToListAsync();
            ViewData["categories"] = new MultiSelectList(categories, "Id", "Title");

            if (post.Slug == null)
            {
                post.Slug = AppUtilities.GenerateSlug(post.Title);
            }
            if (await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.PostId != post.PostId))
            {
                ModelState.AddModelError("Slug", "Slug already exists.");
                return View(post);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var postUpdate = await _context.Posts
                        .Include(p => p.PostCategories)
                        .FirstOrDefaultAsync(p => p.PostId == id);
                    if (postUpdate == null)
                    {
                        return NotFound();
                    }

                    // Update post details
                    postUpdate.Title = post.Title;
                    postUpdate.Description = post.Description;
                    postUpdate.Content = post.Content;
                    postUpdate.Published = post.Published;
                    postUpdate.Slug = post.Slug;
                    postUpdate.DateUpdated = DateTime.Now;

                    // Update categories
                    if (post.CategoryIds == null) post.CategoryIds = Array.Empty<int>();

                    var oldCateIds = postUpdate.PostCategories.Select(c => c.CategoryID).ToArray();
                    var newCateIds = post.CategoryIds;

                    var removeCatePosts = postUpdate.PostCategories
                        .Where(pc => !newCateIds.Contains(pc.CategoryID))
                        .ToList();

                    _context.PostCategories.RemoveRange(removeCatePosts);

                    var addCateIds = newCateIds
                        .Where(cateId => !oldCateIds.Contains(cateId))
                        .ToList();

                    foreach (var cateId in addCateIds)
                    {
                        postUpdate.PostCategories.Add(new PostCategory()
                        {
                            PostID = id,
                            CategoryID = cateId
                        });
                    }

                    // Update post in the database
                    _context.Update(postUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
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

            // Prepare view data for categories and author
            ViewData["AuthorId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id", post.AuthorId);
            return View(post);
        }


        // GET: Blog/Post/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền của người dùng
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, post, "AuthorOrEditorPolicy");

            if (!authorizationResult.Succeeded)
            {
                return Forbid(); // Trả về 403 nếu người dùng không có quyền
            }

            return View(post);
        }

        // POST: Blog/Post/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post != null)
            {
                // Kiểm tra quyền của người dùng
                var authorizationResult = await _authorizationService.AuthorizeAsync(User, post, "AuthorOrEditorPolicy");

                if (!authorizationResult.Succeeded)
                {
                    return Forbid(); // Trả về 403 nếu người dùng không có quyền
                }

                _context.Posts.Remove(post);
            }

            StatusMessage = "Delete succesfully";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }
    }
}
