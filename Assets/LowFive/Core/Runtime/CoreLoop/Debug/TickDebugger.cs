using UnityEngine;
using LowFive.Core.CoreLoop;

public sealed class TickDebugger : MonoBehaviour
{
    void Start() => CoreNetManager.Instance.Tick += OnTick;
    void OnTick(uint t, LowFive.Core.Input.LFInputStruct input) =>
        Debug.Log($"[TickDbg] {t}  0x{input.packed:X16}");
    void OnDestroy()
    {
        if (CoreNetManager.Instance != null)
            CoreNetManager.Instance.Tick -= OnTick;
    }
}
