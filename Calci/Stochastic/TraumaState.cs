using Unity.Mathematics;

namespace Latios.Calci
{
    /// <summary>
    /// Additive impact trauma accumulator for driving shake intensity.
    /// <para>
    /// The trauma model separates <em>what causes shake</em> (impacts) from <em>how much shake
    /// occurs</em> (volatility). On collision, call <see cref="AddTrauma"/> to accumulate a
    /// value in [0, 1]. Each frame, <see cref="Decay"/> reduces trauma linearly. The quadratic
    /// <see cref="Intensity"/> property (trauma²) maps trauma non-linearly to a [0, 1] scale
    /// factor: small hits barely register while large hits produce the full effect.
    /// </para>
    /// <para>
    /// Pair with <see cref="OrnsteinUhlenbeckState"/> or <see cref="OrnsteinUhlenbeckState3"/>:
    /// pass <see cref="Intensity"/> as the <c>sigmaScale</c> argument to their Update methods.
    /// </para>
    /// <para>
    /// The zero-initialized struct is a valid default state — no explicit initialization needed.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    ///   // On impact event:
    ///   trauma.AddTrauma(0.7f);
    ///
    ///   // Per-frame Burst job:
    ///   trauma.Decay(decayRate: 1.5f, deltaTime);
    ///   if (trauma.IsActive)
    ///       ou.Update(ref rng, in p, trauma.Intensity, deltaTime);
    ///   else
    ///       ou.Initialize(float3.zero);   // reset so next hit starts clean
    /// </code>
    /// </para>
    /// </summary>
    public struct TraumaState
    {
        /// <summary>
        /// Current trauma level in [0, 1]. Multiple calls to <see cref="AddTrauma"/> in the
        /// same frame compound additively, clamped to 1. Decays to 0 over time via <see cref="Decay"/>.
        /// </summary>
        public float trauma;

        /// <summary>
        /// Adds <paramref name="amount"/> to the current trauma level, clamping the result to [0, 1].
        /// Multiple hits in the same frame accumulate: a 0.4 hit followed by a 0.5 hit
        /// yields trauma = 0.9, not 0.5.
        /// </summary>
        /// <param name="amount">Trauma magnitude in [0, 1]. Values outside this range are clamped.</param>
        public void AddTrauma(float amount) => trauma = math.saturate(trauma + amount);

        /// <summary>
        /// Reduces trauma linearly each frame. Call once per frame before reading
        /// <see cref="Intensity"/> or <see cref="IsActive"/>.
        /// </summary>
        /// <param name="decayRate">
        /// Rate of decay in units per second (world-space seconds of trauma removed per real second).
        /// For example, 1.0 reduces full trauma to zero in one second; 2.0 in half a second.
        /// </param>
        /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
        public void Decay(float decayRate, float deltaTime) =>
            trauma = math.max(trauma - decayRate * deltaTime, 0f);

        /// <summary>
        /// Quadratic intensity: <c>trauma²</c>.
        /// <para>
        /// Squaring maps low trauma to a near-zero effect while preserving the full range at
        /// high trauma — a 0.2 hit yields 0.04 intensity, a 0.5 hit yields 0.25, a 1.0 hit yields 1.0.
        /// Pass this to the <c>sigmaScale</c> argument of <see cref="OrnsteinUhlenbeckState.Update"/>
        /// or <see cref="OrnsteinUhlenbeckState3.Update"/> to drive shake amplitude.
        /// </para>
        /// </summary>
        public float Intensity => trauma * trauma;

        /// <summary>
        /// Returns <c>true</c> when trauma is non-negligible (above 1e-4).
        /// Use to gate the per-frame OU Update call and avoid burning RNG while at rest.
        /// </summary>
        public bool IsActive => trauma > 1e-4f;
    }
}
