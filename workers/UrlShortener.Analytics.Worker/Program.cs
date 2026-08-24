using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrlShortener.Analytics.Worker;
using UrlShortener.Analytics.Worker.Maintenance;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Services;
using UrlShortener.Infrastructure.Configuration;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

var persistenceSection = builder.Configuration.GetRequiredSection(PersistenceOptions.SectionName);
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Analytics persistence requires ConnectionStrings:SqlServer. " +
        "Provide it through environment-specific configuration or ConnectionStrings__SqlServer.");
}

builder.Services.AddOptions<PersistenceOptions>()
    .Bind(persistenceSection)
    .Validate(
        options => options.CommandTimeoutSeconds is >= 1 and <= 300,
        "Persistence:CommandTimeoutSeconds must be between 1 and 300.")
    .ValidateOnStart();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.CommandTimeout(persistenceOptions.CommandTimeoutSeconds));
});

builder.Services.AddRabbitMqTransport(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IClickEventPersistence, ClickEventPersistence>();
builder.Services.AddScoped<ClickEventHandler>();
builder.Services.AddMaintenanceScheduling(builder.Configuration);
builder.Services.AddHostedService<TransportStartupService>();
builder.Services.AddHostedService<AnalyticsWorkerService>();

await builder.Build().RunAsync();
