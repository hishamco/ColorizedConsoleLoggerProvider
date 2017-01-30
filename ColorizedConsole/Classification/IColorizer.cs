using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.ColorizedConsole.Classification
{
    public interface IColorizer
    {
        ConsoleColor Color { get; set; }
    }
}
