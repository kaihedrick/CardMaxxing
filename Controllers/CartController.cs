using CardMaxxing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CardMaxxing.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartDataService _cartService;

        public CartController(ICartDataService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "User");

            await _cartService.AddToCartAsync(userId, productId);
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "User");

            await _cartService.RemoveFromCartAsync(userId, productId);
            return RedirectToAction("Cart");
        }

        [HttpGet]
        public async Task<IActionResult> ShoppingCart()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "User");

            var cartItems = await _cartService.GetCartItemsAsync(userId);
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "User");

            await _cartService.CheckoutAsync(userId);
            return RedirectToAction("Orders", "User");
        }
    }
}
