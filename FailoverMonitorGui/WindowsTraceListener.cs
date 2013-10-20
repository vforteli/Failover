using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;

namespace Flexinets.MobileData
{
    public class WindowTraceListener : TraceListener
    {
        private readonly TextBox _textbox;
        internal Boolean Autoscroll = true;

        public WindowTraceListener(TextBox textbox)
        {
            _textbox = textbox;
        }


        public override void Write(string message)
        {
            Action append = delegate
            {
                _textbox.AppendText(message);
                if (Autoscroll)
                {
                    _textbox.ScrollToEnd();
                    _textbox.CaretIndex = _textbox.Text.Length;
                }
            };
            if (_textbox.Dispatcher.Thread != Thread.CurrentThread)
            {
                _textbox.Dispatcher.BeginInvoke(append);
            }
            else
            {
                append();
            }
        }
  

        public override void Write(string message, string category)
        {
            Write(message);
        }


        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
  

        public override void WriteLine(string message, string category)
        {
            WriteLine(message);
        }
    }
}
