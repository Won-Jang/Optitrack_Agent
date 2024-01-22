/* 
Copyright © 2023 Boystown Research Hospital

Modified By: Won Jang (won DOT jang AT boystown DOT org)
*/
namespace Agent
{
    interface BTSocket
    {
        void Start();
        void Stop();
        event ServerHandlePacketData OnDataReceived;
        void SendImmediateToAll(byte[] data);
    }
}
