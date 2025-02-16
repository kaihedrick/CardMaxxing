using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace CardMaxxing.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductDataService _productService;

        public ProductController(IProductDataService productService)
        {
            _productService = productService;
        }

        // Display all products with the user's cart.
        public async Task<IActionResult> AllProducts()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<OrderItemsModel>()
                : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();

            ViewBag.Cart = cart;
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        // Display details for a specific product.
        public async Task<IActionResult> Details(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null)
                return NotFound();
            return View(product);
        }

        // Render the form to create a new product.
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct()
        {
            return View();
        }

        // Process the creation of a new product.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductModel product)
        {
            if (!ModelState.IsValid)
                return View(product);

            bool success = await _productService.CreateProductAsync(product);

            if (success)
            {
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("AllProducts");
            }

            ModelState.AddModelError("", "Error creating product. Try again.");
            return View(product);
        }

        // Render the form to edit an existing product.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null)
                return NotFound();
            return View(product);
        }

        // Process the editing of an existing product.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, ProductModel product)
        {
            if (!ModelState.IsValid)
                return View(product);

            var existingProduct = await _productService.GetProductByIDAsync(id);
            if (existingProduct == null)
                return NotFound();

            product.ID = id;
            bool updated = await _productService.EditProductAsync(product);
            if (!updated)
            {
                ModelState.AddModelError("", "Error updating product.");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
        }

        // Delete a product.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null)
                return NotFound();

            bool deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "Error deleting product. Try again.";
                return RedirectToAction("EditProduct", new { id });
            }

            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("AllProducts");
        }

        // Search for products by name or manufacturer.
        public async Task<IActionResult> Search(string searchTerm)
        {
            var results = await _productService.SearchProductsAsync(searchTerm);
            if (results.Count == 0)
            {
                ViewBag.Message = "No products found.";
            }
            return View("Index", results);
        }

        // Render the admin edit form for a product.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null)
                return NotFound();

            return View("EditProduct", product);
        }

        // Process the admin edit form submission.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id, ProductModel product)
        {
            if (!ModelState.IsValid)
                return View(product);

            var existingProduct = await _productService.GetProductByIDAsync(id);
            if (existingProduct == null)
                return NotFound();

            product.ID = id;
            bool updated = await _productService.EditProductAsync(product);
            if (!updated)
            {
                ModelState.AddModelError("", "Error updating product.");
                return View(product);
            }

            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("AllProducts");
        }
    }
}
