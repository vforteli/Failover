using System.Linq;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;

namespace Flexinets.Failover
{
    /// <summary>
    /// Used for monitoring the master server and change operation mode depending on status
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class FailoverNode : IFailoverService
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(FailoverNode));
        public delegate void StateChangedEventHandler(object sender, StateEventArgs e);
        public event StateChangedEventHandler StateChanged;

        private volatile Boolean _running;
        public Boolean Running
        {
            get
            {
                return _running;
            }
        }
        public volatile Boolean Active;
        private readonly object _padlock = new object();
        private readonly Int32 _pollInterval;
        private readonly Int32 _pollTimeout;
        private readonly List<Partner> _partners = new List<Partner>();
        private ServiceHost _host;
        private Int32 _priority;




        /// <summary>
        /// Initialize a new monitor with possible default values
        /// </summary>
        /// <param name="partners"></param>
        /// <param name="pollInterval"></param>
        /// <param name="pollTimeout"></param>
        public FailoverNode(IEnumerable<Uri> partners, Int32 pollInterval = 2000, Int32 pollTimeout = 1000)
        {
            _pollInterval = pollInterval;
            _pollTimeout = pollTimeout;
            foreach (var partner in partners)
            {
                _partners.Add(new Partner{ Uri = partner});
            }
        }


        /// <summary>
        /// Starts polling the master server. Fires event when status changes
        /// </summary>
        public void Start(Uri endpointUri, Int32 priority)
        {
            _priority = priority;
            _running = true;
            Active = false; // Set the initial state to passive and let the failover algoritm figure out what to do

            //PollPartners();
            Task.Factory.StartNew(PollPartners);
            _log.Info("Partner polling started");

            try
            {
                StartListener(endpointUri);
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.Message);
                throw;
            }
            _log.Info("Alive listener started on " + endpointUri);
            _log.Info("Started...");
        }


        /// <summary>
        /// Start the listener web service on the specified local endpoint
        /// </summary>
        /// <param name="endpointUri"></param>
        private void StartListener(Uri endpointUri)
        {
            _host = new ServiceHost(this, new[] {endpointUri});
            _host.AddServiceEndpoint(typeof (IFailoverService), new BasicHttpBinding(), "FailoverPoll");
            var smb = new ServiceMetadataBehavior
            {
                HttpGetEnabled = true,
                MetadataExporter = {PolicyVersion = PolicyVersion.Policy15}
            };
            _host.Description.Behaviors.Add(smb);
            _host.Open();
        }


        /// <summary>
        /// Stops the failover monitor
        /// </summary>
        public void Stop()
        {
            _running = false;
            if (_host != null)
            {
                _host.Close();
            }
            lock (_padlock)
            {
                Monitor.Pulse(_padlock);
            }
            _log.Info("Stopped...");
        }



        /// <summary>
        /// Polls the partners and does something used based on the result
        /// </summary>
        private async void PollPartners()
        {
            while (_running)
            {
                await Task.WhenAll(_partners.Select(partner => Task.Factory.StartNew(() =>
                {
                    var result = GetPartnerStatus(partner.Uri);
                    partner.PartnersVisible = result.PartnersVisible;
                    partner.Alive = result.Alive;
                    partner.Prioity = result.Prioity;
                    partner.Active = result.Active;
                })));

                var nodesAlive = _partners.Count(o => o.Alive) + 1;
                var nodes = _partners.Count + 1;

                _log.Debug("Found " + nodesAlive + " partners alive");

                if (nodesAlive >= (Decimal)nodes / 2)
                {
                    _log.Debug("Wiih, we can haz quorum with " + nodesAlive + " / " + nodes + " nodes alive");

                    if (!_partners.Any(o => o.Active) || _priority > _partners.Max(o => o.Prioity))
                    {
                        _log.Info("Set active");
                        UpdateState(true);
                    }
                    else
                    {
                        UpdateState(false);
                    } 
                }
                else
                {
                    // Roll over and die
                    UpdateState(false);
                    _log.Warn("No quorum with " + nodesAlive + " / " + nodes + " nodes alive");
                }


                lock (_padlock)
                {
                    Monitor.Wait(_padlock, _pollInterval);  // Sleep...
                }
            }
        }


        /// <summary>
        /// Get the status of a partner
        /// </summary>
        /// <param name="partner"></param>
        /// <returns></returns>
        private Partner GetPartnerStatus(Uri partner)
        {
            _log.Debug("Sending ping to " + partner);

            try
            {
                var endpoint = new EndpointAddress(partner + "/FailoverPoll");
                using (var pipeFactory =
                    new ChannelFactory<IFailoverService>(
                        new BasicHttpBinding {OpenTimeout = TimeSpan.FromMilliseconds(_pollTimeout)}, endpoint))
                {

                    var client = pipeFactory.CreateChannel();
                    var result = client.GetStatus();


                    _log.Debug("Partner " + partner + " is alive and sez: " + result);

                    return result;
                }
            }
            catch (Exception sex)
            {
                _log.Debug("No response from " + partner + " - " + sex.Message);
            }

            return new Partner {Alive = false};
        }



        /// <summary>
        /// Fire the event if applicable
        /// </summary>
        /// <param name="state"></param>
        private void UpdateState(Boolean state)
        {
            if (Active == state) return;
            Active = state;
            StateChanged(this, new StateEventArgs(state));
        }



        public Partner GetStatus()
        {
            var bestpartner = _partners.Where(o => o.Alive).OrderByDescending(o => o.Prioity).FirstOrDefault();
            var response = new Partner
            {
                Alive = true,
                Prioity = _priority,
                PartnersVisible = _partners.Count(o => o.Alive),
                BestPartnerVisible = bestpartner != null ? bestpartner.Uri : null,
                Active = Active
            };
            return response;
        }
    }
}
