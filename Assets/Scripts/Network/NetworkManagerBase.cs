using System;

namespace ARPeerToPeerSample.Network
{
    public abstract class NetworkManagerBase : INetworkManager
    {
        // todo: polling system would be a better implementation, but this is fine for now
        public Action<string> ServiceFound;
        public Action<byte[]> MessageReceived;
        public Action ConnectionEstablished;

        public enum NET_MESSAGE_TYPES { SendColor, SetHost, SendMovement, ParticleRPC, SpawnObject, SpawnObjectReq };

        public virtual void Connect()
        {
            throw new NotImplementedException();
        }

        public virtual void SendMessage(byte[] message)
        {
            throw new NotImplementedException();
        }

        public virtual void Start()
        {
            throw new NotImplementedException();
        }
    }
}
