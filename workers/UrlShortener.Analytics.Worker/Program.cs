using UrlShortener.Analytics.Worker;
using UrlShortener.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddRabbitMqTransport(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<TransportStartupService>();

await builder.Build().RunAsync();
