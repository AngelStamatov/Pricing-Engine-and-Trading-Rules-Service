using PricingAndTrading.Infrastructure;
using PricingAndTrading.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializePersistenceAsync();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
