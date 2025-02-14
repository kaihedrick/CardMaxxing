using CardMaxxing.Services;
using CardMaxxing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        // View all user orders (Admin Only)
        public async Task<IActionResult> AllOrders()
        {
            List<OrderModel> allOrders = await _orderService.GetAllOrdersAsync();
            return View(allOrders);
        }

        // View all users and manage roles (Admin Only)
        public async Task<IActionResult> ManageUsers()
        {
            List<UserModel> users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // Promote a user to admin role (Admin Only)
        [HttpPost]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            bool success = await _userService.UpdateUserRoleAsync(userId, "Admin");

            if (!success)
            {
                TempData["Error"] = "Failed to update user role.";
            }
            else
            {
                TempData["Success"] = "User promoted to admin.";
            }

            return RedirectToAction("ManageUsers");
        }

        // Demote an admin back to user role (Admin Only)
        [HttpPost]
        public async Task<IActionResult> DemoteToUser(string userId)
        {
            bool success = await _userService.UpdateUserRoleAsync(userId, "User");

            if (!success)
            {
                TempData["Error"] = "Failed to update user role.";
            }
            else
            {
                TempData["Success"] = "Admin demoted to user.";
            }

            return RedirectToAction("ManageUsers");
        }
    }
}
