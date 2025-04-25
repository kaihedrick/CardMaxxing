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
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace CardMaxxing.Controllers
{
    /*** 
 * @class UserController
 * @description Manages user authentication, registration, dashboard, shopping cart, checkout, and order history.
 */
    public class UserController : Controller
    {
        private readonly IUserDataService _userService;
        private readonly IOrderDataService _orderService;
        private readonly ICartDataService _cartService; //not used in the current iteration but can be used for permanent data later
        private readonly IProductDataService _productService;
        private readonly ILogger<UserController> _logger;
        private readonly TelemetryClient _telemetryClient;

/***
 * @constructor UserController
 * @description Initializes a new instance of the UserController with required services.
 * @param {IUserDataService} userService - Service for user account management.
 * @param {IOrderDataService} orderService - Service for order and checkout management.
 * @param {ICartDataService} cartService - Service for cart data handling.
 * @param {IProductDataService} productService - Service for product retrieval.
 * @param {ILogger<UserController>} logger - Logger instance for application telemetry.
 * @param {TelemetryClient} telemetryClient - Application Insights telemetry client.
 */
        public UserController(
            IUserDataService userService, 
            IOrderDataService orderService, 
            ICartDataService cartService, 
            IProductDataService productService,
            ILogger<UserController> logger,
            TelemetryClient telemetryClient)
        {
            _userService = userService;
            _orderService = orderService;
            _cartService = cartService;
            _productService = productService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

/***
 * @method Register
 * @description Displays the user registration form view.
 * @returns {IActionResult} - Registration view.
 */
        public IActionResult Register()
        {
            _logger.LogInformation("User accessing registration page");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("RegisterForm");
                _telemetryClient.TrackEvent("RegisterFormViewed");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing registration page");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "RegisterForm" }
                });
                throw;
            }
        }
/***
 * @method Register (POST)
 * @description Processes user registration form submission.
 * @param {UserModel} user - New user data for account creation.
 * @returns {Task<IActionResult>} - Redirects on success or returns view with errors.
 */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserModel user)
        {
            _logger.LogInformation("Processing user registration for {Email}", user.Email);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("RegisterUser");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state during registration for {Email}", user.Email);
                    _telemetryClient.TrackEvent("RegisterValidationFailed");
                    return View(user);
                }

                bool exists = await _userService.CheckEmailDuplicateAsync(user.Email);
                if (exists)
                {
                    _logger.LogWarning("Registration attempted with duplicate email: {Email}", user.Email);
                    _telemetryClient.TrackEvent("RegisterDuplicateEmail", new Dictionary<string, string>
                    {
                        { "Email", user.Email }
                    });
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(user);
                }

                bool result = await _userService.CreateUserAsync(user);
                if (result)
                {
                    _logger.LogInformation("User registered successfully: {Email}", user.Email);
                    _telemetryClient.TrackEvent("UserRegistered", new Dictionary<string, string>
                    {
                        { "Email", user.Email }
                    });
                    return RedirectToAction("Login");
                }

                _logger.LogWarning("Failed to create user account: {Email}", user.Email);
                _telemetryClient.TrackEvent("RegisterFailed");
                ModelState.AddModelError("", "Error creating account.");
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", user.Email);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "RegisterUser" },
                    { "Email", user.Email }
                });
                throw;
            }
        }

/***
 * @method Login
 * @description Displays the user login form view.
 * @returns {IActionResult} - Login view.
 */
        public IActionResult Login()
        {
            _logger.LogInformation("User accessing login page");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("LoginForm");
                _telemetryClient.TrackEvent("LoginFormViewed");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing login page");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "LoginForm" }
                });
                throw;
            }
        }


/***
 * @method Login (POST)
 * @description Authenticates user credentials and sets authentication cookies.
 * @param {LoginModel} credentials - User login credentials.
 * @returns {Task<IActionResult>} - Redirects on successful login or returns view with errors.
 */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel credentials)
        {
            _logger.LogInformation("Processing login for username: {Username}", credentials.Username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("LoginAttempt");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state during login for {Username}", credentials.Username);
                    _telemetryClient.TrackEvent("LoginValidationFailed");
                    return View(credentials);
                }

                bool authenticated = await _userService.VerifyAccountAsync(credentials.Username, credentials.Password);
                UserModel user = await _userService.GetUserByUsernameAsync(credentials.Username);

                if (!authenticated || user == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", credentials.Username);
                    _telemetryClient.TrackEvent("LoginFailed", new Dictionary<string, string>
                    {
                        { "Username", credentials.Username }
                    });
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(credentials);
                }

                string role = user.Role ?? "User";
                
                _logger.LogInformation("User {Username} logged in successfully with role {Role}", user.Username, role);
                _telemetryClient.TrackEvent("UserLoggedIn", new Dictionary<string, string>
                {
                    { "UserId", user.ID },
                    { "Username", user.Username },
                    { "Role", role }
                });

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.ID),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                                             new ClaimsPrincipal(claimsIdentity), 
                                             authProperties);

                return role == "Admin" ? RedirectToAction("AdminDashboard", "Admin") : RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Username}", credentials.Username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "LoginAttempt" },
                    { "Username", credentials.Username }
                });
                throw;
            }
        }

/***
 * @method Dashboard
 * @description Displays the authenticated user's dashboard with profile information.
 * @returns {Task<IActionResult>} - Dashboard view.
 */
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} accessing dashboard", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("UserDashboard");
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims when accessing dashboard");
                    _telemetryClient.TrackEvent("DashboardAccessDenied");
                    return RedirectToAction("Login");
                }

                var user = await _userService.GetUserByIDAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found in database when accessing dashboard", userId);
                    _telemetryClient.TrackEvent("DashboardUserNotFound", new Dictionary<string, string>
                    {
                        { "UserId", userId }
                    });
                    return RedirectToAction("Login");
                }

                _telemetryClient.TrackEvent("DashboardViewed", new Dictionary<string, string>
                {
                    { "UserId", userId }
                });

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing dashboard for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "UserDashboard" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }
/***
 * @method OrderHistory
 * @description Displays authenticated user's order history with order details.
 * @returns {Task<IActionResult>} - Order history view.
 */
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} accessing order history", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("OrderHistory");
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims when accessing order history");
                    _telemetryClient.TrackEvent("OrderHistoryAccessDenied");
                    return RedirectToAction("Login");
                }

                List<(OrderModel, List<OrderItemsModel>, decimal)> orderDetails = 
                    await _orderService.GetOrdersWithDetailsByUserIDAsync(userId);
                
                _logger.LogInformation("Retrieved {OrderCount} orders for user {UserId}", 
                    orderDetails.Count, userId);
                _telemetryClient.TrackEvent("OrderHistoryViewed", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "OrderCount", orderDetails.Count.ToString() }
                });

                return View(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "OrderHistory" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method ShoppingCart
 * @description Displays the user's current shopping cart contents.
 * @returns {Task<IActionResult>} - Shopping cart view.
 */
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ShoppingCart()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} viewing shopping cart", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ViewCart");
                
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
                
                _logger.LogInformation("Cart loaded with {ItemCount} items for user {UserId}", 
                    cart.Count, userId);
                _telemetryClient.TrackEvent("CartViewed", new Dictionary<string, string>
                {
                    { "UserId", userId ?? "unknown" },
                    { "ItemCount", cart.Count.ToString() },
                    { "CartValue", cart.Sum(i => i.Product?.Price * i.Quantity ?? 0).ToString("F2") }
                });
                
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shopping cart for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ViewCart" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method UpdateCart
 * @description Updates the quantity of a product in the user's shopping cart.
 * @param {Dictionary<string, string>} request - Contains productId and action (add/remove).
 * @returns {IActionResult} - Updated cart item quantity as JSON.
 */
        [HttpPost]
        [Authorize]
        public IActionResult UpdateCart([FromBody] Dictionary<string, string> request)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("UpdateCart");
                
                if (!request.ContainsKey("productId") || !request.ContainsKey("action"))
                {
                    _logger.LogWarning("Invalid cart update request from user {UserId}", userId);
                    _telemetryClient.TrackEvent("InvalidCartUpdateRequest");
                    return BadRequest(new { error = "Invalid request data" });
                }

                string productId = request["productId"];
                string action = request["action"];
                
                _logger.LogInformation("User {UserId} updating cart: {Action} product {ProductId}", 
                    userId, action, productId);
                
                var cart = GetCartFromSession();
                var existingItem = cart.FirstOrDefault(item => item.ProductID == productId);

                if (action == "add")
                {
                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                        _logger.LogInformation("Increased quantity of product {ProductId} to {Quantity}", 
                            productId, existingItem.Quantity);
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
                            _logger.LogInformation("Added new product {ProductId} to cart", productId);
                        }
                    }
                    
                    _telemetryClient.TrackEvent("ProductAddedToCart", new Dictionary<string, string>
                    {
                        { "UserId", userId ?? "unknown" },
                        { "ProductId", productId },
                        { "NewQuantity", existingItem?.Quantity.ToString() ?? "0" }
                    });
                }
                else if (action == "remove" && existingItem != null)
                {
                    existingItem.Quantity--;
                    if (existingItem.Quantity <= 0)
                    {
                        cart.Remove(existingItem);
                        existingItem = null;
                        _logger.LogInformation("Removed product {ProductId} from cart", productId);
                    }
                    else
                    {
                        _logger.LogInformation("Decreased quantity of product {ProductId} to {Quantity}", 
                            productId, existingItem.Quantity);
                    }
                    
                    _telemetryClient.TrackEvent("ProductRemovedFromCart", new Dictionary<string, string>
                    {
                        { "UserId", userId ?? "unknown" },
                        { "ProductId", productId },
                        { "NewQuantity", existingItem?.Quantity.ToString() ?? "0" }
                    });
                }
                
                SaveCartToSession(cart);
                int updatedQuantity = existingItem?.Quantity ?? 0;
                
                return Json(new { quantity = updatedQuantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart for user {UserId}, product {ProductId}, action {Action}", 
                    userId, request.GetValueOrDefault("productId"), request.GetValueOrDefault("action"));
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "UpdateCart" },
                    { "UserId", userId ?? "unknown" },
                    { "ProductId", request.GetValueOrDefault("productId", "unknown") },
                    { "Action", request.GetValueOrDefault("action", "unknown") }
                });
                throw;
            }
        }

/***
 * @method AddToCart
 * @description Adds a product to the shopping cart or increases quantity if already added.
 * @param {Dictionary<string, string>} request - Contains productId to add.
 * @returns {IActionResult} - Updated cart item quantity as JSON.
 */
        [HttpPost]
        [Authorize]
        public IActionResult AddToCart([FromBody] Dictionary<string, string> request)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AddToCart");
                
                if (!request.ContainsKey("productId"))
                {
                    _logger.LogWarning("Invalid add to cart request from user {UserId}", userId);
                    _telemetryClient.TrackEvent("InvalidAddToCartRequest");
                    return BadRequest(new { error = "Invalid request data" });
                }

                string productId = request["productId"];
                _logger.LogInformation("User {UserId} adding product {ProductId} to cart", userId, productId);
                
                var cart = GetCartFromSession();
                var existingItem = cart.FirstOrDefault(item => item.ProductID == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    _logger.LogInformation("Increased quantity of product {ProductId} to {Quantity}", 
                        productId, existingItem.Quantity);
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
                        _logger.LogInformation("Added new product {ProductId} to cart", productId);
                    }
                }
                
                SaveCartToSession(cart);
                int updatedQuantity = existingItem?.Quantity ?? 1;
                
                _telemetryClient.TrackEvent("ProductAddedToCart", new Dictionary<string, string>
                {
                    { "UserId", userId ?? "unknown" },
                    { "ProductId", productId },
                    { "NewQuantity", updatedQuantity.ToString() }
                });
                
                return Json(new { quantity = updatedQuantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart for user {UserId}, product {ProductId}", 
                    userId, request.GetValueOrDefault("productId"));
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AddToCart" },
                    { "UserId", userId ?? "unknown" },
                    { "ProductId", request.GetValueOrDefault("productId", "unknown") }
                });
                throw;
            }
        }

/***
 * @method GetCartFromSession
 * @description Retrieves the shopping cart from the current session.
 * @returns {List<OrderItemsModel>} - Current cart items.
 */
        private List<OrderItemsModel> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<OrderItemsModel>();
            }
            return JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();
        }

/***
 * @method SaveCartToSession
 * @description Saves the updated shopping cart into the session.
 * @param {List<OrderItemsModel>} cart - Updated cart contents.
 */
        private void SaveCartToSession(List<OrderItemsModel> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

/***
 * @method RemoveFromCart
 * @description Removes or decrements a product quantity from the shopping cart.
 * @param {string} productId - ID of the product to remove.
 * @returns {IActionResult} - Redirects to the shopping cart view.
 */

        [HttpPost]
        [Authorize]
        public IActionResult RemoveFromCart(string productId)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} removing product {ProductId} from cart", userId, productId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("RemoveFromCart");
                
                var cart = GetCartFromSession();
                var item = cart.Find(x => x.ProductID == productId);
                
                if (item != null)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0)
                    {
                        cart.Remove(item);
                        _logger.LogInformation("Removed product {ProductId} from cart", productId);
                    }
                    else
                    {
                        _logger.LogInformation("Decreased quantity of product {ProductId} to {Quantity}", 
                            productId, item.Quantity);
                    }
                    
                    _telemetryClient.TrackEvent("ProductRemovedFromCart", new Dictionary<string, string>
                    {
                        { "UserId", userId ?? "unknown" },
                        { "ProductId", productId },
                        { "NewQuantity", (item.Quantity <= 0 ? 0 : item.Quantity).ToString() }
                    });
                }
                
                SaveCartToSession(cart);
                return RedirectToAction("ShoppingCart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product from cart for user {UserId}, product {ProductId}", 
                    userId, productId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "RemoveFromCart" },
                    { "UserId", userId ?? "unknown" },
                    { "ProductId", productId }
                });
                throw;
            }
        }


/***
 * @method ClearCart
 * @description Clears all items from the user's shopping cart.
 * @returns {IActionResult} - Redirects to the shopping cart view.
 */
        [HttpPost]
        [Authorize]
        public IActionResult ClearCart()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} clearing cart", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ClearCart");
                
                var cart = GetCartFromSession();
                int itemCount = cart.Count;
                decimal cartValue = cart.Sum(i => i.Product?.Price * i.Quantity ?? 0);
                
                SaveCartToSession(new List<OrderItemsModel>());
                
                _telemetryClient.TrackEvent("CartCleared", new Dictionary<string, string>
                {
                    { "UserId", userId ?? "unknown" },
                    { "ItemCount", itemCount.ToString() },
                    { "CartValue", cartValue.ToString("F2") }
                });
                
                return RedirectToAction("ShoppingCart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ClearCart" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method Checkout
 * @description Prepares the checkout page displaying order summary and total.
 * @returns {Task<IActionResult>} - Checkout view.
 */
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} proceeding to checkout", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("Checkout");
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims during checkout");
                    _telemetryClient.TrackEvent("CheckoutAccessDenied");
                    return RedirectToAction("Login");
                }

                var cart = GetCartFromSession();
                if (cart.Count == 0)
                {
                    _logger.LogWarning("User {UserId} attempted checkout with empty cart", userId);
                    _telemetryClient.TrackEvent("EmptyCartCheckout", new Dictionary<string, string>
                    {
                        { "UserId", userId }
                    });
                    return RedirectToAction("ShoppingCart");
                }

                string newOrderId = Guid.NewGuid().ToString();
                var newOrder = new OrderModel { ID = newOrderId, UserID = userId };
                decimal totalPrice = cart.Sum(item => item.Quantity * item.Product.Price);
                
                _logger.LogInformation("User {UserId} checkout prepared with {ItemCount} items, total: ${TotalPrice}", 
                    userId, cart.Count, totalPrice);
                _telemetryClient.TrackEvent("CheckoutViewed", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "OrderId", newOrderId },
                    { "ItemCount", cart.Count.ToString() },
                    { "TotalPrice", totalPrice.ToString("F2") }
                });
                
                var checkoutTuple = Tuple.Create(newOrder, cart, totalPrice);
                return View("Checkout", checkoutTuple);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "Checkout" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method ConfirmOrder
 * @description Processes the confirmed order by creating it and clearing the cart.
 * @returns {Task<IActionResult>} - Redirects to order history view.
 */
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmOrder()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} confirming order", userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ConfirmOrder");
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims during order confirmation");
                    _telemetryClient.TrackEvent("OrderConfirmationAccessDenied");
                    return RedirectToAction("Login");
                }

                var cart = GetCartFromSession();
                if (cart.Count == 0)
                {
                    _logger.LogWarning("User {UserId} attempted to confirm order with empty cart", userId);
                    _telemetryClient.TrackEvent("EmptyCartOrderConfirmation", new Dictionary<string, string>
                    {
                        { "UserId", userId }
                    });
                    return RedirectToAction("ShoppingCart");
                }

                string newOrderId = Guid.NewGuid().ToString();
                var newOrder = new OrderModel { ID = newOrderId, UserID = userId };
                decimal totalPrice = cart.Sum(item => item.Quantity * item.Product.Price);
                int itemCount = cart.Sum(item => item.Quantity);
                
                bool orderCreated = await _orderService.CreateOrderWithItemsAsync(newOrder, cart);

                if (!orderCreated)
                {
                    _logger.LogWarning("Failed to create order for user {UserId}", userId);
                    _telemetryClient.TrackEvent("OrderCreationFailed", new Dictionary<string, string>
                    {
                        { "UserId", userId },
                        { "OrderId", newOrderId },
                        { "ItemCount", itemCount.ToString() },
                        { "TotalPrice", totalPrice.ToString("F2") }
                    });
                    ModelState.AddModelError("", "Failed to process order. Ensure products are in stock.");
                    return RedirectToAction("ShoppingCart");
                }
                
                _logger.LogInformation("Order {OrderId} created successfully for user {UserId} with {ItemCount} items, total: ${TotalPrice}", 
                    newOrderId, userId, itemCount, totalPrice);
                _telemetryClient.TrackEvent("OrderPlaced", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "OrderId", newOrderId },
                    { "ItemCount", itemCount.ToString() },
                    { "TotalPrice", totalPrice.ToString("F2") }
                });
                
                SaveCartToSession(new List<OrderItemsModel>());
                return RedirectToAction("OrderHistory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ConfirmOrder" },
                    { "UserId", userId ?? "unknown" }
                });
                throw;
            }
        }

/***
 * @method Logout
 * @description Signs the user out and clears authentication cookies.
 * @returns {Task<IActionResult>} - Redirects to login view.
 */
        public async Task<IActionResult> Logout()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? username = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation("User {Username} ({UserId}) logging out", username, userId);
            
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("Logout");
                
                _telemetryClient.TrackEvent("UserLogout", new Dictionary<string, string>
                {
                    { "UserId", userId ?? "unknown" },
                    { "Username", username ?? "unknown" }
                });
                
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {Username} ({UserId})", username, userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "Logout" },
                    { "UserId", userId ?? "unknown" },
                    { "Username", username ?? "unknown" }
                });
                throw;
            }
        }
    }
}
