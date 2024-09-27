using Microsoft.AspNetCore.Identity;
//using RazorWeb.Attributes;
using System.ComponentModel.DataAnnotations;
namespace AppMVC.Models
{

    public class AppUser : IdentityUser
    {
        // nếu muốn thêm các truòng dữ liệu cho bảng User 
        // thì thêm các thược tính cảu class này , sử dụng các annotaition
        // tạo thêm migrations và update 

        [DataType(DataType.Date)]
        //[MinAge(18, ErrorMessage = "You must be at least 18 years old.")]
        public DateTime? Bod { get; set; }

    }

}
