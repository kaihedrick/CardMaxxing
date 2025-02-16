using CardMaxxing.Services;
using CardMaxxing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CardMaxxing.Controllers
{
    // AdminController: Manages features available only to administrators.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;

        // Constructor: Sets up services for user and order operations.
        public AdminController(IUserDataService userService, IOrderDataService orderService)
        {
            _userService = userService;
            _orderService = orderService;
        }

        // AdminDashboard Action: Returns the admin dashboard view.
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // AllOrders Action: Retrieves all orders with associated user details and total price.
        public async Task<IActionResult> AllOrders()
        {
            List<OrderModel> allOrders = await _orderService.GetAllOrdersAsync();
            var ordersWithUsers = new List<(OrderModel, string, List<OrderItemsModel>, decimal)>();

            foreach (var order in allOrders)
            {
                // Get the user info for the order.
                var user = await _userService.GetUserByIDAsync(order.UserID);
                var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";

                // Get items and total price for the order.
                var orderItems = await _orderService.GetOrderItemsByOrderIDAsync(order.ID);
                var totalPrice = await _orderService.GetOrderTotalAsync(order.ID);

                ordersWithUsers.Add((order, userName, orderItems, totalPrice));
            }

            return View(ordersWithUsers);
        }
    }
}
