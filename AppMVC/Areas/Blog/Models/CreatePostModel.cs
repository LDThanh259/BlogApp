using AppMVC.Models.Blog;
using System.ComponentModel.DataAnnotations;

namespace AppMVC.Areas.Blog.Models
{
    public class CreatePostModel : Post
    {
        [Display(Name ="Chuyên mục")]
        public int[] CategoryIds { get; set; }
    }
}
