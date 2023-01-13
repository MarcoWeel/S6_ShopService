using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using ShopService.Models;
using ShopService.Services.Interfaces;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ShopService.Data;

namespace ShopService.Services
{
    public class ProductService : IProductService
    {
        private readonly IMessagingService _messagingService;
        public ProductService(IMessagingService messagingService)
        {
            _messagingService = messagingService;
        }
        public void SubscribeToGlobal()
        {
            _messagingService.Subscribe("product", (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, request), ExchangeType.Fanout, "*");
        }

        private static async void RouteCallback(BasicDeliverEventArgs ea, string request)
        {
            using ShopServiceContext context = new();

            string data = Encoding.UTF8.GetString(ea.Body.ToArray());

            switch (request)
            {
                case "addProduct":
                    {
                        var product = JsonConvert.DeserializeObject<Product>(data);
                        if (product == null)
                            return;

                        var existing = await context.Product.SingleOrDefaultAsync(m => m.Id == product.Id);
                        if (existing != null)
                            return;

                        context.Add(product);
                        await context.SaveChangesAsync();

                        break;
                    }
                case "deleteProduct":
                    {
                        Guid id = Guid.Parse(data);
                        Product product = await context.Product.SingleOrDefaultAsync(m => m.Id == id);
                        if (product == null)
                            return;
                        context.Product.Remove(product);
                        await context.SaveChangesAsync();
                        break;
                    };
                case "updateProduct":
                    {
                        var product = JsonConvert.DeserializeObject<Product>(data);
                        if (product == null)
                            return;

                        var existing = await context.Product.SingleOrDefaultAsync(m => m.Id == product.Id);
                        if (existing == null) context.Add(product);
                        else
                        {
                            context.Product.Update(product);
                        }
                        await context.SaveChangesAsync();
                        break;
                    }
                default:
                    Console.WriteLine($"Request {request} Not Found");
                    break;
            }
        }
        public async Task<Product> GetProductAsync(Guid id)
        {
            using ShopServiceContext context = new();

            var product = await context.Product.Include(m => m.Material).SingleOrDefaultAsync(m => m.Id == id);

            if (product != null)
                return product;

            string response = await _messagingService.PublishAndRetrieve("product-data", "getProductById", Encoding.UTF8.GetBytes(id.ToString()));

            product = JsonConvert.DeserializeObject<Product>(response);
            if (product == null)
                return null;

            try
            {
                context.Add(product);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Already added");
            }


            return product;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            using ShopServiceContext context = new();

            if (!hasallProducts)
                await RetrieveAllProducts(context);

            return await context.Product.ToListAsync();
        }

        public async Task<Product> SaveProductAsync(Product product)
        {
            product.Id = Guid.NewGuid();
            using ShopServiceContext context = new();
            product.Material = context.Material.FirstOrDefault(x => x.Id == product.Material.Id);


            var existing = await context.Product.SingleOrDefaultAsync(m => m.Name == product.Name && m.Id == product.Id);
            if (existing != null)
                return null;

            string response = await _messagingService.PublishAndRetrieve("product-data", "addProduct", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(product)));

            product = JsonConvert.DeserializeObject<Product>(response);
            if (product == null)
                return null;

            _messagingService.Publish("product", "product-messaging", "addProduct", "addProduct", Encoding.UTF8.GetBytes(response));

            return product;
        }


        public async Task<Product> UpdateProductAsync(Product updated)
        {
            var response = await _messagingService.PublishAndRetrieve("product-data", "updateProduct", Encoding.UTF8.GetBytes(updated.Id.ToString()));
            if (response == null)
                return null;

            _messagingService.Publish("product", "product-messaging", "updateProduct", "updateProduct", Encoding.UTF8.GetBytes(updated.Id.ToString()));

            return updated;
        }

        private bool hasallProducts = false;
        private Task gettingProducts;
        private async Task RetrieveAllProducts(ShopServiceContext context)
        {
            try
            {
                string response = await _messagingService.PublishAndRetrieve("product-data", "getAllProducts");
                List<Product> products = JsonConvert.DeserializeObject<List<Product>>(response);
                foreach (Product product in products)
                {
                    bool existing = context.Product.FirstOrDefault(e => e.Id == product.Id) != null;
                    if (!existing)
                        context.Product.Add(product);
                }
                await context.SaveChangesAsync();
                await Task.Delay(1000);
                hasallProducts = true;
            }
            catch (Exception ex)
            {
                gettingProducts = null;
                throw new Exception(ex.Message);
            }
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var response = await _messagingService.PublishAndRetrieve("product-data", "deleteProduct", Encoding.UTF8.GetBytes(id.ToString()));

            _messagingService.Publish("product", "product-messaging", "deleteProduct", "deleteProduct", Encoding.UTF8.GetBytes(id.ToString()));
        }
    }
}
