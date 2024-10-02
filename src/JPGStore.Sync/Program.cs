using Cardano.Sync;
using Cardano.Sync.Reducers;
using JPGStore.Data.Models;
using JPGStore.Sync.Reducers;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCardanoIndexer<JPGStoreSyncDbContext>(builder.Configuration, builder.Configuration.GetValue("DatabaseQueryTimeout", 60 * 60 * 10));

// Reducers
builder.Services.AddSingleton<IReducer, ListingByAddressReducer>();

WebApplication app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
JPGStoreSyncDbContext dbContext = scope.ServiceProvider.GetRequiredService<JPGStoreSyncDbContext>();
dbContext.Database.Migrate();

app.Run();
