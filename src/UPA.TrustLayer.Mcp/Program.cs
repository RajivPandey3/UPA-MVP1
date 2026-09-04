using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using UPA.TrustLayer.Api.Services;
using UPA.TrustLayer.Mcp.Tools;

namespace UPA.TrustLayer.Mcp;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton(TrustEmitterFactory.Create(config));
        services.AddSingleton<ITrustEmissionAdapter, TrustEmissionAdapter>();
        services.AddSingleton<ITrustVerificationService, TrustVerificationService>();
        services.AddSingleton<ITrustVerificationAdapter, TrustVerificationAdapter>();
        services.AddSingleton<ITrustInspectionService, TrustInspectionService>();
        services.AddSingleton<ITrustInspectionAdapter, TrustInspectionAdapter>();

        services.AddMcpServer(options => {
            options.ServerInfo = new Implementation { Name = "UPA.TrustLayer.Mcp", Version = "1.1.0" };
        })
        .WithTools<EmitTrustTool>()
        .WithTools<VerifyTrustTool>()
        .WithTools<InspectTrustTool>()
        .WithStdioServerTransport();

        var sp = services.BuildServiceProvider();
        var server = sp.GetRequiredService<McpServer>();
        
        await server.RunAsync(default);
    }
}
