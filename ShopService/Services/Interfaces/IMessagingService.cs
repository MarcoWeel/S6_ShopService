using RabbitMQ.Client.Events;

namespace ShopService.Services.Interfaces
{
    public interface IMessagingService
    {
        void Subscribe(string exchange, Action<BasicDeliverEventArgs, string, string> callback, string exchangeType);
        void Subscribe(string exchange, Action<BasicDeliverEventArgs, string, string> callback, string exchangeType, string bindingKey);
        void Publish(string exchange, string queue, string route, string request);
        void Publish(string exchange, string queue, string route, string request, byte[] message);
        void Publish(string exchange, string exchangeType, string queue, string route, string request, byte[] message);
        Task<string> PublishAndRetrieve(string exchange, string request);
        Task<string> PublishAndRetrieve(string exchange, string request, byte[] message);
    }
}
