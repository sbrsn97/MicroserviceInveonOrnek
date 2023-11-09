using Inveon.Services.Email;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<RabbitMQConsumer>();
        
    })
    .Build();

host.Run();
