using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using AmestoCandidateTask.Models;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text;


namespace Functions
{
    public class Task1
    {
        private readonly ILogger<Task1> _logger;
        private readonly HttpClient _httpClient;
        private readonly BlobServiceClient _blobServiceClient;


        public Task1(ILogger<Task1> logger, HttpClient httpClient, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _blobServiceClient = blobServiceClient;
        }

        /**
         * GetCompaniesProductsAndSalesAsync
         * 
         * This function fetches companies, products and sales orders from external APIs.
         * It then creates a summary of the orders and uploads it to Azure Blob Storage.
         * 
         * @param req HttpRequest
         * @return Task<IActionResult>
         */
        [Function("GetCompaniesProductsAndSalesFunction")]
        public async Task<IActionResult> GetCompaniesProductsAndSalesAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            // Create a list to store the summary of orders
            var list = new List<Summary>();

            // Create a list to store the orders
            var ordersList = new List<Order>();

            try
            {
                // Fetch companies and products
                var companiesResult = await GetCompanies() as JsonResult;
                var companies = companiesResult?.Value as List<Company>;

                var productsResult = await GetProducts() as JsonResult;
                var products = productsResult?.Value as List<Product>;

                if (companies == null || companies.Count == 0)
                {
                    _logger.LogWarning("No companies found");
                    return new NotFoundResult();
                }

                // Loop through companies
                foreach (var company in companies)
                {
                    _logger.LogInformation($"Company: {company.Name} (ID: {company.CompanyId})");

                    // Fetch orders
                    var ordersResult = await GetOrders(company.CompanyId) as JsonResult;
                    var orders = ordersResult?.Value as List<Order>;

                    if (orders == null || orders.Count == 0)
                    {
                        _logger.LogWarning($"No orders found for Company ID: {company.CompanyId}");
                        continue;
                    }

                    // Loop through orders
                    foreach (var order in orders)
                    {
                        _logger.LogInformation($"Order: {order.OrderId} (Company ID: {company.CompanyId})");

                        if (products == null || products.Count == 0)
                        {
                            _logger.LogWarning("No products found");
                            return new JsonResult(companies);
                        }

                        // Loop through products
                        foreach (var product in products)
                        {
                            // If the product ID matches the order ID, add the order to the list
                            if (product.ItemId == order.ItemId)
                            {
                                ordersList.Add(new Order
                                {
                                    OrderId = order.OrderId,
                                    ItemId = order.ItemId,
                                    Description = order.Description,
                                    Category = product.Category,
                                    Price = product.Price,
                                    Amount = order.Amount
                                });
                            }
                        }
                    }

                    // Add the summary to the list
                    list.Add(new Summary
                    {
                        CompanyId = company.CompanyId,
                        CompanyName = company.Name,
                        Order = ordersList
                    });
                }

                // Serialize the list to a JSON string
                string summaryString = JsonConvert.SerializeObject(list);

                // Upload the JSON string to Azure Blob Storage
                await UploadToBlob(JsonConvert.SerializeObject(list));

                _logger.LogInformation("Summary created successfully");

                // Return the list as a JSON response
                return new JsonResult(list);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }


        /** Helper methods **/
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _httpClient.GetFromJsonAsync<List<Company>>(Environment.GetEnvironmentVariable("CompaniesUrl"));

                if (companies == null || companies.Count == 0)
                {
                    _logger.LogWarning("No companies found");
                    return new NotFoundResult();
                }

                _logger.LogInformation("Companies fetched successfully");

                return new JsonResult(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _httpClient.GetFromJsonAsync<List<Product>>(Environment.GetEnvironmentVariable("ProductsUrl"));

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("No products found");
                    return new NotFoundResult();
                }

                _logger.LogInformation("Products fetched successfully");

                return new JsonResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<IActionResult> GetOrders(int companyId)
        {
            try
            {
                var orders = await _httpClient.GetFromJsonAsync<List<Order>>(Environment.GetEnvironmentVariable("SalesOrdersUrl") + companyId);

                if (orders == null || orders.Count == 0)
                {
                    _logger.LogWarning("No orders found");
                    return new NotFoundResult();
                }

                _logger.LogInformation("Orders fetched successfully");

                return new JsonResult(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task UploadToBlob(string summaryString)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient("orders");
                var blobClient = containerClient.GetBlobClient("task1.json");

                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(summaryString)))
                {
                    await blobClient.UploadAsync(memoryStream, true);
                    _logger.LogInformation("Summary uploaded to blob storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error when uploading JSON: {ex.Message}");
            }
        }

        /** End of helper methods **/

    }
}
