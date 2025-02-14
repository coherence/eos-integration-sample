#if !EOS_DISABLE
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace EosSample
{
    public struct EOSConnection
    {
        public SocketId? SocketId { get; set; }
        public ProductUserId User { get; set; }

        public override int GetHashCode()
        {
            return User.GetHashCode();
        }
    }
}
#endif