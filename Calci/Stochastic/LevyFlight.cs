using System.Diagnostics;
using Unity.Mathematics;

namespace Latios.Calci
{
    /// <summary>
    /// Stateless utilities for sampling step lengths from a truncated Pareto (Lévy flight) distribution.
    /// <para>
    /// Step lengths follow P(l) ∝ l<sup>-(alpha+1)</sup>, producing clusters of small steps
    /// punctuated by occasional large excursions ("slips"). Lower <c>alpha</c> values produce
    /// heavier tails with more frequent large jumps.
    /// </para>
    /// <para>
    /// Typical use: biological grip struggle (<c>alpha</c> 1.5–2.5), foraging search patterns,
    /// anomalous diffusion effects.
    /// </para>
    /// </summary>
    public static partial class LevyFlight
    {
        /// <summary>
        /// Samples a step length from a truncated Pareto distribution using inversion sampling.
        /// </summary>
        /// <param name="u">
        /// A uniform random variate in (0, 1). Obtain via <c>rng.NextFloat()</c>.
        /// </param>
        /// <param name="alpha">
        /// Tail exponent. Range (0, 3]. Lower values produce heavier tails (more frequent large steps).
        /// <list type="bullet">
        /// <item><description>1.5 — chaotic (frequent large slips)</description></item>
        /// <item><description>2.0 — balanced struggle (default)</description></item>
        /// <item><description>2.5 — controlled (small steps dominate)</description></item>
        /// </list>
        /// </param>
        /// <param name="minStep">
        /// Minimum step length (scale parameter of the distribution). Must be greater than 0.
        /// </param>
        /// <param name="maxStep">Maximum step length (truncation bound). Must be >= <paramref name="minStep"/>.</param>
        /// <returns>A step length in [<paramref name="minStep"/>, <paramref name="maxStep"/>].</returns>
        public static float SampleStepLength(float u, float alpha, float minStep, float maxStep)
        {
            CheckAlpha(alpha);
            CheckMinStep(minStep);
            CheckMinMax(minStep, maxStep);

            // Truncated Pareto inversion: CDF F(l) = 1 - (minStep/l)^alpha
            // => l = minStep / (1 - u)^(1/alpha)
            // Guard against u approaching 1 (would yield infinity) with an epsilon clamp.
            float denom = math.max(1f - u, 1e-6f);
            float l     = minStep / math.pow(denom, 1f / alpha);
            return math.clamp(l, minStep, maxStep);
        }

        /// <summary>
        /// Samples a 2D Lévy flight displacement with a uniformly random unit direction.
        /// Consumes 2 values from <paramref name="rngSequence"/> (1 for step length, 1 for direction).
        /// </summary>
        public static float2 NextLevyDisplacement2D(ref this Rng.RngSequence rngSequence, float alpha, float minStep, float maxStep)
        {
            float length = SampleStepLength(rngSequence.NextFloat(), alpha, minStep, maxStep);
            return rngSequence.NextFloat2Direction() * length;
        }

        /// <summary>
        /// Samples a 3D Lévy flight displacement with a uniformly random unit direction.
        /// Consumes 3 values from <paramref name="rngSequence"/> (1 for step length, 2 for direction).
        /// </summary>
        public static float3 NextLevyDisplacement3D(ref this Rng.RngSequence rngSequence, float alpha, float minStep, float maxStep)
        {
            float length = SampleStepLength(rngSequence.NextFloat(), alpha, minStep, maxStep);
            return rngSequence.NextFloat3Direction() * length;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAlpha(float alpha)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (alpha <= 0f || alpha > 3f)
                throw new System.ArgumentOutOfRangeException("alpha",
                    "alpha must be in (0, 3]. Typical struggle range is 1.5 to 2.5.");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckMinStep(float minStep)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (minStep <= 0f)
                throw new System.ArgumentOutOfRangeException("minStep", "minStep must be > 0.");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckMinMax(float minStep, float maxStep)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (maxStep < minStep)
                throw new System.ArgumentException("maxStep must be >= minStep.");
#endif
        }
    }
}
