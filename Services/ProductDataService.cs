﻿using Dapper;
using MySql.Data.MySqlClient;
using CardMaxxing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public class ProductDataService : IProductDataService
    {
        private readonly IDbConnection _db;

        public ProductDataService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<bool> CheckProductDuplicateAsync(string name)
        {
            string query = "SELECT COUNT(*) FROM products WHERE Name = @Name;";
            int count = await _db.ExecuteScalarAsync<int>(query, new { Name = name });
            return count > 0;
        }

        public async Task<bool> CreateProductAsync(ProductModel product)
        {
            string query = @"INSERT INTO products (ID, Name, Manufacturer, Description, Price, Quantity, ImageUrl)
                             VALUES (@ID, @Name, @Manufacturer, @Description, @Price, @Quantity, @ImageUrl);";

            product.ID = Guid.NewGuid().ToString();

            int rowsAffected = await _db.ExecuteAsync(query, product);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            string query = "DELETE FROM products WHERE ID = @ID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });
            return rowsAffected > 0;
        }

        public async Task<ProductModel> GetProductByIDAsync(string id)
        {
            string query = "SELECT * FROM products WHERE ID = @ID;";
            return await _db.QueryFirstOrDefaultAsync<ProductModel>(query, new { ID = id });
        }

        public async Task<List<ProductModel>> GetAllProductsAsync()
        {
            string query = "SELECT * FROM products;";
            return (await _db.QueryAsync<ProductModel>(query)).AsList();
        }

        public async Task<bool> EditProductAsync(ProductModel product)
        {
            string query = @"UPDATE products 
                             SET Name = @Name, Manufacturer = @Manufacturer, Description = @Description, 
                                 Price = @Price, Quantity = @Quantity, ImageUrl = @ImageUrl
                             WHERE ID = @ID;";

            int rowsAffected = await _db.ExecuteAsync(query, product);
            return rowsAffected > 0;
        }

        public async Task<List<ProductModel>> SearchProductsAsync(string searchTerm)
        {
            string query = @"SELECT * FROM products 
                             WHERE Name LIKE @SearchTerm OR Manufacturer LIKE @SearchTerm;";

            return (await _db.QueryAsync<ProductModel>(query, new { SearchTerm = "%" + searchTerm + "%" })).AsList();
        }
        public async Task<bool> DecreaseStockAsync(string productId, int quantity)
        {
            string query = @"
        UPDATE products 
        SET Quantity = Quantity - @Quantity 
        WHERE ID = @ProductID AND Quantity >= @Quantity;";

            int rowsAffected = await _db.ExecuteAsync(query, new { ProductID = productId, Quantity = quantity });

            if (rowsAffected == 0)
            {
                Console.WriteLine($"[ERROR] Not enough stock for ProductID: {productId}. Current quantity may be too low.");
            }
            else
            {
                Console.WriteLine($"[INFO] Stock updated for ProductID: {productId}. Decreased by {quantity}.");
            }

            return rowsAffected > 0;
        }


    }
}
