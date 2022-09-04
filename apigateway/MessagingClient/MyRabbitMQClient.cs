using System.Text;
using RabbitMQ.Client;

namespace apigateway.MessagingClient;

public class MyRabbitMQClient : IMessagingClient
{

    public IModel _channel { get; init; }
    public IConnection _conn { get; init; }
    
    public MyRabbitMQClient(IConfiguration configuration)
    {
        var rabbitMqServiceName = configuration.GetValue<string>("RabbitMqServiceName");
        ConnectionFactory factory = new ConnectionFactory() {HostName = rabbitMqServiceName};
        _conn = factory.CreateConnection();
        _channel = _conn.CreateModel();
        
        _channel.ExchangeDeclare(exchange: "MediaProcessExchange", type: ExchangeType.Direct);
        _channel.QueueDeclare("processqueue", false, false);
        _channel.QueueBind("processqueue","MediaProcessExchange","process");
    }
    
    public void SendToProcess(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(exchange: "MediaProcessExchange",
            routingKey: "process",
            basicProperties: null,
            body: body);
    }
    
    
}