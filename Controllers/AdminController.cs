using CardMaxxing.Services;
using CardMaxxing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CardMaxxing.Controllers
{
    // Only admins can access this controller
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;

        public AdminController(IUserDataService userService, IOrderDataService orderService)
        {
            _userService = userService;
            _orderService = orderService;
        }

        // GET: Admin/Dashboard - Admin-only dashboard
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // ✅ View all orders from all users (Admin Only)
        public async Task<IActionResult> AllOrders()
        {
            List<OrderModel> allOrders = await _orderService.GetAllOrdersAsync();
            var ordersWithUsers = new List<(OrderModel, string, List<OrderItemsModel>, decimal)>();

            foreach (var order in allOrders)
            {
                var user = await _userService.GetUserByIDAsync(order.UserID);
                var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";

                var orderItems = await _orderService.GetOrderItemsByOrderIDAsync(order.ID);
                var totalPrice = await _orderService.GetOrderTotalAsync(order.ID);

                ordersWithUsers.Add((order, userName, orderItems, totalPrice));
            }

            return View(ordersWithUsers);
        }
    }
}
