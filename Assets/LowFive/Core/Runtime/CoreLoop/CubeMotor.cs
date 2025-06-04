// Assets/LowFive/Core/Runtime/CoreLoop/CubeMotor.cs
using UnityEngine;
using LowFive.Core.CoreLoop;
using LowFive.Core.Input;

[RequireComponent(typeof(Collider))]
public sealed class CubeMotor : MonoBehaviour
{
    /* ───────── config ───────── */
    private const float SPEED = 1f;          // units / second

    /* ───────── public sim helper (used by host) ───────── */
    public static Vector3 Simulate(Vector3 startPos, LFInputStruct inp)
    {
        Vector3 delta = Vector3.zero;

        if (inp.W()) delta += Vector3.forward;
        if (inp.S()) delta += Vector3.back;
        if (inp.A()) delta += Vector3.left;
        if (inp.D()) delta += Vector3.right;

        delta.Normalize();                               // diagonal still 1×
        delta *= SPEED * TickTimer.SECONDS_PER_TICK;

        if (inp.LMB()) delta += Vector3.up;              // 1-unit jump

        return startPos + delta;
    }

    /* ───────── live instance movement ───────── */
    void OnEnable()
    {
        if (CoreNetManager.Instance != null)
            CoreNetManager.Instance.Tick += OnTick;
    }

    void OnDisable()
    {
        if (CoreNetManager.Instance != null)
            CoreNetManager.Instance.Tick -= OnTick;
    }

    void OnTick(uint tick, LFInputStruct inp)
    {
        transform.position = Simulate(transform.position, inp);
    }
}
