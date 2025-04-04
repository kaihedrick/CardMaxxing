using System.Data; // Add this import
using Dapper;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using MySql.Data.MySqlClient;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Loggly
var logglyToken = builder.Configuration["Loggly:CustomerToken"];
if (string.IsNullOrEmpty(logglyToken))
{
    throw new InvalidOperationException("Loggly customer token is missing! Check appsettings.json.");
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Loggly(
        customerToken: logglyToken,
        tags: "CardMaxxing,Production"
    )
    .CreateLogger();

// Add Serilog to the application
builder.Host.UseSerilog();

// Load Connection String Safely
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing! Check appsettings.json.");
}

// Register MySQL Connection for Dapper (Transient Scope)
builder.Services.AddTransient<IDbConnection>(sp => new MySqlConnection(connectionString));

// Register Dependency Injection Services
builder.Services.AddScoped<IUserDataService, UserDataService>();
builder.Services.AddScoped<IProductDataService, ProductDataService>();
builder.Services.AddScoped<ICartDataService, CartDataService>();
builder.Services.AddScoped<IOrderDataService, OrderDataService>();

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

var app = builder.Build(); // 🚨 Services can’t be modified after this!

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Ensure Middleware Order is Correct
app.UseAuthentication();
app.UseSession(); // ⚡ Session MUST be before Authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
