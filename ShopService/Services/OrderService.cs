using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using ShopService.Data;
using ShopService.Models;
using ShopService.Services.Interfaces;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ShopService.Services
{
    public class OrderService : IOrderService
    {
        private readonly IMessagingService _messagingService;
        public OrderService(IMessagingService messagingService)
        {
            _messagingService = messagingService;
        }
        public void SubscribeToGlobal()
        {
            _messagingService.Subscribe("order", (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, request), ExchangeType.Fanout, "*");
            _messagingService.Subscribe("gdprExchange",
                (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, request),
                ExchangeType.Topic, "*");
        }

        private static async void RouteCallback(BasicDeliverEventArgs ea, string request)
        {
            using ShopServiceContext context = new();

            string data = Encoding.UTF8.GetString(ea.Body.ToArray());

            switch (request)
            {
                case "addOrder":
                    {
                        var order = JsonConvert.DeserializeObject<Order>(data);
                        if (order == null)
                            return;

                        var existing = await context.Order.SingleOrDefaultAsync(m => m.Id == order.Id);
                        if (existing != null)
                            return;

                        context.Add(order);
                        await context.SaveChangesAsync();

                        break;
                    }
                case "deleteOrder":
                    {
                        Guid id = Guid.Parse(data);
                        Order order = await context.Order.Include(x=>x.Products).SingleOrDefaultAsync(m => m.Id == id);
                        if (order == null)
                            return;
                        context.Order.Remove(order);
                        await context.SaveChangesAsync();
                        break;
                    };
                case "updateOrder":
                    {
                        var order = JsonConvert.DeserializeObject<Order>(data);
                        if (order == null)
                            return;

                        var existing = await context.Order.SingleOrDefaultAsync(m => m.Id == order.Id);
                        if (existing == null) context.Add(order);
                        else
                        {
                            context.Order.Update(order);
                        }
                        await context.SaveChangesAsync();
                        break;
                    }
                case "gdprDelete":
                {
                    var orders = await context.Order.Where(m => m.UserGuid == Guid.Parse(data)).ToListAsync();
                    foreach (var order in orders)
                    {
                        context.Order.Remove(order);
                    }
                    await context.SaveChangesAsync();
                    break;
                }
                default:
                    Console.WriteLine($"Request {request} Not Found");
                    break;
            }
        }
        public async Task<Order> GetOrderAsync(Guid id)
        {
            using ShopServiceContext context = new();

            var order = await context.Order.SingleOrDefaultAsync(m => m.Id == id);

            if (order != null)
                return order;

            string response = await _messagingService.PublishAndRetrieve("order-data", "getOrderById", Encoding.UTF8.GetBytes(id.ToString()));

            order = JsonConvert.DeserializeObject<Order>(response);
            if (order == null)
                return null;

            try
            {
                context.Add(order);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Already added");
            }


            return order;
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            using ShopServiceContext context = new();

            if (!hasallOrders)
                await RetrieveAllOrders(context);

            return await context.Order.ToListAsync();
        }

        public async Task<Order> SaveOrderAsync(Order order)
        {
            order.Id = Guid.NewGuid();
            using ShopServiceContext context = new(); 


            var existing = await context.Order.SingleOrDefaultAsync(m => m.Id == order.Id);
            if (existing != null)
                return null;

            string response = await _messagingService.PublishAndRetrieve("order-data", "addOrder", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order)));

            order = JsonConvert.DeserializeObject<Order>(response);
            if (order == null)
                return null;

            List<ProductToInvoice> products = new List<ProductToInvoice>();
            foreach (var mapping in order.Products)
            {
                var product = await context.Product.SingleOrDefaultAsync(x => x.Id == mapping.ProductId);
                var material = await context.Material.SingleOrDefaultAsync(x => x.Id == product.MaterialId);
                products.Add(new ProductToInvoice
                {
                    Description = product.Description,
                    Id = product.Id,
                    Material = material,
                    Name = product.Name,
                    StockAmount = product.StockAmount
                });
            }

            var orderToInvoice = new OrderToInvoice()
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                UserGuid = order.UserGuid,
                Products = products
            };

            var invoiceMessage = JsonConvert.SerializeObject(orderToInvoice);
            _messagingService.Publish("order", "order-messaging", "addOrder", "addOrder", Encoding.UTF8.GetBytes(response));
            _messagingService.Publish("order", "order-messaging", "addOrderToInvoice", "addOrderToInvoice", Encoding.UTF8.GetBytes(invoiceMessage));

            return order;
        }


        public async Task<Order> UpdateOrderAsync(Order updated)
        {
            var response = await _messagingService.PublishAndRetrieve("order-data", "updateOrder", Encoding.UTF8.GetBytes(updated.Id.ToString()));
            if (response == null)
                return null;

            _messagingService.Publish("order", "order-messaging", "updateOrder", "updateOrder", Encoding.UTF8.GetBytes(updated.Id.ToString()));

            return updated;
        }

        private bool hasallOrders = false;
        private Task gettingOrders;
        private async Task RetrieveAllOrders(ShopServiceContext context)
        {
            try
            {
                string response = await _messagingService.PublishAndRetrieve("order-data", "getAllOrders");
                List<Order> orders = JsonConvert.DeserializeObject<List<Order>>(response);
                foreach (Order order in orders)
                {
                    bool existing = context.Order.FirstOrDefault(e => e.Id == order.Id) != null;
                    if (!existing)
                        context.Order.Add(order);
                }
                await context.SaveChangesAsync();
                await Task.Delay(1000);
                hasallOrders = true;
            }
            catch (Exception ex)
            {
                gettingOrders = null;
                throw new Exception(ex.Message);
            }
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            var response = await _messagingService.PublishAndRetrieve("order-data", "deleteOrder", Encoding.UTF8.GetBytes(id.ToString()));

            _messagingService.Publish("order", "order-messaging", "deleteOrder", "deleteOrder", Encoding.UTF8.GetBytes(id.ToString()));
        }
    }
}
