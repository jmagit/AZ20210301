using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace demo.servicebus {
    public static class Extensions {
        public static T As<T>(this ServiceBusMessage message) where T : class {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
        }
        public static ServiceBusMessage AsMessage(this object obj) {
            return new ServiceBusMessage(JsonConvert.SerializeObject(obj));
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
            return $"Item {Id}: {Mensaje} ({Categoria})";
        }
    }

    public class Program {
        static string ServiceBusConnectionString = "";
        static ServiceBusClient srv = null;
        static string QueueOrTopicName = "";
        static string SubscriptionName = "";
        static int NumeroDeEnvios = 10;
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
                        Console.WriteLine($"Abandon: {args[i + 1]}s");
                        AbandonarMultiplosDe = int.Parse(args[i + 1]);
                        break;

                }
            srv = new ServiceBusClient(ServiceBusConnectionString);
        }
    }
}
