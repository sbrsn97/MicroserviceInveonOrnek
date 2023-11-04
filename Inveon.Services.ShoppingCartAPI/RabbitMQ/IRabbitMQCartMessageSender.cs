using Inveon.MessageBus;

namespace Inveon.Services.ShoppingCartAPI.RabbitMQ
{
    public interface IRabbitMQCartMessageSender
    {
        void SendMessage(BaseMessage baseMessage, String queueName);
    }
}
