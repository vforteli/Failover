using System;
using System.Diagnostics;

namespace Flexinets.MobileData
{
    public class TextTraceListener : TextWriterTraceListener
    {
        public TextTraceListener(String fileName)
            : base(fileName)
        {
        }
        public override void WriteLine(string message)
        {
            base.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss fff") + " :: " + message);
        }
        public override void WriteLine(string message, string category)
        {
            base.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss fff") + " :: " + category + " :: " + message);
        }
    }
}
