using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ColorizedConsole.Classification;
using System;

class Program
{
    private static readonly ILogger _logger;

    static Program()
    {
        var factory = new LoggerFactory();
        _logger = factory.CreateLogger<Program>();
        var classifications = new[]
        {
                new RegexClassification
                {
                    ClassificationName = "URL",
                    IgnoreCase = false,
                    RegexPattern = @"(http|https|ftp|)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]",
                    Color = ConsoleColor.Magenta
                },
                new RegexClassification
                {
                    ClassificationName = "SQL",
                    IgnoreCase = true,
                    RegexPattern = @"\""Select (Distinct )?(Top \d+ )?(\*|[a-zA-Z]+(,[a-zA-Z]+)?) From [a-zA-Z]+\""",
                    Color = ConsoleColor.Yellow
                },
                new RegexClassification
                {
                    ClassificationName = "DateTime",
                    RegexPattern = @"\d{1,2}\/\d{2}\/\d{4} \d{1,2}:\d{2}:\d{2} [paPA][Mm]",
                    Color = ConsoleColor.Green
                }
            };
        factory.AddColorizedConsole(classifications);
    }

    static void Main(string[] args)
    {
        _logger.LogInformation($"The application starting @ {DateTime.Now}.");
        _logger.LogInformation($"-= Colorized Console Logger Provider =-");
        _logger.LogInformation("GitHub: https://github.com/hishamco/ColorizedConsoleLoggerProvider");
        _logger.LogWarning("The query \"Select * From Employees\" takes long execution time.");
        _logger.LogError("The query \"Select Top 3 Name From Employees\" causes an error.");
        _logger.LogInformation("Executing the query \"Select Distinct City,Address From Employees\" takes long execution time.");
        _logger.LogInformation($"The application stopping @ {DateTime.Now}.");
        Console.ReadKey();
    }
}
