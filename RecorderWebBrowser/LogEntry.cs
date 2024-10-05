using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecorderWebBrowser
{
    public class LogEntry
    {
        public int StepNumber { get; set; }
        public string Attribute { get; set; }
        public string AttributeValue { get; set; }
        public string ValueType { get; set; }
        public string Value { get; set; }
        public string Action { get; set; }
    }
}
