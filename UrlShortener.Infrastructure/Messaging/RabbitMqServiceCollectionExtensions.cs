using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqTransport(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetRequiredSection(RabbitMqOptions.SectionName);

        services.AddOptions<RabbitMqOptions>()
            .Bind(section)
            .Validate(options => !string.IsNullOrWhiteSpace(options.HostName), "RabbitMq:HostName is required.")
            .Validate(options => options.Port is >= 1 and <= 65_535, "RabbitMq:Port must be between 1 and 65535.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.VirtualHost), "RabbitMq:VirtualHost is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.UserName), "RabbitMq:UserName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMq:Password is required.")
            .Validate(
                options => environment.IsDevelopment() ||
                    !string.Equals(options.UserName, "guest", StringComparison.Ordinal) ||
                    !string.Equals(options.Password, "guest", StringComparison.Ordinal),
                "RabbitMq guest credentials are allowed only in Development.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientProvidedName) &&
                    options.ClientProvidedName.Length <= 200,
                "RabbitMq:ClientProvidedName must contain 1 to 200 characters.")
            .Validate(
                options => !options.UseTls || !string.IsNullOrWhiteSpace(options.TlsServerName),
                "RabbitMq:TlsServerName is required when TLS is enabled.")
            .Validate(
                options => options.ConnectionTimeoutMilliseconds is >= 100 and <= 60_000,
                "RabbitMq:ConnectionTimeoutMilliseconds must be between 100 and 60000.")
            .Validate(
                options => options.OperationTimeoutMilliseconds is >= 100 and <= 60_000,
                "RabbitMq:OperationTimeoutMilliseconds must be between 100 and 60000.")
            .Validate(
                options => options.RequestedHeartbeatSeconds is >= 5 and <= 120,
                "RabbitMq:RequestedHeartbeatSeconds must be between 5 and 120.")
            .Validate(
                options => options.NetworkRecoveryIntervalMilliseconds is >= 100 and <= 60_000,
                "RabbitMq:NetworkRecoveryIntervalMilliseconds must be between 100 and 60000.")
            .Validate(
                options => options.ConsumerPrefetchCount is >= 1 and <= 1_000,
                "RabbitMq:ConsumerPrefetchCount must be between 1 and 1000.")
            .Validate(
                options => options.DeliveryLimit is >= 2 and <= 20,
                "RabbitMq:DeliveryLimit must be between 2 and 20.")
            .Validate(
                options => options.RetryBaseDelayMilliseconds is >= 50 and <= 10_000,
                "RabbitMq:RetryBaseDelayMilliseconds must be between 50 and 10000.")
            .Validate(
                options => IsValidTopologyName(options.ExchangeName) &&
                    IsValidTopologyName(options.QueueName) &&
                    IsValidTopologyName(options.RoutingKey) &&
                    IsValidTopologyName(options.DeadLetterExchangeName) &&
                    IsValidTopologyName(options.DeadLetterQueueName) &&
                    IsValidTopologyName(options.DeadLetterRoutingKey) &&
                    !string.Equals(options.ExchangeName, options.DeadLetterExchangeName, StringComparison.Ordinal) &&
                    !string.Equals(options.QueueName, options.DeadLetterQueueName, StringComparison.Ordinal),
                "RabbitMq topology names must be non-empty printable ASCII values no longer than 200 characters.")
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<IEventConsumer, RabbitMqEventConsumer>();

        return services;
    }

    private static bool IsValidTopologyName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 200 &&
        value.All(character => character is >= (char)33 and <= (char)126);
}
