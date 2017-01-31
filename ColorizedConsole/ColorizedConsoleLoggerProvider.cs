using Microsoft.Extensions.Logging.ColorizedConsole.Classification;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.ColorizedConsole
{
    public class ColorizedConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ColorizedConsoleLogger> _loggers = new ConcurrentDictionary<string, ColorizedConsoleLogger>();

        private readonly Func<string, LogLevel, bool> _filter;
        private IConsoleLoggerSettings _settings;
        private RegexClassification[] _classifications;

        public ColorizedConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            _filter = filter;
            _settings = new ConsoleLoggerSettings()
            {
                IncludeScopes = includeScopes,
            };
        }

        public ColorizedConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes, RegexClassification[] classification)
            : this(filter, includeScopes)
        {

            _classifications = classification;
        }

        public ColorizedConsoleLoggerProvider(IConsoleLoggerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _settings = settings;

            if (_settings.ChangeToken != null)
            {
                _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private void OnConfigurationReload(object state)
        {
            // The settings object needs to change here, because the old one is probably holding on
            // to an old change token.
            _settings = _settings.Reload();

            foreach (var logger in _loggers.Values)
            {
                logger.Filter = GetFilter(logger.Name, _settings);
                logger.IncludeScopes = _settings.IncludeScopes;
            }

            // The token will change each time it reloads, so we need to register again.
            if (_settings?.ChangeToken != null)
            {
                _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        private ColorizedConsoleLogger CreateLoggerImplementation(string name)
        {
            return new ColorizedConsoleLogger(name, GetFilter(name, _settings), _settings.IncludeScopes, _classifications);
        }

        private Func<string, LogLevel, bool> GetFilter(string name, IConsoleLoggerSettings settings)
        {
            if (_filter != null)
            {
                return _filter;
            }

            if (settings != null)
            {
                foreach (var prefix in GetKeyPrefixes(name))
                {
                    LogLevel level;
                    if (settings.TryGetSwitch(prefix, out level))
                    {
                        return (n, l) => l >= level;
                    }
                }
            }

            return (n, l) => false;
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }

        public void Dispose()
        {
        }
    }
}