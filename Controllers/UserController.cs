using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

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
        public IActionResult Register(UserModel user)
        {
            if (!ModelState.IsValid) return View(user);

            // Hash password
            user.Password = HashPassword(user.Password ?? string.Empty);

            bool exists = _userService.checkEmailDuplicate(user.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(user);
            }

            bool result = _userService.createUser(user);
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

        // POST: User/Login - Authenticate user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel credentials)
        {
            if (!ModelState.IsValid) return View(credentials);

            bool authenticated = _userService.verifyAccount(credentials.Username, credentials.Password);
            UserModel? user = _userService.getUserByUsername(credentials.Username);

            if (!authenticated || user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(credentials);
            }

            // Store user session data (custom session handling)
            HttpContext.Session.SetString("UserId", user.ID);
            HttpContext.Session.SetString("Username", user.Username);

            // Sign in the user using cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.ID)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect to Dashboard after login
            return RedirectToAction("Index", "Home");
        }

        // GET: User/Dashboard - User Dashboard (After Login)
        [Authorize]
        public IActionResult Dashboard()
        {
            string? userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var user = _userService.getUserByID(userId);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        // GET: User/OrderHistory - View past orders
        [Authorize]
        public IActionResult OrderHistory()
        {
            string? userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            List<OrderModel> orders = _orderService.getOrdersByUserID(userId);
            return View(orders);
        }

        // GET: User/Edit/{id} - Show edit user form
        [Authorize]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            var user = _userService.getUserByID(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: User/Edit/{id} - Update user details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserModel user)
        {
            if (!ModelState.IsValid) return View(user);

            var existingUser = _userService.getUserByID(user.ID);
            if (existingUser == null) return NotFound();

            user.Password = HashPassword(user.Password ?? string.Empty);
            bool result = _userService.editUser(user);

            if (result) return RedirectToAction("Dashboard");

            ModelState.AddModelError("", "Error updating account.");
            return View(user);
        }

        // GET: User/Delete/{id} - Show delete confirmation page
        [Authorize]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            var user = _userService.getUserByID(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: User/Delete/{id} - Confirm user deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var user = _userService.getUserByID(id);
            if (user == null) return NotFound();

            bool result = _userService.deleteUser(id);
            if (result)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Register");
            }

            ModelState.AddModelError("", "Error deleting account.");
            return View(user);
        }

        // GET: User/Logout - Logout user
        // Ensure logout action signs  user out from cookie authentication system 
        public async Task<IActionResult> Logout()
        {
            // Clear session data
            HttpContext.Session.Clear();

            // Sign out using cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to Home (or Login) after logout
            return RedirectToAction("Index", "Home");
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
