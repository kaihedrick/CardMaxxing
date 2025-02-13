using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CardMaxxing.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;

        public UserController(IUserDataService userService, IOrderDataService orderService)
        {
            _userService = userService;
            _orderService = orderService;
        }

        // GET: User/Register - Show registration form
        public IActionResult Register()
        {
            return View();
        }

        // POST: User/Register - Register a new user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserModel user)
        {
            if (!ModelState.IsValid) return View(user);

            user.Role = "User"; // Ensure new users are assigned the default 'User' role

            bool exists = await _userService.CheckEmailDuplicateAsync(user.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(user);
            }

            bool result = await _userService.CreateUserAsync(user); // Hashing is handled inside this method
            if (result)
            {
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Error creating account.");
            return View(user);
        }


        // GET: User/Login - Show login form
        public IActionResult Login()
        {
            return View();
        }

        // POST: User/Login - Authenticate user (Using Claims & Cookies)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel credentials)
        {
            if (!ModelState.IsValid) return View(credentials);

            bool authenticated = await _userService.VerifyAccountAsync(credentials.Username, credentials.Password);
            UserModel user = await _userService.GetUserByUsernameAsync(credentials.Username);

            if (!authenticated || user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(credentials);
            }

            // Retrieve user role from database
            string role = user.Role ?? "User"; // Fallback to 'User' if Role is null

            // Create user claims for authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role) // Store actual role from DB
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction("Index", "Home");
        }

        // GET: User/Dashboard - Protected User Dashboard
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var user = await _userService.GetUserByIDAsync(userId);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        // GET: User/OrderHistory - View past orders (Protected)
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            List<OrderModel> orders = await _orderService.GetOrdersByUserIDAsync(userId);
            return View(orders);
        }

        // GET: Admin/Dashboard - Admin-only dashboard
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // GET: User/Logout - Logout User
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }


    }
}
