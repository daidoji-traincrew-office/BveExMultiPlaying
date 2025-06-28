using BveExMultiPlaying.Server.Hubs;

namespace BveExMultiPlaying.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddSignalR();
       
        // Add the TrainHub service
        var app = builder.Build();
        app.MapHub<TrainHub>("/hubs/train");

        app.Run();
    }
}