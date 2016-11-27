using System.ServiceModel;

namespace WCFTestService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITestInputService" in both code and config file together.
    [ServiceContract]
    public interface ITestInputService
    {
        [OperationContract]
        void DoWork();
    }
}
