using Microsoft.EntityFrameworkCore;

using RBTB_ServiceStrategy.Background;
using RBTB_ServiceStrategy.Database;
using RBTB_ServiceStrategy.Domain.Options;
using RBTB_ServiceStrategy.Notification.Telergam;
using RBTB_ServiceStrategy.Strategies;

var builder = WebApplication.CreateBuilder(args);

#region [Options]
builder.Services.Configure<TelegramOption>(builder.Configuration.GetSection("TelegramOption"));
builder.Services.Configure<LevelStrategyOption>(builder.Configuration.GetSection("LevelStrategyOption"));
#endregion

#region [Services]
builder.Services.AddTransient<TelegramClient>();
builder.Services.AddTransient<LevelStrategy>();
#endregion

#region [Hosteds]
builder.Services.AddHostedService<StrategyLevelHostedService>();
#endregion

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<AnaliticContext>(
	options => { options.UseNpgsql( builder.Configuration.GetConnectionString( "DbConnection" ) ); }, ServiceLifetime.Transient );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();

app.Run();