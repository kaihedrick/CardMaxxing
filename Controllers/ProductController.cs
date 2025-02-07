using CardMaxxing.Models;
using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardMaxxing.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductDataService _productService;
        private readonly ICartDataService _cartService; // ✅ Declare _cartService
        public ProductController(IProductDataService productService)
        {
            _productService = productService;
        }

        // GET: Product/Index - Show all products
        public IActionResult Index()
        {
            var products = _productService.getAllProducts();
            return View(products);
        }

        // GET: Product/Details/{id} - Show details for a single product
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            var product = _productService.getProductByID(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create - Show create product form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create - Add a new product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductModel product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            bool exists = _productService.checkProductDuplicate(product.Name);
            if (exists)
            {
                ModelState.AddModelError("Name", "Product with this name already exists.");
                return View(product);
            }

            bool result = _productService.createProduct(product);
            if (result)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "An error occurred while creating the product.");
            return View(product);
        }

        // GET: Product/Edit/{id} - Show edit product form
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            var product = _productService.getProductByID(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Edit/{id} - Update product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductModel product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var existingProduct = _productService.getProductByID(product.ID);
            if (existingProduct == null)
            {
                return NotFound();
            }

            bool result = _productService.editProduct(product);
            if (result)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "An error occurred while editing the product.");
            return View(product);
        }

        // GET: Product/Delete/{id} - Show delete confirmation page
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            var product = _productService.getProductByID(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/{id} - Confirm deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var product = _productService.getProductByID(id);
            if (product == null)
            {
                return NotFound();
            }

            bool result = _productService.deleteProduct(id);
            if (result)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "An error occurred while deleting the product.");
            return View(product);
        }
        public IActionResult Index(string searchTerm = "", string category = "")
        {
            var products = string.IsNullOrEmpty(searchTerm) && string.IsNullOrEmpty(category)
                ? _productService.getAllProducts()
                : _productService.searchProducts(searchTerm, category);

            return View(products);
        }
        public IActionResult AddToCart(string productId)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "User");

            bool result = _cartService.addToCart(userId, productId);
            if (result) return RedirectToAction("Cart", "Order");

            ModelState.AddModelError("", "Could not add item to cart.");
            return RedirectToAction("Index");
        }

    }
}
