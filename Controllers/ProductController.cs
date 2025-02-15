﻿using CardMaxxing.Models;
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

        // Show all products
        public async Task<IActionResult> AllProducts()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<OrderItemsModel>()
                : JsonSerializer.Deserialize<List<OrderItemsModel>>(cartJson) ?? new List<OrderItemsModel>();

            ViewBag.Cart = cart;  // ✅ Pass cart to the view

            var products = await _productService.GetAllProductsAsync(); // ✅ Await the async method
            return View(products); // ✅ Now it passes List<ProductModel>, not Task<List<ProductModel>>
        }



        // Show product details
        public async Task<IActionResult> Details(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // Show create product form (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProduct()
        {
            return View();
        }

        // Handle product creation (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductModel product)
        {
            if (!ModelState.IsValid) return View(product);

            bool success = await _productService.CreateProductAsync(product);

            if (success)
            {
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("AllProducts");
            }

            ModelState.AddModelError("", "Error creating product. Try again.");
            return View(product);
        }

        // Show edit form (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // Handle product editing (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, ProductModel product)
        {
            if (!ModelState.IsValid) return View(product);

            var existingProduct = await _productService.GetProductByIDAsync(id);
            if (existingProduct == null) return NotFound();

            product.ID = id;
            bool updated = await _productService.EditProductAsync(product);
            if (!updated)
            {
                ModelState.AddModelError("", "Error updating product.");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
        }

        // Handle product deletion (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null) return NotFound();

            bool deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "Error deleting product. Try again.";
                return RedirectToAction("EditProduct", new { id });
            }

            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("AllProducts");
        }


        // Search products by name or manufacturer
        public async Task<IActionResult> Search(string searchTerm)
        {
            var results = await _productService.SearchProductsAsync(searchTerm);
            if (results.Count == 0)
            {
                ViewBag.Message = "No products found.";
            }
            return View("Index", results);
        }

        // Show edit form (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id)
        {
            var product = await _productService.GetProductByIDAsync(id);
            if (product == null) return NotFound();

            return View("EditProduct", product); // ✅ Explicitly reference "EditProduct.cshtml"
        }



        // Handle product editing (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(string id, ProductModel product)
        {
            if (!ModelState.IsValid) return View(product);

            var existingProduct = await _productService.GetProductByIDAsync(id);
            if (existingProduct == null) return NotFound(); // Prevent editing a non-existing product

            // Ensure we keep the same ID and update the new values
            product.ID = id;

            bool updated = await _productService.EditProductAsync(product);
            if (!updated)
            {
                ModelState.AddModelError("", "Error updating product.");
                return View(product);
            }

            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("AllProducts"); // Redirect to avoid reloading old data
        }

        // Handle product deletion (Admin only)

    }
}
