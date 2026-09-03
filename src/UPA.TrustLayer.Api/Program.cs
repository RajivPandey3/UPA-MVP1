using UPA.TrustLayer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton(
    TrustEmitterFactory.Create(builder.Configuration));

builder.Services.AddSingleton<ITrustEmissionAdapter>(
    sp => new TrustEmissionAdapter(
        sp.GetRequiredService<UPA.MVP3.TrustEmission.TrustEmitter>()));

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program { }
