namespace ChainsAPM.Interfaces
{
    public interface IConnectionHandler
    {
        bool Disconnect();
        bool Recycle();
        bool Flush();
    }
}
