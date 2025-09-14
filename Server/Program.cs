using SDK;
using SDK.InstanceTools;

namespace Server;

class Program {
    
    static void Main(string[] args) {
        Console.CancelKeyPress += (_, _) => Core.SaveInstances();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Core.SaveInstances();
        
        Core.LoadPlugins();
        SDK.Server.Start();
        
        while (true) Loop();
    }

    static void Loop() {
        InstanceEvents.Tick();
        Core.UpdateInstances();
        Thread.Sleep(1);
    }

}
