using ConsoleDemo.Demos;

//await Memory.Run();
//await Nats.Run();

//await HostDemo.Run();
//await HostDemo.Run(HostDemo.ExampleType.ExampleTwo);
//await HostDemo.Run(HostDemo.ExampleType.ExampleThree);

//await StorageDemo.Run()
await WildCardDemo.Run();

Console.WriteLine("Exiting in 5 seconds...");
await Task.Delay(5000);

