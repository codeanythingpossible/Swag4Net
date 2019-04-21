using System;
using System.Threading.Tasks;
using Swag4Net.RestClient;
using Swag4Net.RestClient.Results;
using Swag4Net.Sample.Generated;

namespace Swag4Net.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
          var client = new PetstoreClient("https://petstore.swagger.io/v2/");

          var rs1 = await client.GetOrderById(2);
          
          await client.GetOrderById(2)
            .OnSuccess(order =>
            {
              Console.WriteLine($"Order({order.Id}): {order.Quantity}");
              return Task.CompletedTask;
            })
            .OnError(error =>
            {
              Console.WriteLine($"Error: {error}");
              return Task.CompletedTask;
            });

          await client.FindPetsByStatus(new []{"available"})
            .OnSuccess(pets =>
            {
              foreach (var pet in pets) Console.WriteLine($"- {pet.Name}");
              return Task.CompletedTask;
            })
            .OnError(error =>
            {
              Console.WriteLine($"Error: {error}");
              return Task.CompletedTask;
            });

          
          Console.WriteLine("Press any key to quit ...");
          Console.ReadKey(true);
        }
    }
}
