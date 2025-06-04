using System.Collections.Generic;
using UnityEngine;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Simple runtime registry that maps peer ids to their cube transforms.
    /// The host fills it directly; clients update it from SNAP packets.
    /// </summary>
    internal sealed class CubeRegistry : MonoBehaviour
    {
        public static CubeRegistry Instance { get; private set; }

        public readonly Dictionary<byte, Transform> byPeer = new();

        [SerializeField] private GameObject cubePrefab;   // drag the basic Cube here

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        /// <summary>Returns (or lazily instantiates) the cube for <paramref name="peer"/>.</summary>
        public Transform GetOrCreate(byte peer)
        {
            if (byPeer.TryGetValue(peer, out var t))
                return t;

            // Make a new cube
            var go = cubePrefab != null
                     ? Instantiate(cubePrefab)
                     : GameObject.CreatePrimitive(PrimitiveType.Cube);

            go.name = $"Cube_peer{peer}";
            t = go.transform;
            byPeer[peer] = t;
            return t;
        }
    }
}
