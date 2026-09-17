using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace ZOG.Networking
{
    /// <summary>
    /// Throwaway connection UI for prototyping: start a host, a dedicated server, or join one.
    /// IMGUI on purpose - it needs no prefabs or canvas wiring, and it gets deleted the moment
    /// there is a real front end.
    /// </summary>
    public class ConnectionMenu : MonoBehaviour
    {
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        private string _status = string.Empty;

        private void OnGUI()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null)
            {
                GUI.Label(new Rect(12f, 12f, 420f, 24f), "No NetworkManager in the scene.");
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 260f, 220f));

            if (!manager.IsClient && !manager.IsServer)
            {
                address = GUILayout.TextField(address);

                if (GUILayout.Button("Host (server + local player)"))
                {
                    Configure(manager);
                    _status = manager.StartHost() ? "Hosting" : "Failed to start host";
                }

                if (GUILayout.Button("Client (join)"))
                {
                    Configure(manager);
                    _status = manager.StartClient() ? "Connecting..." : "Failed to start client";
                }

                if (GUILayout.Button("Dedicated server (no local player)"))
                {
                    Configure(manager);
                    _status = manager.StartServer() ? "Server running" : "Failed to start server";
                }
            }
            else
            {
                var role = manager.IsHost ? "Host" : manager.IsServer ? "Server" : "Client";
                GUILayout.Label($"{role} | clients: {manager.ConnectedClientsIds.Count}");

                if (manager.IsClient)
                {
                    GUILayout.Label($"my client id: {manager.LocalClientId}");
                }

                if (GUILayout.Button("Disconnect"))
                {
                    manager.Shutdown();
                    _status = "Disconnected";
                }
            }

            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Label(_status);
            }

            GUILayout.EndArea();
        }

        private void Configure(NetworkManager manager)
        {
            if (manager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                transport.SetConnectionData(address, port);
            }
        }
    }
}
