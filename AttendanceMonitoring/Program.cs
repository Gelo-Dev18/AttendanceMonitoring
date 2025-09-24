using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
//para ioverride yung default ng asp.net kapag nag back sa browser after logout
builder.Services.ConfigureApplicationCookie(options => 
{
    options.LoginPath = "/Login/Login"; //eto yung path
});

builder.Services.AddRazorPages()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");

//
//builder.Services.ConfigureApplicationCookie(options =>
//{
//    options.ExpireTimeSpan = TimeSpan.FromMinutes(1); //time to expire when use is inactive
//    options.SlidingExpiration = true; // resets the time on activity
//    options.LoginPath = "/Login/Login";// path to go when cookie expires
//});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

//if using razor pages for default instead of using mvc pattern
//app.MapGet("/", context =>
//{
//    context.Response.Redirect("/Identity/Account/Login");
//    return Task.CompletedTask;
//});
app.MapRazorPages();



using (var scope = app.Services.CreateScope()) //gives you a fresh room for a task, and when you're done, it cleans everything up.
{
                                                              //Accessing and managing roles.//Service from dependecyinjection
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); //you can create, delete, or manage roles in your asp.net core system

    var roles = new[] { "Admin", "Teacher", "Secretary" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role)) // check if may existing na role
            await roleManager.CreateAsync(new IdentityRole(role)); // create ng role if wala
    }

}

//using (var scope = app.Services.CreateScope())
////Si RoleManager is initial setup to seed Admin, TEacher and secretary // Si UserManager to create user and assign them into a role
//{   //👉 Used to create, delete, and manage users — including assigning roles, changing passwords, and retrieving user info
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

//    //string email = "admin@admin.com";
//    //string password = "admin12345";

//    string email = "example@email.com";
//    string password = "example12345";



//    ////if (await userManager.FindByEmailAsync(email) == null)
//    ////{
//    ////    var user = new AppUser();

//    ////    user.UserName = email;
//    ////    user.Email = email;

//    ////    await userManager.CreateAsync(user, password);
//    ////    await userManager.AddToRoleAsync(user, "Admin");

//    ////}

//    //if (await userManager.FindByEmailAsync(email) == null)
//    //{
//    //    var user = new AppUser
//    //    {
//    //        UserName = email,
//    //        Email = email,
//    //    };


//    //    var result = await userManager.CreateAsync(user, password);

//    //    if (result.Succeeded)
//    //    {
//    //        await userManager.AddToRoleAsync(user, "Teacher");
//    //        Console.WriteLine($"Success");

//    //    }
//    //    else
//    //    {
//    //        foreach (var error in result.Errors)
//    //        {
//    //            Console.WriteLine($"User Creation failed: {error.Description}");
//    //        }
//    //    }
//    //}


//}


app.Run();
