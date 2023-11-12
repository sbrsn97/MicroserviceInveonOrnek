using Inveon.Services.Email.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Inveon.Services.Email
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumer> _logger;

        public RabbitMQConsumer(ILogger<RabbitMQConsumer> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "127.0.0.1",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            while (!stoppingToken.IsCancellationRequested)
                {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    //baðlanacaðýmýz kuyruðun adýný belirtiyoruz 
                    channel.QueueDeclare(queue: "emailqueue",
                                       durable: false,
                                       exclusive: false,
                                       autoDelete: false,
                                       arguments: null);
                    //Event Delegate 
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        //kuyruktan okumaya baþlayacak
                        var body = ea.Body.ToArray();
                        //Byte array olrarak kuyruktan okuduðu veriyi önce String e dönüþtürecek

                        var message = Encoding.UTF8.GetString(body);
                        CheckoutHeaderDto checkoutHeaderDto = new CheckoutHeaderDto();
                        //String olarak dönüþtürdüðü veriye DeSerialezi ile tekra produc objesine dönüþütürecek
                        checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(message);
                        SendEmail(checkoutHeaderDto);

                    };

                    channel.BasicConsume(queue: "emailqueue",
                                     autoAck: true,
                                     consumer: consumer);
                    
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        public void SendEmail(CheckoutHeaderDto dto)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Ecommerce", "noreply.ecommerce.example@gmail.com"));
            message.To.Add(new MailboxAddress($"{dto.FirstName}", $"{dto.Email}"));
            message.Subject = "Order Confirmation";

            message.Body = new TextPart("plain")
            {
                Text = GenerateMailBody(dto)
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 465, true);

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate("noreply.ecommerce.example@gmail.com", "kzppmghxodnjiijo");

                client.Send(message);
                client.Disconnect(true);
            }
        }
        public string GenerateMailBody(CheckoutHeaderDto dto)
        {
            string products = "";

            if (dto != null)
            {
                foreach (var details in dto.CartDetails)
                {
                    products += $"{details.Product.Name}   x{details.Count} : ${Convert.ToDouble(details.Product.Price) * Convert.ToDouble(details.Count)} \n ";
                }
                return $" Dear {dto.FirstName} {dto.LastName},\r\n\r\n We are pleased to inform you that your order has been successfully placed. Below are the details of your order:\r\n\r\n Order Date: {DateTime.Now}\r\n\r\n Products:\r\n\r\n {products} \r\n Total Price: ${dto.OrderTotal:F2} \r\n Your Phone Number: {dto.Phone} \r\n Thank you for choosing our services. If you have any questions or need further assistance, please feel free to contact us.\r\n\r\n Best regards, Inveon.Web\r\n";

            }
            return products;
        }
    }
}