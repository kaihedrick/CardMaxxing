using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace CardMaxxing.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;
        private readonly ICartDataService _cartService;
        private readonly IProductDataService _productService;

        public UserController(IUserDataService userService, IOrderDataService orderService, ICartDataService cartService, IProductDataService productService)
        {
            _userService = userService;
            _orderService = orderService;
            _cartService = cartService;
            _productService = productService;
        }

        // Render the registration form.
        public IActionResult Register()
        {
            return View();
        }

        // Process registration form submission.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserModel user)
        {
            if (!ModelState.IsValid) return View(user);

            bool exists = await _userService.CheckEmailDuplicateAsync(user.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(user);
            }

            bool result = await _userService.CreateUserAsync(user);
            if (result) return RedirectToAction("Login");

            ModelState.AddModelError("", "Error creating account.");
            return View(user);
        }

        // Render the login form.
        public IActionResult Login()
        {
            return View();
        }

        // Process login and set up authentication.
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

            string role = user.Role ?? "User";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return role == "Admin" ? RedirectToAction("AdminDashboard", "Admin") : RedirectToAction("Index", "Home");
        }

        // Show the user's dashboard.
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var user = await _userService.GetUserByIDAsync(userId);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        // Display the user's order history.
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            List<(OrderModel, List<OrderItemsModel>, decimal)> orderDetails = await _orderService.GetOrdersWithDetailsByUserIDAsync(userId);
            return View(orderDetails);
        }

        // Display the shopping cart with product details.
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ShoppingCart()
        {
            var cart = GetCartFromSession();
            for (int i = 0; i < cart.Count; i++)
            {
                var product = await _productService.GetProductByIDAsync(cart[i].ProductID);
                if (product != null)
                {
                    cart[i].Product = product;
                }
            }
            SaveCartToSession(cart);
            return View(cart);
        }

        // Update the shopping cart by adding or removing a product.
        [HttpPost]
        [Authorize]
        public IActionResult UpdateCart([FromBody] Dictionary<string, string> request)
        {
            if (!request.ContainsKey("productId") || !request.ContainsKey("action"))
            {
                return BadRequest(new { error = "Invalid request data" });
            }

            string productId = request["productId"];
            string action = request["action"];
            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(item => item.ProductID == productId);

            if (action == "add")
            {
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    var product = _productService.GetProductByIDAsync(productId).Result;
                    if (product != null)
                    {
                        existingItem = new OrderItemsModel
                        {
                            ProductID = product.ID,
                            Quantity = 1,
                            Product = product
                        };
                        cart.Add(existingItem);
                    }
                }
            }
            else if (action == "remove" && existingItem != null)
            {
                existingItem.Quantity--;
                if (existingItem.Quantity <= 0)
                {
                    cart.Remove(existingItem);
                    existingItem = null;
                }
            }
            SaveCartToSession(cart);
            int updatedQuantity = existingItem?.Quantity ?? 0;
            return Json(new { quantity = updatedQuantity });
        }

        // Add a product to the shopping cart.
        [HttpPost]
        [Authorize]
        public IActionResult AddToCart([FromBody] Dictionary<string, string> request)
        {
            if (!request.ContainsKey("productId"))
            {
                return BadRequest(new { error = "Invalid request data" });
            }

            string productId = request["productId"];
            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(item => item.ProductID == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                var product = _productService.GetProductByIDAsync(productId).Result;
                if (product != null)
                {
                    cart.Add(new OrderItemsModel
                    {
                        ProductID = product.ID,
                        Quantity = 1,
                        Product = product
                    });
                }
            }
            SaveCartToSession(cart);
            int updatedQuantity = existingItem?.Quantity ?? 1;
            return Json(new { quantity = updatedQuantity });
        }

        // Retrieve the shopping cart from the session.
        private List<OrderItemsModel> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<OrderItemsModel>();
            }
            return JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();
        }

        // Save the shopping cart to the session.
        private void SaveCartToSession(List<OrderItemsModel> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        // Remove or decrement a product from the shopping cart.
        [HttpPost]
        [Authorize]
        public IActionResult RemoveFromCart(string productId)
        {
            var cart = GetCartFromSession();
            var item = cart.Find(x => x.ProductID == productId);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    cart.Remove(item);
            }
            SaveCartToSession(cart);
            return RedirectToAction("ShoppingCart");
        }

        // Clear all items from the shopping cart.
        [HttpPost]
        [Authorize]
        public IActionResult ClearCart()
        {
            SaveCartToSession(new List<OrderItemsModel>());
            return RedirectToAction("ShoppingCart");
        }

        // Prepare the checkout view with order details.
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("ShoppingCart");

            string newOrderId = Guid.NewGuid().ToString();
            var newOrder = new OrderModel { ID = newOrderId, UserID = userId };
            decimal totalPrice = cart.Sum(item => item.Quantity * item.Product.Price);
            var checkoutTuple = Tuple.Create(newOrder, cart, totalPrice);
            return View("Checkout", checkoutTuple);
        }

        // Confirm the order and clear the shopping cart.
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmOrder()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("ShoppingCart");

            string newOrderId = Guid.NewGuid().ToString();
            var newOrder = new OrderModel { ID = newOrderId, UserID = userId };
            bool orderCreated = await _orderService.CreateOrderWithItemsAsync(newOrder, cart);

            if (!orderCreated)
            {
                ModelState.AddModelError("", "Failed to process order. Ensure products are in stock.");
                return RedirectToAction("ShoppingCart");
            }
            SaveCartToSession(new List<OrderItemsModel>());
            return RedirectToAction("OrderHistory");
        }

        // Log out the current user.
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
