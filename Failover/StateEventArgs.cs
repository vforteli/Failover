using System;

namespace Flexinets.Failover
{
    public class StateEventArgs : EventArgs
    {
        public StateEventArgs(Boolean status)
        {
            Status = status;
        }
        public Boolean Status
        {
            get;
            set;
        }
    }  
}
