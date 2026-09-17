using UnityEngine;

namespace ZOG.Player
{
    /// <summary>
    /// Follows the local player from above. Deliberately simple - this gets replaced by a
    /// Cinemachine rig once the camera needs framing rules, but a prototype does not need one.
    /// </summary>
    public class TopDownCamera : MonoBehaviour
    {
        private static TopDownCamera _instance;

        [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -8f);
        [SerializeField] private float followLerp = 8f;

        private Transform _target;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>Called by the local player when it spawns.</summary>
        public static void SetTarget(Transform target)
        {
            if (_instance != null)
            {
                _instance._target = target;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var desired = _target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);
            transform.LookAt(_target.position);
        }
    }
}
