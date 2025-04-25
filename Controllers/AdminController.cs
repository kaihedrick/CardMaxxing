using CardMaxxing.Services;
using CardMaxxing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Security.Claims;

namespace CardMaxxing.Controllers
{
    /***
 * @class AdminController
 * @description Handles administrative actions including dashboard access and viewing all orders.
 */
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;
        private readonly ILogger<AdminController> _logger;
        private readonly TelemetryClient _telemetryClient;

/***
 * @constructor AdminController
 * @description Initializes AdminController with services for user management, order management, logging, and telemetry.
 * @param {IUserDataService} userService - Service for user-related data operations.
 * @param {IOrderDataService} orderService - Service for order-related data operations.
 * @param {ILogger<AdminController>} logger - Logger instance for diagnostic and operational logging.
 * @param {TelemetryClient} telemetryClient - Application Insights telemetry client for monitoring and logging.
 */
        public AdminController(
            IUserDataService userService, 
            IOrderDataService orderService,
            ILogger<AdminController> logger,
            TelemetryClient telemetryClient)
        {
            _userService = userService;
            _orderService = orderService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

/***
 * @method AdminDashboard
 * @description Displays the main administrative dashboard with basic analytics and operational access.
 * @returns {IActionResult} - Returns the AdminDashboard view.
 */
        public IActionResult AdminDashboard()
        {
            string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Admin {AdminId} accessing dashboard", adminId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AdminDashboard");
                
                _telemetryClient.TrackEvent("AdminDashboardViewed", new Dictionary<string, string>
                {
                    { "AdminId", adminId ?? "unknown" }
                });
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing admin dashboard for admin {AdminId}", adminId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AdminDashboard" },
                    { "AdminId", adminId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method AllOrders
 * @description Retrieves and displays all orders with related user details and calculates total revenue metrics.
 * @returns {Task<IActionResult>} - Returns the AllOrders view populated with detailed order information.
 */
        public async Task<IActionResult> AllOrders()
        {
            string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Admin {AdminId} viewing all orders", adminId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AdminAllOrders");
                
                // Get all orders with details in a single operation instead of multiple queries
                var ordersWithDetails = await _orderService.GetAllOrdersWithDetailsAsync();
                
                // Calculate total revenue
                // The fourth item (Item4) in the tuple is the total price
                decimal totalRevenue = ordersWithDetails.Sum(o => o.Item4);
                
                _logger.LogInformation("Retrieved {OrderCount} orders with total revenue ${TotalRevenue}", 
                    ordersWithDetails.Count, totalRevenue);
                
                _telemetryClient.TrackEvent("AdminOrdersViewed", new Dictionary<string, string>
                {
                    { "AdminId", adminId ?? "unknown" },
                    { "OrderCount", ordersWithDetails.Count.ToString() },
                    { "TotalRevenue", totalRevenue.ToString("F2") },
                    { "UniqueCustomers", ordersWithDetails.Select(o => o.Item1.UserID).Distinct().Count().ToString() }
                });

                return View(ordersWithDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders for admin {AdminId}", adminId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AdminAllOrders" },
                    { "AdminId", adminId ?? "unknown" }
                });
                throw;
            }
        }
    }
}
