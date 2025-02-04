using Aether;
using Aether.Abstractions.Storage;
using RickDotNet.Extensions.Base;

namespace ConsoleDemo.Demos;

public class StorageDemo
{
    public static async Task Run()
    {
        var aether = AetherClient.MemoryClient;
        var storage = aether.Storage;

        await storage.Insert(
            "data-1",
            AetherData.Serialize(new Person("Alice"))
        );

        await storage.Insert(
            "data-2",
            AetherData.Serialize(new Person("Bob"))
        );
        
        var data1 = await storage.Get("data-1");
        var data2 = await storage.Get("data-2");

        Console.WriteLine(data1.ValueOrDefault()?.As<Person>());
        Console.WriteLine(data2.ValueOrDefault()?.As<Person>());

        var filter = new FilterCriteria<Person>().WithFilter(x=>x.Name == "Bob");
        var items = await storage.List(filter);
        items.OnSuccess(filteredData =>
        {
            foreach (var item in filteredData)
            {
                Console.WriteLine(item.As<Person>());
            }
        });
    }

    private record Person(string Name);
}