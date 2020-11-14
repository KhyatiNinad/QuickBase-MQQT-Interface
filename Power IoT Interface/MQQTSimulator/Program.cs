using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;

namespace MQQTSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sender");
            Console.WriteLine("Press Enter to send data");
            Console.ReadLine();

            const string EXCHANGE_NAME = "MQQTExchange";

            ConnectionFactory factory = new ConnectionFactory();

            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {
                    factory.HostName = "localhost";

                    channel.ExchangeDeclare(EXCHANGE_NAME, ExchangeType.Fanout, true, false, null);

                    channel.QueueDeclare(queue: "MQQTQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                    MQQTMessage msg = new MQQTMessage
                    {
                        TimeStamp = DateTime.UtcNow.ToString("s") + "Z",
                        EquipmentId = "1",
                        Tag = "gasturbine/power",
                        Value = "10.22"
                    };
                    string message = JsonConvert.SerializeObject(msg);
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: EXCHANGE_NAME,
                                         routingKey: "QB",
                                         basicProperties: null,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);
                    Console.ReadLine();
                    
                }
            }

            Console.WriteLine("Done Sending");
        }


        public class MQQTMessage
        {
            public String TimeStamp { get; set; }
            public String Tag { get; set; }
            public String EquipmentId { get; set; }
            public String Value { get; set; }

        }
    }
}
