using ConsoleDemo.Demos;

//await InMemory.Run();
//await Nats.Run();

//await HostDemo.Run();
await HostDemo.Run(HostDemo.ExampleType.ExampleTwo);
//await HostDemo.Run(HostDemo.ExampleType.ExampleThree);

Console.WriteLine("Exiting in 5 seconds...");
await Task.Delay(5000);

