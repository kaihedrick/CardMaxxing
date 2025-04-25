using CardMaxxing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{/*** 
 * @interface IOrderDataService
 * @description Interface defining operations for managing orders and order items in the database
 */
    public interface IOrderDataService
    {

/***
 * @method CreateOrderAsync
 * @description Creates a new order record without associated items
 * @param {OrderModel} order - Order model containing basic order information
 * @returns {Task<bool>} - True if order creation succeeds, otherwise false
 */
        Task<bool> CreateOrderAsync(OrderModel order);
        /***
 * @method DeleteOrderAsync
 * @description Deletes an order from the database by its unique identifier
 * @param {string} id - Unique identifier of the order
 * @returns {Task<bool>} - True if order deletion succeeds, otherwise false
 */
        Task<bool> DeleteOrderAsync(string id);
        /***
 * @method GetOrderByIDAsync
 * @description Retrieves a specific order by its unique identifier
 * @param {string} id - Unique identifier of the order
 * @returns {Task<OrderModel>} - Order model if found, otherwise null
 */

        Task<OrderModel> GetOrderByIDAsync(string id);
        /***
 * @method GetOrdersByUserIDAsync
 * @description Retrieves all orders belonging to a specific user
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<OrderModel>>} - List of the user's orders
 */
        Task<List<OrderModel>> GetOrdersByUserIDAsync(string userId);
        /***
 * @method GetOrderItemsByOrderIDAsync
 * @description Retrieves all order items for a specific order
 * @param {string} orderId - Unique identifier of the order
 * @returns {Task<List<OrderItemsModel>>} - List of order items
 */

        Task<List<OrderItemsModel>> GetOrderItemsByOrderIDAsync(string orderId);
        /***
 * @method GetAllOrdersAsync
 * @description Retrieves all orders from the database
 * @returns {Task<List<OrderModel>>} - List of all orders
 */

        Task<List<OrderModel>> GetAllOrdersAsync();
        /***
 * @method GetOrdersWithDetailsByUserIDAsync
 * @description Retrieves complete order details including items and totals for a specific user
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<(OrderModel, List<OrderItemsModel>, decimal)>>} - List of orders with detailed information
 */

        Task<List<(OrderModel, List<OrderItemsModel>, decimal)>> GetOrdersWithDetailsByUserIDAsync(string userId);
        /***
 * @method CreateOrderWithItemsAsync
 * @description Creates an order with multiple items and updates inventory in a transaction
 * @param {OrderModel} order - Basic order model
 * @param {List<OrderItemsModel>} items - List of order items to associate
 * @returns {Task<bool>} - True if transaction completes successfully, otherwise false
 */
        Task<bool> CreateOrderWithItemsAsync(OrderModel order, List<OrderItemsModel> items);
        /***
 * @method GetOrderTotalAsync
 * @description Calculates the total price of all items in a specific order
 * @param {string} orderId - Unique identifier of the order
 * @returns {Task<decimal>} - Total cost of the order
 */
        Task<decimal> GetOrderTotalAsync(string orderId);
        /***
 * @method GetAllOrdersWithDetailsAsync
 * @description Retrieves all orders along with user information, order items, and total prices
 * @returns {Task<List<(OrderModel, string, List<OrderItemsModel>, decimal)>>} - List of all orders with detailed information
 */
        Task<List<(OrderModel, string, List<OrderItemsModel>, decimal)>> GetAllOrdersWithDetailsAsync();
    }
}
