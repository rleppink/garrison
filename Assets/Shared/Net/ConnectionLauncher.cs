using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace Garrison.Shared.Net
{
    public sealed class ConnectionLauncher : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UDPTransport transport;
        [SerializeField] private string defaultAddress = "127.0.0.1";
        [SerializeField] private ushort defaultPort = 5000;

        public string DefaultAddress => defaultAddress;
        public ushort DefaultPort => defaultPort;

        [ContextMenu("Host")]
        public void Host()
        {
            ApplyEndpoint(defaultAddress, defaultPort);
            networkManager.StartHost();
        }

        public void JoinDefault()
        {
            JoinByAddress(defaultAddress, defaultPort);
        }

        public void JoinByAddress(string ip, ushort port)
        {
            ApplyEndpoint(ip, port);
            networkManager.StartClient();
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            networkManager.StopClient();
            networkManager.StopServer();
        }

        private void ApplyEndpoint(string ip, ushort port)
        {
            transport.address = ip;
            transport.serverPort = port;
        }
    }
}
