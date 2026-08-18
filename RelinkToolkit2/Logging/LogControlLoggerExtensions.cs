using System;

using Microsoft.Extensions.Logging;

using RelinkToolkit2.Logging;

namespace RelinkToolkit2.LoggingProvider;

public static class LogControlLoggerExtensions
{
    public static ILoggingBuilder AddLogger(this ILoggingBuilder builder) => builder.AddLogger(new LogControlLoggerConfiguration());

    public static ILoggingBuilder AddLogger(this ILoggingBuilder builder, Action<LogControlLoggerConfiguration> configure)
    {
        var config = new LogControlLoggerConfiguration();
        configure(config);

        return builder.AddLogger(config);
    }

    public static ILoggingBuilder AddLogger(this ILoggingBuilder builder, LogControlLoggerConfiguration config)
    {
        builder.AddProvider(new LogControlLoggerProvider(config));
        return builder;
    }
}
