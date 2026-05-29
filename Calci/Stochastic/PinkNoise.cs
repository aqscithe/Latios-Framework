using System.Diagnostics;
using Unity.Mathematics;

namespace Latios.Calci
{
    /// <summary>
    /// Stateful pink (1/f) noise generator using the Voss-McCartney algorithm.
    /// Produces correlated samples in approximately [-1, 1] with a 1/f power spectrum,
    /// making it ideal for organic micro-tremor effects.
    /// <para>
    /// Must call <see cref="Initialize"/> once before the first call to <see cref="Next"/>.
    /// Persist this struct across frames as a field in your ECS component.
    /// </para>
    /// <para>
    /// <b>Note:</b> Because this struct contains a <c>fixed float</c> buffer, any component
    /// struct that contains a <c>PinkNoiseState</c> field must be declared <c>unsafe</c>.
    /// </para>
    /// </summary>
    public unsafe struct PinkNoiseState
    {
        // 16 independently-updated rows, each holding a value in [-1, 1].
        // Fixed buffer keeps the struct fully unmanaged and stack-allocatable.
        internal fixed float m_rows[16];
        internal float       m_runningSum;
        internal uint        m_counter;
        internal bool        m_initialized;

        /// <summary>
        /// Seeds all 16 rows with independent random values and computes the initial running sum.
        /// Must be called once before the first call to <see cref="Next"/>.
        /// </summary>
        /// <param name="rng">The RNG sequence to draw seed values from.</param>
        public void Initialize(ref Rng.RngSequence rng)
        {
            m_runningSum  = 0f;
            m_counter     = 0;
            m_initialized = true;
            for (int i = 0; i < 16; i++)
            {
                float v      = rng.NextFloat(-1f, 1f);
                m_rows[i]    = v;
                m_runningSum += v;
            }
        }

        /// <summary>
        /// Advances the generator by one sample and returns the next pink noise value.
        /// <para>
        /// The output is in approximately [-1, 1]. In pathological cases where all rows
        /// simultaneously reach extremes the value can briefly exceed this range; clamp
        /// if hard limits are required.
        /// </para>
        /// </summary>
        /// <param name="rng">The RNG sequence to draw the new row value from. Consumes 1 value.</param>
        /// <returns>A pink noise sample in approximately [-1, 1].</returns>
        public float Next(ref Rng.RngSequence rng)
        {
            CheckInitialized();

            m_counter++;
            // Row index = trailing-zero count of counter, masked to [0, 15].
            // math.tzcnt returns 32 when input is 0, but counter is never 0 after
            // the first increment, so the mask handles the 32-bit edge case cleanly.
            int   rowIndex = (int)(math.tzcnt(m_counter) & 15u);
            float oldVal   = m_rows[rowIndex];
            float newVal   = rng.NextFloat(-1f, 1f);
            m_rows[rowIndex] = newVal;
            m_runningSum   += newVal - oldVal;

            // Divide by 16 to normalize into [-1, 1] on average.
            return m_runningSum * (1f / 16f);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckInitialized()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_initialized)
                throw new System.InvalidOperationException(
                    "PinkNoiseState must be initialized before use. Call Initialize() first.");
#endif
        }
    }
}
