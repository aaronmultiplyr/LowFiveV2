// Assets/LowFive/Core/Runtime/CoreLoop/CubeMotor.cs
using UnityEngine;
using LowFive.Core.CoreLoop;
using LowFive.Core.Input;

[RequireComponent(typeof(Collider))]
public sealed class CubeMotor : MonoBehaviour
{
    private void OnEnable()
    {
        CoreNetManager.Instance.Tick += OnTick;
    }

    private void OnDisable()
    {
        if (CoreNetManager.Instance != null)
            CoreNetManager.Instance.Tick -= OnTick;
    }

    private void OnTick(uint _, LFInputStruct inp)
    {
        const float speed = 1f;                 // units per second
        Vector3 delta = Vector3.zero;

        if (inp.W()) delta += Vector3.forward;
        if (inp.S()) delta += Vector3.back;
        if (inp.A()) delta += Vector3.left;
        if (inp.D()) delta += Vector3.right;

        delta.Normalize();                      // diagonal = 1× speed
        delta *= speed * TickTimer.SECONDS_PER_TICK;

        if (inp.LMB()) delta += Vector3.up;     // simple 1-unit hop

        transform.position += delta;
    }
}
