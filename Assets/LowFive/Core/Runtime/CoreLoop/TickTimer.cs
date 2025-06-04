// Assets/LowFive/Core/Runtime/CoreLoop/TickTimer.cs
using UnityEngine;

namespace LowFive.Core.CoreLoop
{
    /// <summary>Fixed-rate tick counter (deterministic step time).</summary>
    public sealed class TickTimer
    {
        public const int TICKS_PER_SECOND = 60;
        public const float SECONDS_PER_TICK = 1f / TICKS_PER_SECOND;

        private double _accum;     // accumulated unprocessed time
        public uint tick;       // public tick counter

        /// <summary>
        /// Accumulate elapsedTime; return true as long as at least one full tick remains.
        /// Call this every Update() and run your simulation while Step() yields true.
        /// </summary>
        public bool Step(float elapsed)
        {
            _accum += elapsed;
            if (_accum < SECONDS_PER_TICK)
                return false;

            _accum -= SECONDS_PER_TICK;
            tick++;
            return true;
        }

        public void Reset()
        {
            _accum = 0;
            tick = 0;
        }
    }
}
