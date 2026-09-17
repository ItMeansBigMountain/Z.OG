using Unity.Netcode;
using UnityEngine;

namespace ZOG.Networking
{
    /// <summary>
    /// Server-side visibility filter: a client is only sent this object while it is inside that
    /// client's awareness radius.
    ///
    /// This is the network half of the fog-of-war rule in CLAUDE.md. Hiding other players in the
    /// renderer alone is cosmetic - the positions still arrive on the wire and any modified client
    /// can draw them. A client that never receives a position cannot reveal it. It is also what
    /// keeps bandwidth flat as the player count on one server grows, since each client's traffic
    /// scales with players nearby rather than players total.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class ProximityInterest : NetworkBehaviour
    {
        [SerializeField] private float visibilityRadius = 40f;
        [SerializeField] private float reevaluateInterval = 0.25f;

        private float _nextEvaluation;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            // Decides visibility at spawn time; the periodic sweep below handles movement after.
            NetworkObject.CheckObjectVisibility = IsVisibleTo;
        }

        private void Update()
        {
            if (!IsServer || Time.time < _nextEvaluation)
            {
                return;
            }

            _nextEvaluation = Time.time + reevaluateInterval;
            Reevaluate();
        }

        private void Reevaluate()
        {
            var manager = NetworkManager;
            if (manager == null)
            {
                return;
            }

            foreach (var clientId in manager.ConnectedClientsIds)
            {
                // The owner always sees their own player, and a host is the server - there is
                // nothing to withhold from either.
                if (clientId == OwnerClientId || clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                var shouldSee = IsVisibleTo(clientId);
                var canSee = NetworkObject.IsNetworkVisibleTo(clientId);

                if (shouldSee && !canSee)
                {
                    NetworkObject.NetworkShow(clientId);
                }
                else if (!shouldSee && canSee)
                {
                    NetworkObject.NetworkHide(clientId);
                }
            }
        }

        private bool IsVisibleTo(ulong clientId)
        {
            if (clientId == OwnerClientId || clientId == NetworkManager.ServerClientId)
            {
                return true;
            }

            var manager = NetworkManager;
            if (manager == null || !manager.ConnectedClients.TryGetValue(clientId, out var client))
            {
                return false;
            }

            var observer = client.PlayerObject;
            if (observer == null)
            {
                return false;
            }

            var flatDelta = observer.transform.position - transform.position;
            flatDelta.y = 0f;
            return flatDelta.sqrMagnitude <= visibilityRadius * visibilityRadius;
        }
    }
}
