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
    public class MaterialService : IMaterialService
    {
        private readonly IMessagingService _messagingService;
        public MaterialService(IMessagingService messagingService)
        {
            _messagingService = messagingService;
        }
        public void SubscribeToGlobal()
        {
            _messagingService.Subscribe("material", (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, request), ExchangeType.Fanout, "*");
        }

        private static async void RouteCallback(BasicDeliverEventArgs ea, string request)
        {
            using ShopServiceContext context = new();

            string data = Encoding.UTF8.GetString(ea.Body.ToArray());

            switch (request)
            {
                case "addMaterial":
                    {
                        var material = JsonConvert.DeserializeObject<Material>(data);
                        if (material == null)
                            return;

                        var existing = await context.Material.SingleOrDefaultAsync(m => m.Id == material.Id);
                        if (existing != null)
                            return;

                        context.Add(material);
                        await context.SaveChangesAsync();

                        break;
                    }
                case "deleteMaterial":
                    {
                        Guid id = Guid.Parse(data);
                        Material material = await context.Material.SingleOrDefaultAsync(m => m.Id == id);
                        if (material == null)
                            return;
                        context.Material.Remove(material);
                        await context.SaveChangesAsync();
                        break;
                    };
                case "updateMaterial":
                    {
                        var material = JsonConvert.DeserializeObject<Material>(data);
                        if (material == null)
                            return;

                        var existing = await context.Material.SingleOrDefaultAsync(m => m.Id == material.Id);
                        if (existing == null) context.Add(material);
                        else
                        {
                            context.Material.Update(material);
                        }
                        await context.SaveChangesAsync();
                        break;
                    }
                default:
                    Console.WriteLine($"Request {request} Not Found");
                    break;
            }
        }
        public async Task<Material> GetMaterialAsync(Guid id)
        {
            using ShopServiceContext context = new();

            var material = await context.Material.SingleOrDefaultAsync(m => m.Id == id);

            if (material != null)
                return material;

            string response = await _messagingService.PublishAndRetrieve("material-data", "getMaterialById", Encoding.UTF8.GetBytes(id.ToString()));

            material = JsonConvert.DeserializeObject<Material>(response);
            if (material == null)
                return null;

            try
            {
                context.Add(material);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Already added");
            }


            return material;
        }

        public async Task<List<Material>> GetMaterialsAsync()
        {
            using ShopServiceContext context = new();

            if (!hasallMaterials)
                await RetrieveAllMaterials(context);

            return await context.Material.ToListAsync();
        }

        public async Task<Material> SaveMaterialAsync(Material material)
        {
            material.Id = Guid.NewGuid();
            using ShopServiceContext context = new();


            var existing = await context.Material.SingleOrDefaultAsync(m => m.Name == material.Name && m.Id == material.Id);
            if (existing != null)
                return null;

            string response = await _messagingService.PublishAndRetrieve("material-data", "addMaterial", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(material)));

            material = JsonConvert.DeserializeObject<Material>(response);
            if (material == null)
                return null;

            _messagingService.Publish("material", "material-messaging", "addMaterial", "addMaterial", Encoding.UTF8.GetBytes(response));

            return material;
        }


        public async Task<Material> UpdateMaterialAsync(Material updated)
        {
            var response = await _messagingService.PublishAndRetrieve("material-data", "updateMaterial", Encoding.UTF8.GetBytes(updated.Id.ToString()));
            if (response == null)
                return null;

            _messagingService.Publish("material", "material-messaging", "updateMaterial", "updateMaterial", Encoding.UTF8.GetBytes(updated.Id.ToString()));

            return updated;
        }

        private bool hasallMaterials = false;
        private Task gettingMaterials;
        private async Task RetrieveAllMaterials(ShopServiceContext context)
        {
            try
            {
                string response = await _messagingService.PublishAndRetrieve("material-data", "getAllMaterials");
                List<Material> materials = JsonConvert.DeserializeObject<List<Material>>(response);
                foreach (Material material in materials)
                {
                    bool existing = context.Material.FirstOrDefault(e => e.Id == material.Id) != null;
                    if (!existing)
                        context.Material.Add(material);
                }
                await context.SaveChangesAsync();
                await Task.Delay(1000);
                hasallMaterials = true;
            }
            catch (Exception ex)
            {
                gettingMaterials = null;
                throw new Exception(ex.Message);
            }
        }

        public async Task DeleteMaterialAsync(Guid id)
        {
            var response = await _messagingService.PublishAndRetrieve("material-data", "deleteMaterial", Encoding.UTF8.GetBytes(id.ToString()));

            _messagingService.Publish("material", "material-messaging", "deleteMaterial", "deleteMaterial", Encoding.UTF8.GetBytes(id.ToString()));
        }
    }
}
