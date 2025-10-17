using Hs_HealthChecks.Extensions;
using Hs_HealthChecks.Models;
using MongoDB.Driver;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddStartupHealthChecks(hc =>
{
    hc.AddSqlServer(builder.Configuration.GetConnectionString("SqlMain")!, name: "SQL:MainDb");
    hc.AddSqlServer(builder.Configuration.GetConnectionString("SqlReporting")!, name: "SQL:ReportingDb");
    hc.AddMongoDb(sp => builder.Configuration.GetConnectionString("MongoLogs")!, name: "Mongo:Logs");

    hc.AddRedis(builder.Configuration.GetConnectionString("RedisCache")!, name: "Redis:Cache");


    hc.AddUrlGroup(new Uri("https://example.com/api/ping"), name: "ExternalAPI");

});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/healthchecks.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run HealthChecks before starting application
await app.Services.ValidateStartupHealthChecksAsync(new HealthCheckRunnerOptions
{
    RetryCount = 3,
    DelaySeconds = 5,
    TimeoutSeconds = 10,
    MaxParallelism = 4
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
