using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Security.Claims;
namespace CardMaxxing.Controllers
{
    /***
 * @class ProductController
 * @description Manages product listing, details, creation, editing, deletion, and searching.
 */
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductDataService _productService;
        private readonly ILogger<ProductController> _logger;
        private readonly TelemetryClient _telemetryClient;

/***
 * @constructor ProductController
 * @description Initializes a new instance of ProductController with required services.
 * @param {IProductDataService} productService - Service for product management.
 * @param {ILogger<ProductController>} logger - Logger instance for telemetry.
 * @param {TelemetryClient} telemetryClient - Application Insights telemetry client.
 */

        public ProductController(
            IProductDataService productService,
            ILogger<ProductController> logger,
            TelemetryClient telemetryClient)
        {
            _productService = productService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

/***
 * @method AllProducts
 * @description Displays all available products along with user's cart information.
 * @returns {Task<IActionResult>} - View of all products.
 */

        public async Task<IActionResult> AllProducts()
        {
            _logger.LogInformation("Fetching all products");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AllProducts");
                
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<OrderItemsModel>()
                    : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();

                ViewBag.Cart = cart;
                var products = await _productService.GetAllProductsAsync();
                
                _logger.LogInformation("Retrieved {Count} products", products.Count());
                _telemetryClient.TrackEvent("ProductsViewed", new Dictionary<string, string>
                {
                    { "ProductCount", products.Count().ToString() },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" }
                });
                
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AllProducts" }
                });
                throw;
            }
        }


/***
 * @method Details
 * @description Displays detailed information for a specific product.
 * @param {string} id - Product ID.
 * @returns {Task<IActionResult>} - Product detail view.
 */
        public async Task<IActionResult> Details(string id)
        {
            _logger.LogInformation("Viewing details for product {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ProductDetails");
                
                var product = await _productService.GetProductByIDAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("ProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id }
                    });
                    return NotFound();
                }
                
                _telemetryClient.TrackEvent("ProductDetailsViewed", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" }
                });
                
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details for {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ProductDetails" },
                    { "ProductId", id }
                });
                throw;
            }
        }

/***
 * @method CreateProduct
 * @description Displays the form to create a new product (Admin only).
 * @returns {IActionResult} - Create product form view.
 */

        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct()
        {
            _logger.LogInformation("Admin accessing create product form");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateProductForm");
                
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
                _telemetryClient.TrackEvent("CreateProductFormViewed", new Dictionary<string, string>
                {
                    { "UserId", userId }
                });
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing create product form");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateProductForm" }
                });
                throw;
            }
        }

/***
 * @method Create
 * @description Processes the form submission to create a new product (Admin only).
 * @param {ProductModel} product - Product details to create.
 * @returns {Task<IActionResult>} - Redirects or reloads form on error.
 */

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductModel product)
        {
            _logger.LogInformation("Admin attempting to create product {ProductName}", product.Name);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateProduct");
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when creating product {ProductName}", product.Name);
                    _telemetryClient.TrackEvent("ProductCreateValidationFailed", new Dictionary<string, string>
                    {
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" },
                        { "ValidationErrors", string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)) }
                    });
                    return View(product);
                }

                bool success = await _productService.CreateProductAsync(product);

                if (success)
                {
                    _logger.LogInformation("Product created successfully: {ProductName}", product.Name);
                    _telemetryClient.TrackEvent("ProductCreated", new Dictionary<string, string>
                    {
                        { "ProductName", product.Name },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction("AllProducts");
                }

                _logger.LogWarning("Failed to create product {ProductName}", product.Name);
                _telemetryClient.TrackEvent("ProductCreateFailed", new Dictionary<string, string>
                {
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                ModelState.AddModelError("", "Error creating product. Try again.");
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", product.Name);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateProduct" },
                    { "ProductName", product.Name }
                });
                throw;
            }
        }

/***
 * @method Edit
 * @description Displays the edit form for an existing product (Admin only).
 * @param {string} id - Product ID.
 * @returns {Task<IActionResult>} - Edit product form view.
 */
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            _logger.LogInformation("Admin accessing edit form for product {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditProductForm");
                
                var product = await _productService.GetProductByIDAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product to edit not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("EditProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    return NotFound();
                }
                
                _telemetryClient.TrackEvent("EditProductFormViewed", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing edit form for product {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditProductForm" },
                    { "ProductId", id }
                });
                throw;
            }
        }

/***
 * @method Edit (POST)
 * @description Processes the edit form submission for an existing product (Admin only).
 * @param {string} id - Product ID.
 * @param {ProductModel} product - Updated product data.
 * @returns {Task<IActionResult>} - Redirects or reloads form on error.
 */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, ProductModel product)
        {
            _logger.LogInformation("Admin attempting to update product {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditProduct");
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when updating product {ProductId}", id);
                    _telemetryClient.TrackEvent("ProductEditValidationFailed", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" },
                        { "ValidationErrors", string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)) }
                    });
                    return View(product);
                }

                var existingProduct = await _productService.GetProductByIDAsync(id);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product to update not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("UpdateProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    return NotFound();
                }

                product.ID = id;
                bool updated = await _productService.EditProductAsync(product);
                if (!updated)
                {
                    _logger.LogWarning("Failed to update product {ProductId}", id);
                    _telemetryClient.TrackEvent("ProductUpdateFailed", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "ProductName", product.Name },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    ModelState.AddModelError("", "Error updating product.");
                    return View(product);
                }

                _logger.LogInformation("Product updated successfully: {ProductId}", id);
                _telemetryClient.TrackEvent("ProductUpdated", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditProduct" },
                    { "ProductId", id }
                });
                throw;
            }
        }

/***
 * @method Delete
 * @description Deletes a product from the system (Admin only).
 * @param {string} id - Product ID.
 * @returns {Task<IActionResult>} - Redirects to product list.
 */
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Admin attempting to delete product {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DeleteProduct");
                
                var product = await _productService.GetProductByIDAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product to delete not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("DeleteProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    return NotFound();
                }

                bool deleted = await _productService.DeleteProductAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("Failed to delete product {ProductId}", id);
                    _telemetryClient.TrackEvent("ProductDeleteFailed", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "ProductName", product.Name },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    TempData["ErrorMessage"] = "Error deleting product. Try again.";
                    return RedirectToAction("EditProduct", new { id });
                }

                _logger.LogInformation("Product deleted successfully: {ProductId}", id);
                _telemetryClient.TrackEvent("ProductDeleted", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                TempData["SuccessMessage"] = "Product deleted successfully!";
                return RedirectToAction("AllProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "DeleteProduct" },
                    { "ProductId", id }
                });
                throw;
            }
        }

/***
 * @method Search
 * @description Searches products by name or manufacturer.
 * @param {string} searchTerm - Search keyword.
 * @returns {Task<IActionResult>} - View of search results.
 */
        public async Task<IActionResult> Search(string searchTerm)
        {
            _logger.LogInformation("User searching for products with term: {SearchTerm}", searchTerm);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("SearchProducts");
                
                var results = await _productService.SearchProductsAsync(searchTerm);
                
                _logger.LogInformation("Search for {SearchTerm} returned {Count} results", 
                    searchTerm, results.Count);
                _telemetryClient.TrackEvent("ProductSearch", new Dictionary<string, string>
                {
                    { "SearchTerm", searchTerm ?? "empty" },
                    { "ResultCount", results.Count.ToString() },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" }
                });
                
                ViewBag.SearchTerm = searchTerm;
                
                return View("Search", results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for products with term: {SearchTerm}", searchTerm);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "SearchProducts" },
                    { "SearchTerm", searchTerm ?? "empty" }
                });
                throw;
            }
        }

/***
 * @method SearchPartial
 * @description Handles AJAX product search and returns partial HTML content.
 * @param {string} searchTerm - Search keyword.
 * @returns {Task<IActionResult>} - Partial view for search results.
 */
        public async Task<IActionResult> SearchPartial(string searchTerm)
        {
            _logger.LogInformation("User performing AJAX search with term: {SearchTerm}", searchTerm);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("PartialSearch");
                
                var results = await _productService.SearchProductsAsync(searchTerm);
                
                _logger.LogInformation("Partial search for {SearchTerm} returned {Count} results", 
                    searchTerm, results.Count);
                _telemetryClient.TrackEvent("AjaxProductSearch", new Dictionary<string, string>
                {
                    { "SearchTerm", searchTerm ?? "empty" },
                    { "ResultCount", results.Count.ToString() },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous" }
                });
                
                // Get cart data for displaying quantities
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<OrderItemsModel>()
                    : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();
                ViewBag.Cart = cart;
                
                if (!results.Any())
                {
                    return PartialView("_NoSearchResults", searchTerm);
                }
                
                return PartialView("_ProductList", results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing partial search with term: {SearchTerm}", searchTerm);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "PartialSearch" },
                    { "SearchTerm", searchTerm ?? "empty" }
                });
                return Content("<div class='col-12'><div class='alert alert-danger'>An error occurred. Please try again.</div></div>");
            }
        }

/***
 * @method EditProduct
 * @description Displays the admin edit form for a product.
 * @param {string} id - Product ID.
 * @returns {Task<IActionResult>} - Admin edit form view.
 */
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id)
        {
            _logger.LogInformation("Admin accessing edit product form for {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditProductSpecial");
                
                var product = await _productService.GetProductByIDAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product for admin edit not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("AdminEditProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    return NotFound();
                }
                
                _telemetryClient.TrackEvent("AdminEditProductFormViewed", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                
                return View("EditProduct", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing admin edit form for product {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditProductSpecial" },
                    { "ProductId", id }
                });
                throw;
            }
        }

/***
 * @method EditProduct (POST)
 * @description Processes the admin edit form submission to update a product.
 * @param {string} id - Product ID.
 * @param {ProductModel} product - Updated product data.
 * @returns {Task<IActionResult>} - Redirects to product list or reloads form on error.
 */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id, ProductModel product)
        {
            _logger.LogInformation("Admin submitting edit form for product {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditProductSpecialSubmit");
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state in admin edit for product {ProductId}", id);
                    _telemetryClient.TrackEvent("AdminProductEditValidationFailed", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" },
                        { "ValidationErrors", string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)) }
                    });
                    return View(product);
                }

                var existingProduct = await _productService.GetProductByIDAsync(id);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product for admin update not found: {ProductId}", id);
                    _telemetryClient.TrackEvent("AdminUpdateProductNotFound", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    return NotFound();
                }

                product.ID = id;
                bool updated = await _productService.EditProductAsync(product);
                if (!updated)
                {
                    _logger.LogWarning("Admin failed to update product {ProductId}", id);
                    _telemetryClient.TrackEvent("AdminProductUpdateFailed", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "ProductName", product.Name },
                        { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                    });
                    ModelState.AddModelError("", "Error updating product.");
                    return View(product);
                }

                _logger.LogInformation("Admin successfully updated product {ProductId}", id);
                _telemetryClient.TrackEvent("AdminProductUpdated", new Dictionary<string, string>
                {
                    { "ProductId", id },
                    { "ProductName", product.Name },
                    { "UserId", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown" }
                });
                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("AllProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin update for product {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditProductSpecialSubmit" },
                    { "ProductId", id }
                });
                throw;
            }
        }
    }
}
