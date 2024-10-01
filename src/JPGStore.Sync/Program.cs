using Cardano.Sync;
using Cardano.Sync.Reducers;
using JPGStore.Sync.Reducers;
using JPGStore.Sync.Workers;
using JPGStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using JPGStore.Data.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCardanoIndexer<JPGStoreSyncDbContext>(builder.Configuration, builder.Configuration.GetValue("DatabaseQueryTimeout", 60 * 60 * 10));
builder.Services.AddSingleton<IReducer, ListingByAddressReducer>();
builder.Services.AddSingleton<JPGStoreDataService>();

builder.Services.AddHostedService<VirtualMempoolWorker>();

WebApplication app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
JPGStoreSyncDbContext dbContext = scope.ServiceProvider.GetRequiredService<JPGStoreSyncDbContext>();
dbContext.Database.Migrate();

app.Run();
