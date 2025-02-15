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

        public HomeController(ILogger<HomeController> logger, IProductDataService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        //this will return the view for the highest tier most trending graphics cards
        public async Task<IActionResult> Index()
        {
            // Retrieve the user's cart from session
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<OrderItemsModel>()
                : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();

            ViewBag.Cart = cart;  // ✅ Pass cart to the view

            // Fetch all products from the database
            var allProducts = await _productService.GetAllProductsAsync();

            // Filter for top-tier GPUs (adjust logic based on real product data)
            var featuredProducts = allProducts
                .Where(p => p.Name.Contains("RTX") || p.Name.Contains("RX")) // Select trending models
                .OrderByDescending(p => p.Price) // Prioritize most expensive GPUs
                .Take(3) // Limit to 3 Featured GPUs
                .ToList();

            return View(featuredProducts); // ✅ Now includes cart & featured GPUs
        }


        public IActionResult Privacy()
        {
            return View();
        }

        // Logged-In User Dashboard View
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
