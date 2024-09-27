using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AppMVC.Models.Blog
{
    [Table("Category")]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        [Required(ErrorMessage = "Phải có tên danh mục")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} dài {1} đến {2}")]
        [Display(Name = "Tên danh mục")]
        public string Title { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Nội dung danh mục")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Phải tạo url")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} dài {1} đến {2}")]
        [RegularExpression(@"^[a-z0-9-]*$", ErrorMessage = "Chỉ dùng các ký tự [a-z0-9-]")]
        [Display(Name = "Url hiện thị")]
        public string Slug { get; set; }

        // Các Category con, không cần yêu cầu khi tạo
        public ICollection<Category>? CategoryChildren { get; set; } = new List<Category>();

        [ForeignKey("ParentCategoryId")]
        [Display(Name = "Danh mục cha")]
        public Category? ParentCategory { get; set; }

        public void ChildCategoryIDs(ICollection<Category> childCates, List<int> lists)
        {
            if (childCates == null)
            {
                childCates = this.CategoryChildren;
            }

            foreach(var category in childCates)
            {
                lists.Add(category.Id);
                ChildCategoryIDs(category.CategoryChildren, lists);
            }
        }

        public List<Category> ListParents()
        {
            List<Category> categories = new List<Category>();
            var parent = this.ParentCategory;
            while (parent != null)
            {
                categories.Add(parent);
                parent = parent.ParentCategory;
            }
            categories.Reverse();
            return categories;
        }
    }

}
