using Unity.Mathematics;

namespace Latios.Calci
{
    /// <summary>
    /// Configuration for an <see cref="OrnsteinUhlenbeckState"/> or <see cref="OrnsteinUhlenbeckState3"/> simulation.
    /// All fields are pre-set to balanced defaults suitable for a moderate impact shake.
    /// </summary>
    public struct OrnsteinUhlenbeckParams
    {
        /// <summary>
        /// Mean reversion rate θ (s⁻¹). Controls how quickly the process returns to
        /// <see cref="mu"/> after a displacement. Higher values produce snappier recovery.
        /// <list type="bullet">
        /// <item><description>Gentle impact: 2–4</description></item>
        /// <item><description>Moderate impact: 6–10</description></item>
        /// <item><description>Violent impact: 15–20</description></item>
        /// </list>
        /// Must be &gt; 0. Default: 6.
        /// </summary>
        public float theta;

        /// <summary>
        /// Long-term equilibrium. The process is continuously pulled toward this value.
        /// Typically 0 for a shake effect centered at rest. Default: 0.
        /// </summary>
        public float mu;

        /// <summary>
        /// Noise volatility σ. Controls the amplitude of the stochastic forcing.
        /// The long-run standard deviation of the process is <c>σ / √(2θ)</c>.
        /// <list type="bullet">
        /// <item><description>Gentle: 0.05</description></item>
        /// <item><description>Moderate: 0.2</description></item>
        /// <item><description>Violent: 0.8</description></item>
        /// </list>
        /// When combined with <see cref="TraumaState"/>, pass <see cref="TraumaState.Intensity"/>
        /// as the <c>sigmaScale</c> argument to scale volatility by trauma² each frame.
        /// Must be ≥ 0. Default: 0.2.
        /// </summary>
        public float sigma;

        /// <summary>Returns an <see cref="OrnsteinUhlenbeckParams"/> populated with all default values.</summary>
        public static OrnsteinUhlenbeckParams Default => new OrnsteinUhlenbeckParams
        {
            theta = 6f,
            mu    = 0f,
            sigma = 0.2f,
        };
    }

    /// <summary>
    /// Stateful 1D Ornstein-Uhlenbeck (OU) process using an exact exponential integrator.
    /// <para>
    /// The OU SDE is <c>dX = θ(μ − X) dt + σ dW</c>. This struct implements its exact solution:
    /// </para>
    /// <para>
    /// <c>X_{t+Δt} = X_t · e^{−θΔt} + μ · (1 − e^{−θΔt}) + σ · √((1 − e^{−2θΔt}) / (2θ)) · Z</c>
    /// </para>
    /// <para>
    /// where Z ~ N(0,1). Unlike the Euler-Maruyama approximation, the exact integrator is
    /// unconditionally stable for all θ and Δt. The noise term self-scales with Δt so the
    /// long-run variance <c>σ²/(2θ)</c> is preserved regardless of frame rate.
    /// </para>
    /// <para>
    /// The zero-initialized struct is a valid starting state (x = 0). Call <see cref="Initialize"/>
    /// only when you need the process to begin from a specific non-zero position.
    /// </para>
    /// <para>
    /// Typical use: 1D impact shake, single-axis vibration, volume/parameter pulsing.
    /// For 3D positional shake see <see cref="OrnsteinUhlenbeckState3"/>.
    /// Pair with <see cref="TraumaState"/> to drive amplitude from an impact accumulator.
    /// </para>
    /// </summary>
    public struct OrnsteinUhlenbeckState
    {
        /// <summary>Current value of the process. Read after each <see cref="Update"/> call.</summary>
        public float x;

        /// <summary>
        /// Sets <see cref="x"/> to <paramref name="startValue"/>.
        /// Useful when you want to begin from a non-zero initial displacement.
        /// </summary>
        public void Initialize(float startValue) => x = startValue;

        /// <summary>
        /// Advances the process by one timestep using the exact exponential integrator.
        /// Consumes <b>2</b> values from <paramref name="rng"/> (one Box-Muller pair → one standard normal).
        /// </summary>
        /// <param name="rng">RNG sequence. Consumes 2 values.</param>
        /// <param name="p">Simulation parameters.</param>
        /// <param name="deltaTime">Elapsed seconds since the last Update call.</param>
        public void Update(ref Rng.RngSequence rng, in OrnsteinUhlenbeckParams p, float deltaTime)
        {
            float decay   = math.exp(-p.theta * deltaTime);
            float decay2  = math.exp(-2f * p.theta * deltaTime);
            float noiseSd = p.sigma * math.sqrt((1f - decay2) / (2f * math.max(p.theta, 1e-8f)));
            float z       = BoxMuller(rng.NextFloat(), rng.NextFloat()).x;
            x             = x * decay + p.mu * (1f - decay) + noiseSd * z;
        }

        /// <summary>
        /// Advances the process with <paramref name="sigmaScale"/> multiplied into
        /// <see cref="OrnsteinUhlenbeckParams.sigma"/> before integration.
        /// <para>
        /// Typical usage: pass <see cref="TraumaState.Intensity"/> (trauma²) so shake amplitude
        /// scales non-linearly with impact severity.
        /// </para>
        /// <para>
        /// Consumes <b>2</b> values from <paramref name="rng"/> regardless of
        /// <paramref name="sigmaScale"/> — the RNG sequence always advances to maintain
        /// deterministic behaviour when running alongside other RNG consumers.
        /// </para>
        /// </summary>
        /// <param name="rng">RNG sequence. Consumes 2 values.</param>
        /// <param name="p">Simulation parameters.</param>
        /// <param name="sigmaScale">Multiplied into <see cref="OrnsteinUhlenbeckParams.sigma"/>. Pass <see cref="TraumaState.Intensity"/>.</param>
        /// <param name="deltaTime">Elapsed seconds since the last Update call.</param>
        public void Update(ref Rng.RngSequence rng, in OrnsteinUhlenbeckParams p, float sigmaScale, float deltaTime)
        {
            float decay   = math.exp(-p.theta * deltaTime);
            float decay2  = math.exp(-2f * p.theta * deltaTime);
            float noiseSd = p.sigma * sigmaScale * math.sqrt((1f - decay2) / (2f * math.max(p.theta, 1e-8f)));
            float z       = BoxMuller(rng.NextFloat(), rng.NextFloat()).x;
            x             = x * decay + p.mu * (1f - decay) + noiseSd * z;
        }

        // Box-Muller transform: 2 uniform variates in (0, 1] → 2 independent standard normals.
        // u1 is clamped away from 0 to prevent log(0) when the RNG produces exactly 0.
        static float2 BoxMuller(float u1, float u2)
        {
            float r     = math.sqrt(-2f * math.log(math.max(u1, 1e-10f)));
            float angle = 2f * math.PI * u2;
            return new float2(r * math.cos(angle), r * math.sin(angle));
        }
    }

    /// <summary>
    /// Stateful 3D Ornstein-Uhlenbeck (OU) process using an exact exponential integrator.
    /// <para>
    /// Runs three independent 1D OU processes (one per axis) sharing the same
    /// <see cref="OrnsteinUhlenbeckParams"/>. Each axis receives its own standard normal
    /// variate drawn independently, so the axes are statistically uncorrelated.
    /// </para>
    /// <para>
    /// See <see cref="OrnsteinUhlenbeckState"/> for the full integrator formula and its
    /// stability guarantees.
    /// </para>
    /// <para>
    /// The zero-initialized struct is a valid starting state (x = 0). Call <see cref="Initialize"/>
    /// only when you need to begin from a specific non-zero position.
    /// </para>
    /// <para>
    /// Typical use: 3D positional shake (helmet, camera, held object) on collision impact.
    /// Pair with <see cref="TraumaState"/> to drive amplitude from an impact accumulator.
    /// </para>
    /// </summary>
    public struct OrnsteinUhlenbeckState3
    {
        /// <summary>Current 3D position of the process. Read after each <see cref="Update"/> call.</summary>
        public float3 x;

        /// <summary>
        /// Sets <see cref="x"/> to <paramref name="startValue"/>.
        /// Useful when you want the process to begin from a specific non-zero displacement.
        /// </summary>
        public void Initialize(float3 startValue) => x = startValue;

        /// <summary>
        /// Advances all three axes by one timestep using the exact exponential integrator.
        /// <para>
        /// Consumes <b>4</b> values from <paramref name="rng"/>: two Box-Muller pairs produce
        /// four standard normals; the x, y, z axes use the first three and the fourth is discarded.
        /// </para>
        /// </summary>
        /// <param name="rng">RNG sequence. Consumes 4 values.</param>
        /// <param name="p">Simulation parameters (shared across all three axes).</param>
        /// <param name="deltaTime">Elapsed seconds since the last Update call.</param>
        public void Update(ref Rng.RngSequence rng, in OrnsteinUhlenbeckParams p, float deltaTime)
        {
            float  decay   = math.exp(-p.theta * deltaTime);
            float  decay2  = math.exp(-2f * p.theta * deltaTime);
            float  noiseSd = p.sigma * math.sqrt((1f - decay2) / (2f * math.max(p.theta, 1e-8f)));
            float2 bm0     = BoxMuller(rng.NextFloat(), rng.NextFloat());
            float2 bm1     = BoxMuller(rng.NextFloat(), rng.NextFloat());
            float3 z       = new float3(bm0.x, bm0.y, bm1.x);  // bm1.y discarded
            x              = x * decay + p.mu * (1f - decay) + noiseSd * z;
        }

        /// <summary>
        /// Advances the process with <paramref name="sigmaScale"/> multiplied into
        /// <see cref="OrnsteinUhlenbeckParams.sigma"/> before integration.
        /// <para>
        /// Typical usage: pass <see cref="TraumaState.Intensity"/> (trauma²) so shake amplitude
        /// scales non-linearly with impact severity.
        /// </para>
        /// <para>
        /// Consumes <b>4</b> values from <paramref name="rng"/> regardless of
        /// <paramref name="sigmaScale"/> — the RNG sequence always advances to maintain
        /// deterministic behaviour when running alongside other RNG consumers.
        /// </para>
        /// </summary>
        /// <param name="rng">RNG sequence. Consumes 4 values.</param>
        /// <param name="p">Simulation parameters.</param>
        /// <param name="sigmaScale">Multiplied into <see cref="OrnsteinUhlenbeckParams.sigma"/>. Pass <see cref="TraumaState.Intensity"/>.</param>
        /// <param name="deltaTime">Elapsed seconds since the last Update call.</param>
        public void Update(ref Rng.RngSequence rng, in OrnsteinUhlenbeckParams p, float sigmaScale, float deltaTime)
        {
            float  decay   = math.exp(-p.theta * deltaTime);
            float  decay2  = math.exp(-2f * p.theta * deltaTime);
            float  noiseSd = p.sigma * sigmaScale * math.sqrt((1f - decay2) / (2f * math.max(p.theta, 1e-8f)));
            float2 bm0     = BoxMuller(rng.NextFloat(), rng.NextFloat());
            float2 bm1     = BoxMuller(rng.NextFloat(), rng.NextFloat());
            float3 z       = new float3(bm0.x, bm0.y, bm1.x);  // bm1.y discarded
            x              = x * decay + p.mu * (1f - decay) + noiseSd * z;
        }

        static float2 BoxMuller(float u1, float u2)
        {
            float r     = math.sqrt(-2f * math.log(math.max(u1, 1e-10f)));
            float angle = 2f * math.PI * u2;
            return new float2(r * math.cos(angle), r * math.sin(angle));
        }
    }
}
