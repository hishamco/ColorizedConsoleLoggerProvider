﻿using Microsoft.Extensions.Logging.ColorizedConsole.Classification;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Console.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Logging.ColorizedConsole
{
    public class ColorizedConsoleLogger : ILogger
    {
        // Writing to console is not an atomic operation in the current implementation and since multiple logger
        // instances are created with a different name. Also since Console is global, using a static lock is fine.
        private static readonly object _lock = new object();
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        private IConsole _console;
        private Func<string, LogLevel, bool> _filter;

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        static ColorizedConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public ColorizedConsoleLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes, RegexClassification[] classifications)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Filter = filter ?? ((category, logLevel) => true);
            IncludeScopes = includeScopes;
            Classifications = classifications;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console = new WindowsLogConsole();
            }
            else
            {
                Console = new AnsiLogConsole(new AnsiSystemConsole());
            }
        }

        public IConsole Console
        {
            get { return _console; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _console = value;
            }
        }

        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _filter = value;
            }
        }

        public bool IncludeScopes { get; set; }

        public RegexClassification[] Classifications { get; set; }

        public string Name { get; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, Name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
        {
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            var logLevelColors = default(ConsoleColors);
            var logLevelString = string.Empty;

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received
            if (!string.IsNullOrEmpty(message))
            {
                logLevelColors = GetLogLevelConsoleColors(logLevel);
                logLevelString = GetLogLevelString(logLevel);
                // category and event id
                logBuilder.Append(_loglevelPadding);
                logBuilder.Append(logName);
                logBuilder.Append("[");
                logBuilder.Append(eventId);
                logBuilder.AppendLine("]");
                // scope information
                if (IncludeScopes)
                {
                    GetScopeInformation(logBuilder);
                }
                // message
                logBuilder.Append(_messagePadding);
                var len = logBuilder.Length;
                logBuilder.AppendLine(message);
                logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logBuilder.AppendLine(exception.ToString());
            }

            if (logBuilder.Length > 0)
            {
                var logMessage = logBuilder.ToString();
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(logLevelString))
                    {
                        // log level string
                        Console.Write(
                            logLevelString,
                            logLevelColors.Background,
                            logLevelColors.Foreground);
                    }

                    // use default colors from here on
                    Console.Write(logMessage, DefaultConsoleColor, DefaultConsoleColor);

                    if (Classifications != null)
                    {
                        foreach (var classification in Classifications)
                        {
                            --System.Console.CursorTop;
                            foreach (Match match in Regex.Matches(message, classification.RegexPattern))
                            {
                                System.Console.CursorLeft = match.Index + _messagePadding.Length;
                                Console.Write(match.Value, null, classification.Color);
                            }
                            System.Console.CursorLeft = message.Length + _messagePadding.Length;
                            ++System.Console.CursorTop;
                            System.Console.CursorLeft = 0;
                        }
                    }

                    // In case of AnsiLogConsole, the messages are not yet written to the console,
                    // this would flush them instead.
                    Console.Flush();
                }
            }

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        private IEnumerable<Match> GetMatches(MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                yield return match;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Filter(Name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return ConsoleLogScope.Push(Name, state);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return new ConsoleColors(ConsoleColor.White, ConsoleColor.Red);
                case LogLevel.Error:
                    return new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red);
                case LogLevel.Warning:
                    return new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black);
                case LogLevel.Information:
                    return new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black);
                case LogLevel.Debug:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                case LogLevel.Trace:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                default:
                    return new ConsoleColors(DefaultConsoleColor, DefaultConsoleColor);
            }
        }

        private void GetScopeInformation(StringBuilder builder)
        {
            var current = ConsoleLogScope.Current;
            string scopeLog = string.Empty;
            var length = builder.Length;

            while (current != null)
            {
                if (length == builder.Length)
                {
                    scopeLog = $"=> {current}";
                }
                else
                {
                    scopeLog = $"=> {current} ";
                }

                builder.Insert(length, scopeLog);
                current = current.Parent;
            }
            if (builder.Length > length)
            {
                builder.Insert(length, _messagePadding);
                builder.AppendLine();
            }
        }

        private struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
        }

        private class AnsiSystemConsole : IAnsiSystemConsole
        {
            public void Write(string message)
            {
                System.Console.Write(message);
            }

            public void WriteLine(string message)
            {
                System.Console.WriteLine(message);
            }
        }
    }
}