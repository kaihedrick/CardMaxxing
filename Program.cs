using CardMaxxing.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // Add this import

var builder = WebApplication.CreateBuilder(args);

// Register Dependency Injection Services
builder.Services.AddScoped<IUserDataService, UserDAO>();
builder.Services.AddScoped<IProductDataService, ProductDAO>();
builder.Services.AddScoped<ICartDataService, CartDAO>();
builder.Services.AddScoped<IOrderDataService, OrderDAO>();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";  // Redirect to login page if not authenticated
        options.LogoutPath = "/User/Logout"; // Handle logout action
    });

// Add controllers with views
builder.Services.AddControllersWithViews();

var app = builder.Build(); // 🚨 Once this is called, services cannot be modified!

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();  // Add this line for authentication middleware
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
