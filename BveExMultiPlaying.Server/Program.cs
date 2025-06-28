using BveExMultiPlaying.Server.Hubs;

namespace BveExMultiPlaying.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        
        app.MapHub<TrainHub>("/hubs/train");

        app.Run();
    }
}