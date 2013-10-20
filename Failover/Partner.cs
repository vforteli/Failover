using System;
using System.Runtime.Serialization;

namespace Flexinets.Failover
{
    [DataContract]
    public class Partner
    {
        [DataMember]
        public Uri Uri { get; set; }

        [DataMember]
        public Int32 Prioity { get; set; }

        [DataMember]
        public Boolean Alive { get; set; }

        [DataMember]
        public Int32 PartnersVisible { get; set; }

        [DataMember]
        public Uri BestPartnerVisible { get; set; }

        [DataMember]
        public Boolean Active { get; set; }
    }
}
