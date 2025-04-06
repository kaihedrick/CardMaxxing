using CardMaxxing.Services;
using CardMaxxing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Security.Claims;

namespace CardMaxxing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;
        private readonly ILogger<AdminController> _logger;
        private readonly TelemetryClient _telemetryClient;

        // Initializes a new instance of the AdminController with required dependencies
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

        // Displays the main admin dashboard view with overview metrics
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

        // Retrieves and displays all orders in the system with user details and totals
        public async Task<IActionResult> AllOrders()
        {
            string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Admin {AdminId} viewing all orders", adminId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AdminAllOrders");
                
                List<OrderModel> allOrders = await _orderService.GetAllOrdersAsync();
                var ordersWithUsers = new List<(OrderModel, string, List<OrderItemsModel>, decimal)>();
                decimal totalRevenue = 0;

                foreach (var order in allOrders)
                {
                    var user = await _userService.GetUserByIDAsync(order.UserID);
                    var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";
                    
                    var orderItems = await _orderService.GetOrderItemsByOrderIDAsync(order.ID);
                    var totalPrice = await _orderService.GetOrderTotalAsync(order.ID);
                    totalRevenue += totalPrice;

                    ordersWithUsers.Add((order, userName, orderItems, totalPrice));
                }

                _logger.LogInformation("Retrieved {OrderCount} orders with total revenue ${TotalRevenue}", 
                    allOrders.Count, totalRevenue);
                
                _telemetryClient.TrackEvent("AdminOrdersViewed", new Dictionary<string, string>
                {
                    { "AdminId", adminId ?? "unknown" },
                    { "OrderCount", allOrders.Count.ToString() },
                    { "TotalRevenue", totalRevenue.ToString("F2") },
                    { "UniqueCustomers", ordersWithUsers.Select(o => o.Item1.UserID).Distinct().Count().ToString() }
                });

                return View(ordersWithUsers);
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
