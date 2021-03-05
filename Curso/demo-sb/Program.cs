using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace demo.servicebus {
    public static class ServiceBusExtensions {
        public static ServiceBusMessage AsMessage(this object obj) {
            var msg = new ServiceBusMessage(JsonConvert.SerializeObject(obj));
            msg.ContentType = "application/json";
            return msg;
        }

        public static T As<T>(this ServiceBusReceivedMessage message) where T : class {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
        }
        public static bool Any(this IList<ServiceBusMessage> collection) {
            return collection != null && collection.Count > 0;
        }
    }

    public class Item {
        static Random rnd = new Random();
        public static string[] Categorias = { "Todos", "Profesores", "Alumnos" };
        public static string[] Niveles = { "Avanzado", "Principiante" };
        public int Id { get; set; }
        public string Mensaje { get; set; }
        public string Categoria { get; set; }
        public string Nivel { get; set; }
        public Item() {
            Categoria = Categorias[rnd.Next(0, 3)];
            Nivel = Niveles[rnd.Next(0, 2)];
        }

        public override string ToString() {
            return $"Item {Id}: {Mensaje} ({Categoria}:{Nivel})";
        }
    }

    public class Program {
        static string ServiceBusConnectionString = "Endpoint=sb://cursoeverisprofe5.servicebus.windows.net/;SharedAccessKeyName=Cliente;SharedAccessKey=JIlK7arPS3ccaORg9HrOz10ucRqFlmJS9NvDRSwGkG0=";
        static ServiceBusClient srv = null;
        static string QueueOrTopicName = "";
        static string SubscriptionName = "";
        static int NumeroDeEnvios = 1;
        static int AbandonarMultiplosDe = 0;
        static int TimeToLive = 0;
        static Random rnd = new Random();
        static void Main(string[] args) {
            for (int i = 1; i < args.Length; i++)
                switch (args[i].ToLower()) {
                    case "-q":
                        Console.WriteLine($"Name: {args[i + 1]}");
                        QueueOrTopicName = args[i + 1];
                        break;
                    case "-s":
                        Console.WriteLine($"Subscription: {args[i + 1]}");
                        SubscriptionName = args[i + 1];
                        break;
                    case "-n":
                        Console.WriteLine($"Mensajes: {args[i + 1]}");
                        NumeroDeEnvios = int.Parse(args[i + 1]);
                        break;
                    case "-t":
                        Console.WriteLine($"TimeToLive: {args[i + 1]}s");
                        TimeToLive = int.Parse(args[i + 1]);
                        break;
                    case "-a":
                        Console.WriteLine($"Abandon: {args[i + 1]}");
                        AbandonarMultiplosDe = int.Parse(args[i + 1]);
                        break;

                }
            srv = new ServiceBusClient(ServiceBusConnectionString);
            switch (args[0]) {
                default:
                case "send":
                    if (NumeroDeEnvios == 1)
                        SendMessageAsync().GetAwaiter().GetResult();
                    break;
                case "receive":
                    ReceiveMessageAsync().GetAwaiter().GetResult();
                    break;
            }
        }

        static async Task SendMessageAsync() {
            ServiceBusSender sender = srv.CreateSender(QueueOrTopicName);
            try {
                string messageBody = $"Mensaje {DateTime.Now:mm:ss:ff}: valor {rnd.Next(1, 100)}.";
                var item = new Item() { Id = 1, Mensaje = messageBody };
                var message = item.AsMessage();
                message.To = item.Categoria;
                message.Subject = item.Nivel;
                message.ApplicationProperties.Add("nivel", item.Nivel);
                message.MessageId = $"{DateTime.Now.Minute}-1";
                message.SessionId = Guid.NewGuid().ToString();
                if (TimeToLive > 0)
                    message.TimeToLive = TimeSpan.FromSeconds(TimeToLive);
                await sender.SendMessageAsync(message);
                Console.WriteLine($"Sending message: {message.MessageId} - {item}");
            } catch (Exception exception) {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }

            await sender.CloseAsync();
            Console.WriteLine("Messages was sent successfully.");

        }
        static async Task ReceiveMessageAsync() {
            Console.WriteLine("=========================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("=========================================================");
            var options = new ServiceBusProcessorOptions {
                AutoCompleteMessages = false
            };
            await using ServiceBusProcessor processor = string.IsNullOrWhiteSpace(SubscriptionName) ?
                srv.CreateProcessor(QueueOrTopicName, options) :
                srv.CreateProcessor(QueueOrTopicName, SubscriptionName, options);
            processor.ProcessMessageAsync += async (arg) => {
                // Console.WriteLine($"Received message: SequenceNumber: {arg.Message.SequenceNumber} Body: {Encoding.UTF8.GetString(arg.Message.Body)}");
                Console.WriteLine($"Received message: SequenceNumber: {arg.Message.SequenceNumber} Body: {arg.Message.As<Item>().ToString()}");
                if (AbandonarMultiplosDe > 0 && arg.Message.SequenceNumber % AbandonarMultiplosDe == 0) {
                    await arg.AbandonMessageAsync(arg.Message);
                } else
                    await arg.CompleteMessageAsync(arg.Message);
            };
            processor.ProcessErrorAsync += ExceptionReceivedHandler;
            await processor.StartProcessingAsync();
            Console.Read();
            Console.WriteLine("Exit processor...");
        }

        static Task ExceptionReceivedHandler(ProcessErrorEventArgs ex) {
            Console.WriteLine($"Message handler encountered an exception {ex.Exception}.");
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- ErrorSource: {ex.ErrorSource}");
            Console.WriteLine($"- Entity Path: {ex.EntityPath}");
            Console.WriteLine($"- Fully Qualified Namespace: {ex.FullyQualifiedNamespace}");
            return Task.CompletedTask;
        }

    }
}
