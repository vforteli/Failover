using System.ServiceModel;

namespace Flexinets.Failover
{
    [ServiceContract]
    public interface IFailoverService
    {
        [OperationContract]
        Partner GetStatus();
    }
}
