using AppMVC.Models.Blog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppMVC.Models
{
    public class NotificationModel
    {
        [Key]
        public int Id { get; set; }

        public int PostId { set; get; }
        [ForeignKey("PostId")]
        public Post Post { set; get; }

        public string Message { get; set; }
    }
}
