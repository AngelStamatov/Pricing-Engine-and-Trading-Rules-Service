using System.Text.Json.Serialization;
using PricingAndTrading.Infrastructure;
using PricingAndTrading.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializePersistenceAsync();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
