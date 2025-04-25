using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Security.Claims;

namespace CardMaxxing.Controllers
{
    /***
 * @class HomeController
 * @description Handles core page routing such as home, privacy, error, and user dashboard.
 */

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductDataService _productService;
        private readonly TelemetryClient _telemetryClient;

/***
 * @constructor HomeController
 * @description Initializes HomeController with logging, product service, and telemetry.
 * @param {ILogger<HomeController>} logger - Logger instance for the controller.
 * @param {IProductDataService} productService - Service for retrieving product data.
 * @param {TelemetryClient} telemetryClient - Application Insights telemetry client.
 */
        public HomeController(
            ILogger<HomeController> logger, 
            IProductDataService productService,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _productService = productService;
            _telemetryClient = telemetryClient;
        }


/***
 * @method Index
 * @description Displays the home page with featured products and cart summary.
 * @returns {Task<IActionResult>} - Returns the home page view.
 */
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("User accessing home page");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("HomePage");
                
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

                _logger.LogInformation("Home page loaded with {FeaturedCount} featured products", featuredProducts.Count);
                _telemetryClient.TrackEvent("HomePageViewed", new Dictionary<string, string>
                {
                    { "FeaturedProductCount", featuredProducts.Count.ToString() },
                    { "CartItemCount", cart.Count.ToString() },
                    { "IsAuthenticated", User.Identity?.IsAuthenticated.ToString() ?? "false" }
                });
                
                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "HomePage" }
                });
                throw;
            }
        }

/***
 * @method Privacy
 * @description Displays the privacy policy page.
 * @returns {IActionResult} - Returns the privacy policy view.
 */
        public IActionResult Privacy()
        {
            _logger.LogInformation("User accessing privacy page");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("PrivacyPage");
                
                _telemetryClient.TrackEvent("PrivacyPageViewed", new Dictionary<string, string>
                {
                    { "IsAuthenticated", User.Identity?.IsAuthenticated.ToString() ?? "false" }
                });
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading privacy page");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "PrivacyPage" }
                });
                throw;
            }
        }


/***
 * @method Dashboard
 * @description Displays the dashboard page for authenticated users.
 * @returns {IActionResult} - Returns the user dashboard view.
 */
        [Authorize]
        public IActionResult Dashboard()
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} accessing dashboard from home controller", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("HomeDashboard");
                
                _telemetryClient.TrackEvent("HomeDashboardViewed", new Dictionary<string, string>
                {
                    { "UserId", userId ?? "unknown" }
                });
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "HomeDashboard" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method Error
 * @description Displays error page details when unhandled exceptions occur.
 * @returns {IActionResult} - Returns the error page view with request information.
 */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Application error occurred. RequestId: {RequestId}", requestId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ErrorPage");
                
                _telemetryClient.TrackEvent("ErrorPageViewed", new Dictionary<string, string>
                {
                    { "RequestId", requestId },
                    { "Path", HttpContext.Request.Path },
                    { "Method", HttpContext.Request.Method }
                });
                
                return View(new ErrorViewModel
                {
                    RequestId = requestId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rendering error page for RequestId: {RequestId}", requestId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ErrorPage" },
                    { "RequestId", requestId }
                });
                throw;
            }
        }

/***
 * @method About
 * @description Displays the About page describing the application or organization.
 * @returns {IActionResult} - Returns the About page view.
 */
        public IActionResult About()
        {
            _logger.LogInformation("User accessing About page");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AboutPage");
                
                _telemetryClient.TrackEvent("AboutPageViewed", new Dictionary<string, string>
                {
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" }
                });
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading About page");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AboutPage" }
                });
                throw;
            }
        }
    }
}
