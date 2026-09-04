using System.Text.Json.Serialization;
using UPA.TrustLayer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

builder.Services.AddSingleton(
    TrustEmitterFactory.Create(builder.Configuration));

builder.Services.AddSingleton<ITrustEmissionAdapter>(
    sp => new TrustEmissionAdapter(
        sp.GetRequiredService<UPA.MVP3.TrustEmission.TrustEmitter>()));

builder.Services.AddSingleton<ITrustVerificationService, TrustVerificationService>();
builder.Services.AddSingleton<ITrustVerificationAdapter, TrustVerificationAdapter>();
builder.Services.AddSingleton<ITrustInspectionService, TrustInspectionService>();
builder.Services.AddSingleton<ITrustInspectionAdapter, TrustInspectionAdapter>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program { }
