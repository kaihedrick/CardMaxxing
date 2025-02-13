using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
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
        public IActionResult Create()
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

            bool exists = await _productService.CheckProductDuplicateAsync(product.Name);
            if (exists)
            {
                ModelState.AddModelError("Name", "Product with this name already exists.");
                return View(product);
            }

            bool created = await _productService.CreateProductAsync(product);
            if (!created)
            {
                ModelState.AddModelError("", "Error creating product.");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
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
                ModelState.AddModelError("", "Error deleting product.");
                return View(product);
            }

            return RedirectToAction(nameof(Index));
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
    }
}
