using System.Configuration;
using System.Linq;
using Flexinets.Failover;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using log4net;

namespace Flexinets.MobileData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));
        private readonly WindowTraceListener _listener;
        private FailoverNode _failoverNode;


        public MainWindow()
        {
            InitializeComponent();
            
            log4net.Config.XmlConfigurator.Configure(); 
            _listener = new WindowTraceListener(textBox1);
            Trace.Listeners.Add(_listener);
            SetStatusLabel(StatusLabel, "Not running");

            Priority.Text = ConfigurationManager.AppSettings["PartnerPriority"] ?? "50";
        }


        private void FailoverNodeStateChanged(object sender, StateEventArgs e)
        {
            var state = e.Status ? "Active" : "Passive";
            _log.Warn("Oh nos, state changed to " + state);
            SetStatusLabel(StatusLabel, state);
        }


        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            textBox1.Clear();
        }


        private void Button_StartStop(object sender, RoutedEventArgs e)
        {
            if (_failoverNode == null || !_failoverNode.Running)
            {
                var partners = ConfigurationManager.AppSettings["FailoverPartners"].Split(new[] {','}).Select(o => new Uri(o));
                var endpoint = new Uri(ConfigurationManager.AppSettings["HostingEndpoint"]);

                _failoverNode = new FailoverNode(partners);
                _failoverNode.StateChanged += FailoverNodeStateChanged;
                var endpointUri = endpoint;
                _failoverNode.Start(endpointUri, Convert.ToInt32(Priority.Text));
                SetStatusLabel(StatusLabel, _failoverNode.Active);
                button1.Content = "Stop";
            }
            else
            {
                _failoverNode.Stop();
                SetStatusLabel(StatusLabel, "Not running");
                button1.Content = "Start";
            }
        }




        private void AutoScroll_Click(object sender, RoutedEventArgs e)
        {
            if (_listener != null)
            {
                _listener.Autoscroll = AutoScroll.IsChecked.HasValue && AutoScroll.IsChecked.Value;
            }
        }


        private void SetStatusLabel(ContentControl label, Object value)
        {
            Action append = delegate
            {
                label.Content = value.ToString();
            };
            if (label.Dispatcher.Thread != Thread.CurrentThread)
            {
                label.Dispatcher.BeginInvoke(append);
            }
            else
            {
                append();
            }
        }
    }
}
