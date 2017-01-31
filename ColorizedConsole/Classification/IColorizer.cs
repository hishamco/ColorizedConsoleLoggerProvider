using System;

namespace Microsoft.Extensions.Logging.ColorizedConsole.Classification
{
    public interface IColorizer
    {
        ConsoleColor Color { get; set; }
    }
}
