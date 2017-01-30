using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.ColorizedConsole.Classification
{
    public class RegexClassification : IClassification, IColorizer
    {
        public string ClassificationName { get; set; }

        public string RegexPattern { get; set; }

        public bool IgnoreCase { get; set; }

        public ConsoleColor Color { get; set; } = ConsoleColor.Gray;
    }
}
