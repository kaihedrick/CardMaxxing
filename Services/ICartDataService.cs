using System.Collections.Generic;
using System.Threading.Tasks;
using CardMaxxing.Models;

namespace CardMaxxing.Services
{/*** 
 * @interface ICartDataService
 * @description Interface defining operations for managing user shopping carts and checkout processes
 */
    public interface ICartDataService
    {
        /***
 * @method AddToCartAsync
 * @description Adds a product to a user's cart
 * @param {string} userId - Unique identifier of the user
 * @param {string} productId - Unique identifier of the product
 * @returns {Task<bool>} - True if addition succeeds, otherwise false
 */

        Task<bool> AddToCartAsync(string userId, string productId);
        /***
 * @method RemoveFromCartAsync
 * @description Removes a product from a user's cart
 * @param {string} userId - Unique identifier of the user
 * @param {string} productId - Unique identifier of the product
 * @returns {Task<bool>} - True if removal succeeds, otherwise false
 */
        Task<bool> RemoveFromCartAsync(string userId, string productId);
        /***
 * @method GetCartItemsAsync
 * @description Retrieves all items currently in a user's cart
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<OrderItemsModel>>} - List of items in the user's cart
 */
        Task<List<OrderItemsModel>> GetCartItemsAsync(string userId);
        /***
 * @method ClearCartAsync
 * @description Clears all items from a user's cart
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<bool>} - True if cart clearing succeeds, otherwise false
 */
        Task<bool> ClearCartAsync(string userId);
        /***
 * @method CheckoutAsync
 * @description Finalizes the checkout process and creates an order for the user
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<bool>} - True if checkout succeeds, otherwise false
 */

        Task<bool> CheckoutAsync(string userId);
        /***
 * @method GetOrdersByUserAsync
 * @description Retrieves all orders placed by a specific user
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<OrderModel>>} - List of the user's orders
 */
        Task<List<OrderModel>> GetOrdersByUserAsync(string userId);
        /***
 * @method GetOrderItemsAsync
 * @description Retrieves all items associated with a specific order
 * @param {string} orderId - Unique identifier of the order
 * @returns {Task<List<OrderItemsModel>>} - List of order items
 */

        Task<List<OrderItemsModel>> GetOrderItemsAsync(string orderId);
        /***
 * @method AddOrderItemAsync
 * @description Adds an item to an existing order
 * @param {string} orderItemId - Unique identifier for the order item
 * @param {string} orderId - Unique identifier of the order
 * @param {string} productId - Unique identifier of the product
 * @param {int} quantity - Quantity of the product
 * @returns {Task<bool>} - True if addition succeeds, otherwise false
 */
        Task<bool> AddOrderItemAsync(string orderItemId, string orderId, string productId, int quantity);
    }
}
