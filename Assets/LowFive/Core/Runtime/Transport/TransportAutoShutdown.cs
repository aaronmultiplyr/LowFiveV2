using UnityEngine;
using LowFive.Core.Transport;

namespace LowFive.Core.Transport
{
    public sealed class TransportAutoShutdown : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] transportRefs;

        private void OnApplicationQuit()
        {
            // explicit list first
            foreach (var mb in transportRefs)
                if (mb is INetTransport t) t.Shutdown();

            // catch-all without the obsolete call
#if UNITY_2022_2_OR_NEWER   // Unity 6 included
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
#else
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
#endif
                if (mb is INetTransport nt) nt.Shutdown();
        }
    }
}
