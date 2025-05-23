﻿using System.Data;
using Dapper;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using MySql.Data.MySqlClient;


var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options => {
    options.EnableAdaptiveSampling = true;
    options.EnablePerformanceCounterCollectionModule = true;
});


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
