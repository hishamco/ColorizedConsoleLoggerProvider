using Microsoft.Extensions.Logging.ColorizedConsole;
using Microsoft.Extensions.Logging.ColorizedConsole.Classification;

namespace Microsoft.Extensions.Logging
{
    public static class ColorizedConsoleLoggerExtensions
    {
        public static ILoggerFactory AddColorizedConsole(this ILoggerFactory factory, RegexClassification[] classifications)
        {
            factory.AddProvider(new ColorizedConsoleLoggerProvider((n, l) => l >= LogLevel.Information, false, classifications));
            return factory;
        }
    }
}
