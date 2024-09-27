using AppMVC.Data;
using AppMVC.Models;
using AppMVC.Requirement;
using AppMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        builder.Services.AddLogging();

        builder.Services.AddOptions();
        var mailsetting = builder.Configuration.GetSection("MailSettings");
        builder.Services.Configure<MailSettings>(mailsetting);
        builder.Services.AddSingleton<IEmailSender, SendMailService>();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString("AppMVC");
            options.UseSqlServer(connectionString);
        });

        builder.Services.AddIdentity<AppUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Truy cập IdentityOptions
        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Thiết lập về Password
            options.Password.RequireDigit = false; // Không bắt phải có số
            options.Password.RequireLowercase = false; // Không bắt phải có chữ thường
            options.Password.RequireNonAlphanumeric = false; // Không bắt ký tự đặc biệt
            options.Password.RequireUppercase = false; // Không bắt buộc chữ in
            options.Password.RequiredLength = 3; // Số ký tự tối thiểu của password
            options.Password.RequiredUniqueChars = 1; // Số ký tự riêng biệt

            // Cấu hình Lockout - khóa user
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Khóa 5 phút
            options.Lockout.MaxFailedAccessAttempts = 3; // Thất bại 5 lầ thì khóa
            options.Lockout.AllowedForNewUsers = true;

            // Cấu hình về User.
            options.User.AllowedUserNameCharacters = // các ký tự đặt tên user
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;  // Email là duy nhất

            // Cấu hình đăng nhập.
            options.SignIn.RequireConfirmedEmail = true;            // Cấu hình xác thực địa chỉ email (email phải tồn tại)
            options.SignIn.RequireConfirmedPhoneNumber = false;     // Xác thực số điện thoại
            options.SignIn.RequireConfirmedAccount = true;
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/noaccess";

        });

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                var ggconfig = builder.Configuration.GetSection("Authentication:Google");
                options.ClientId = ggconfig["client_id"];
                options.ClientSecret = ggconfig["client_secret"];
                // Cấu hình Url callback lại từ Google (không thiết lập thì mặc định là /signin-google)
                options.CallbackPath = "/googlelogin";
            })
            .AddFacebook(facebookOptions =>
            {
                // Đọc cấu hình
                var fbconfig = builder.Configuration.GetSection("Authentication:Facebook");
                facebookOptions.AppId = fbconfig["AppId"];
                facebookOptions.AppSecret = fbconfig["AppSecret"];
                // Thiết lập đường dẫn Facebook chuyển hướng đến
                facebookOptions.CallbackPath = "/facebooklogin";
            });

        builder.Services.AddSingleton<IdentityErrorDescriber, AppIdentityErrorDescriber>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", builder =>
            {
                builder.RequireAuthenticatedUser();
                builder.RequireRole(RoleName.Administrator);
            });

            options.AddPolicy("EditorPolicy", builder =>
            {
                builder.RequireAuthenticatedUser();
                builder.RequireRole(RoleName.Editor);
            });

            options.AddPolicy("MemberPolicy", builder =>
            {
                builder.RequireAuthenticatedUser();
                builder.RequireRole(RoleName.Member);
            });

            options.AddPolicy("AuthorPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new IsPostAuthorRequirement());
            });

            options.AddPolicy("AuthorOrEditorPolicy", policy =>
                policy.Requirements.Add(new DeletePostRequirement())
            );
        });

        builder.Services.AddScoped<IAuthorizationHandler, IsPostAuthorHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, DeletePostAuthorizationHandler>();


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "Uploads")),
            RequestPath = "/contents"
        });

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {

            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            endpoints.MapRazorPages();
        });

        app.Run();
    }
}