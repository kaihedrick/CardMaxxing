using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace CardMaxxing.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductDataService _productService;

        // Initialize logger and product service.
        public HomeController(ILogger<HomeController> logger, IProductDataService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        // Display featured GPUs and the user's cart.
        public async Task<IActionResult> Index()
        {
            // Retrieve cart data from session; create an empty cart if not found.
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<OrderItemsModel>()
                : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();

            ViewBag.Cart = cart;

            // Fetch all products and select the top three GPUs.
            var allProducts = await _productService.GetAllProductsAsync();
            var featuredProducts = allProducts
                .Where(p => p.Name.Contains("RTX") || p.Name.Contains("RX"))
                .OrderByDescending(p => p.Price)
                .Take(3)
                .ToList();

            return View(featuredProducts);
        }

        // Show the privacy policy page.
        public IActionResult Privacy()
        {
            return View();
        }

        // Show dashboard for logged-in users.
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        // Display error details without caching.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
