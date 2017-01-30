using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.ColorizedConsole.Classification
{
    public interface IClassification
    {
        string ClassificationName { get; set; }
    }
}
