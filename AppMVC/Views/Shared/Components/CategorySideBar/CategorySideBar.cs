using AppMVC.Models.Blog;
using Microsoft.AspNetCore.Mvc;

namespace AppMVC.Views.Shared.Components.CategorySideBar
{
    [ViewComponent]
    public class CategorySideBar : ViewComponent
    {
        public class CategorySideBarData
        {
            public List<Category> categories { get; set; }
            public int level { get; set; }
            public string categoryslug { get; set; }
        }

        public IViewComponentResult Invoke(CategorySideBarData data)
        {
            return View(data);
        }
    }
}
