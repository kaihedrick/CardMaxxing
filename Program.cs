using CardMaxxing.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Dependency Injection Services BEFORE `Build()`
builder.Services.AddScoped<IUserDataService, UserDAO>();
builder.Services.AddScoped<IProductDataService, ProductDAO>();
builder.Services.AddScoped<ICartDataService, CartDAO>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build(); // 🚨 Once this is called, services cannot be modified!

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
