using Unity.Mathematics;
using UnityEngine;

namespace Latios.Calci
{
    /// <summary>
    /// Configuration for a <see cref="LevyStruggleState"/> simulation.
    /// All fields are pre-set to balanced defaults suitable for a medium-intensity grip struggle.
    /// </summary>
    [System.Serializable]
    public struct LevyStruggleParams
    {
        /// <summary>
        /// Lévy tail exponent. Range (0, 3].
        /// Lower values produce more frequent large slips; higher values keep steps small.
        /// <list type="bullet">
        /// <item><description>1.5 — chaotic (frequent slips)</description></item>
        /// <item><description>2.0 — balanced (default)</description></item>
        /// <item><description>2.5 — controlled (small steps dominate)</description></item>
        /// </list>
        /// </summary>
        [Tooltip("Lévy tail exponent (0, 3]. Lower = frequent large slips (chaotic); higher = small " +
                 "steps dominate (controlled). Rule of thumb: 1.5 chaotic, 2.0 balanced, 2.5 tight.")]
        public float alpha;

        /// <summary>
        /// Minimum Lévy step length (world units). Sets the floor of the distribution. Must be > 0.
        /// Default: 0.01.
        /// </summary>
        [Tooltip("Smallest Lévy slip length, in world units. Floor of the distribution; must be > 0 " +
                 "AND <= maxSlipMagnitude (the Lévy sampler throws otherwise). Sets the quietest " +
                 "twitch the simulation can produce.")]
        public float minStepSize;

        /// <summary>
        /// Maximum slip magnitude (world units). Lévy steps are clamped to this value.
        /// Default: 0.5.
        /// </summary>
        [Tooltip("Largest Lévy slip length (world units). Lévy steps are clamped to this. Sets the " +
                 "peak per-slip displacement — how violent a single twitch can be.")]
        public float maxSlipMagnitude;

        /// <summary>
        /// Number of Lévy steps applied per second. Controls the temporal rate of slipping.
        /// Default: 8.
        /// </summary>
        [Tooltip("How many Lévy slip events fire per second. Higher = rapid, buzzy motion; " +
                 "lower = sparse, jerky tugs. Sets the temporal rhythm of the struggle.")]
        public float stepsPerSecond;

        /// <summary>
        /// Overall struggle amplitude in [0, 1]. 0 = full control (no movement), 1 = about to slip.
        /// Scales Lévy displacement and inversely scales the equilibrium bias.
        /// Typically driven by a gameplay mechanic such as a grip-strength meter.
        /// Default: 0.5.
        /// </summary>
        [Tooltip("Overall amplitude in [0,1]. 0 = perfect control (no struggle), 1 = about to slip " +
                 "free. Simultaneously scales Lévy displacement UP and the restoring spring DOWN, " +
                 "so increasing it widens both peak displacement and dwell time away from rest.")]
        public float struggleFactor;

        /// <summary>
        /// Scales the pink noise micro-tremor contribution per second.
        /// <para>
        /// <b>First-order path (<see cref="damping"/> == 0):</b> world-unit position offset per second
        /// (integrated over <c>deltaTime</c>).
        /// </para>
        /// <para>
        /// <b>Second-order path (<see cref="damping"/> &gt; 0):</b> acceleration (world units / second²)
        /// applied to <see cref="LevyStruggleState.velocity"/>.
        /// </para>
        /// Default: 0.02.
        /// </summary>
        [Tooltip("Pink-noise micro-tremor amplitude — the constant low-grade jitter underneath the " +
                 "Lévy slips. Units depend on damping: damping=0 (first-order) is world units / sec " +
                 "of position offset; damping>0 (second-order) is m/s² of acceleration.")]
        public float pinkNoiseIntensity;

        /// <summary>
        /// Spring constant of the restoring force pulling <see cref="LevyStruggleState.position"/>
        /// back toward the equilibrium target. Scaled by <c>(1 - struggleFactor)</c> so it weakens
        /// as the struggle intensifies.
        /// <para>
        /// <b>First-order path:</b> applied as a direct position correction per second (units: s⁻¹).
        /// </para>
        /// <para>
        /// <b>Second-order path:</b> acts as a spring acceleration (units: s⁻²). The natural " +
        /// frequency of the oscillator is <c>ω₀ = √biasStrength</c> rad/s.
        /// </para>
        /// Default: 2.0.
        /// </summary>
        [Tooltip("Spring constant pulling the attachment back to its rest pose. Higher = snappier " +
                 "return. Scaled by (1 − struggleFactor), so it weakens as struggle climbs. " +
                 "In second-order mode (damping>0), effective natural frequency " +
                 "ω₀ = √(biasStrength × (1 − struggleFactor)).")]
        public float biasStrength;

        /// <summary>
        /// Velocity damping rate γ (s⁻¹). When &gt; 0, enables the <b>second-order (hysteresis) path</b>:
        /// Lévy slips and pink noise apply as accelerations to <see cref="LevyStruggleState.velocity"/>
        /// instead of directly to position. The spring overshoots and oscillates before settling,
        /// giving the object inertial "mass" so corrections feel like fighting momentum rather than
        /// fighting a random number generator.
        /// <para>
        /// The effective natural frequency is
        /// <c>ω₀ = √(biasStrength × (1 − struggleFactor))</c>.
        /// Note that <see cref="struggleFactor"/> scales the spring down, so ω₀ depends on both
        /// <see cref="biasStrength"/> and <see cref="struggleFactor"/>.
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Underdamped</b> (γ &lt; 2ω₀): overshoots and rings — desired for inertia feel.</description></item>
        /// <item><description><b>Critical</b> (γ = 2ω₀): fastest return without overshoot.</description></item>
        /// <item><description><b>Overdamped</b> (γ &gt; 2ω₀): smooth exponential return, no oscillation.</description></item>
        /// </list>
        /// Damping ratio ζ = γ / (2ω₀). Target ζ ≈ 0.4–0.6 for a natural underdamped feel.
        /// Set to 0 (default) to use the original first-order path with no velocity state.
        /// Default: 0.
        /// </summary>
        [Tooltip("Velocity damping γ (1/s). 0 = first-order mode: slips move position directly, no " +
                 "inertia. >0 = second-order mode: slips/noise become accelerations on a velocity, " +
                 "so motion overshoots and rings — the 'fighting momentum' feel. Damping ratio " +
                 "ζ = γ / (2ω₀); target ζ ≈ 0.4–0.6 for a natural underdamped ring. " +
                 "ζ ≥ 1 = no ring; ζ << 1 = long ring-down.")]
        public float damping;

        /// <summary>Returns a <see cref="LevyStruggleParams"/> populated with all default values.</summary>
        public static LevyStruggleParams Default => new LevyStruggleParams
        {
            alpha              = 2f,
            minStepSize        = 0.01f,
            maxSlipMagnitude   = 0.5f,
            stepsPerSecond     = 8f,
            struggleFactor     = 0.5f,
            pinkNoiseIntensity = 0.02f,
            biasStrength       = 2f,
            damping            = 0f,
        };
    }

    /// <summary>
    /// Stateful 3D "struggle to maintain control" simulation combining:
    /// <list type="bullet">
    /// <item><description>Pink-noise micro-tremor (framerate-independent, integrated over <c>deltaTime</c>)</description></item>
    /// <item><description>Lévy-flight slips (framerate-independent, integrated over <c>deltaTime</c>)</description></item>
    /// <item><description>Spring-like equilibrium bias (framerate-independent)</description></item>
    /// </list>
    /// <para>
    /// Designed to be stored as a field in an ECS component and updated each simulation frame.
    /// </para>
    /// <para>
    /// Usage:
    /// <code>
    ///   // Either pink noise source works — the API is decoupled from the noise source:
    ///   float pink = pinkState.Next(ref seq);          // algorithmic path
    ///   // float pink = textureSampler.Next();          // texture LUT path
    ///
    ///   struggleState.Update(pink, ref seq, in p, equilibriumPos, deltaTime);
    ///   float3 currentPos = struggleState.position;
    /// </code>
    /// </para>
    /// </summary>
    public struct LevyStruggleState
    {
        /// <summary>
        /// Current 3D position of the struggling point.
        /// Initialise with <see cref="Initialize"/> and read after each <see cref="Update"/> call.
        /// </summary>
        public float3 position;

        /// <summary>
        /// Current velocity (world units / second). Only active when
        /// <see cref="LevyStruggleParams.damping"/> &gt; 0 (second-order / hysteresis path).
        /// Zero in first-order mode.
        /// </summary>
        public float3 velocity;

        /// <summary>
        /// Places <see cref="position"/> at <paramref name="startPosition"/> and zeroes
        /// <see cref="velocity"/>. Call this once before the first <see cref="Update"/>.
        /// </summary>
        public void Initialize(float3 startPosition)
        {
            position = startPosition;
            velocity = float3.zero;
        }

        /// <summary>
        /// Advances the simulation by one timestep.
        /// <para>
        /// <b>First-order path</b> (<see cref="LevyStruggleParams.damping"/> == 0):
        /// Lévy slips and pink noise apply directly to position; the spring bias damps displacement
        /// exponentially toward equilibrium. Classic behaviour, no oscillation.
        /// </para>
        /// <para>
        /// <b>Second-order path</b> (<see cref="LevyStruggleParams.damping"/> &gt; 0):
        /// All forces act on <see cref="velocity"/>, which is then integrated into position.
        /// The object has inertia — a Lévy slip spikes velocity, the spring overshoots and rings,
        /// and <see cref="LevyStruggleParams.damping"/> controls how quickly the oscillation decays.
        /// Underdamped (damping &lt; ω₀ = √biasStrength) gives the heaviest "fighting momentum" feel.
        /// </para>
        /// </summary>
        /// <param name="pinkNoiseSample">
        /// A pre-computed noise value in approximately [-1, 1]. Obtain this from either
        /// <see cref="PinkNoiseState.Next"/> (algorithmic) or <see cref="NoiseTextureSampler.Next"/>
        /// (texture LUT). The noise source is intentionally decoupled from this method.
        /// </param>
        /// <param name="rng">
        /// RNG sequence. This method consumes exactly 5 values:
        /// <c>NextFloat3Direction</c> (2) for the tremor direction,
        /// <c>NextFloat</c> (1) for the Lévy uniform variate, and
        /// <c>NextFloat3Direction</c> (2) for the Lévy slip direction.
        /// </param>
        /// <param name="p">Simulation parameters.</param>
        /// <param name="equilibrium">The target position the spring bias pulls toward.</param>
        /// <param name="deltaTime">Elapsed seconds since the previous Update call.</param>
        public void Update(
            float                  pinkNoiseSample,
            ref Rng.RngSequence    rng,
            in  LevyStruggleParams p,
            float3                 equilibrium,
            float                  deltaTime)
        {
            if (p.damping > 0f)
            {
                // --- Second-order (hysteresis) path ---
                // Forces act on velocity; position integrates through velocity.
                // pinkNoiseIntensity and Lévy contribution are accelerations (world units / s²).
                // biasStrength is a spring acceleration constant (s⁻²): F = k*(eq - pos).
                float3 pinkAcc   = rng.NextFloat3Direction() * (pinkNoiseSample * p.pinkNoiseIntensity);
                float  levyLen   = LevyFlight.SampleStepLength(rng.NextFloat(), p.alpha, p.minStepSize, p.maxSlipMagnitude);
                float3 levyAcc   = rng.NextFloat3Direction() * (levyLen * p.struggleFactor * p.stepsPerSecond);
                float3 springAcc = (equilibrium - position) * (p.biasStrength * (1f - p.struggleFactor));

                velocity += (pinkAcc + levyAcc + springAcc) * deltaTime;
                velocity *= math.exp(-p.damping * deltaTime); // framerate-independent exponential damping
                position += velocity * deltaTime;
            }
            else
            {
                // --- First-order (original) path ---
                // All forces apply directly to position, integrated over dt.
                // pinkNoiseIntensity is world units/second; biasStrength is a first-order decay rate (s⁻¹).
                float3 pinkOffset = rng.NextFloat3Direction() * (pinkNoiseSample * p.pinkNoiseIntensity * deltaTime);
                float  levyLen    = LevyFlight.SampleStepLength(rng.NextFloat(), p.alpha, p.minStepSize, p.maxSlipMagnitude);
                float3 levyOffset = rng.NextFloat3Direction() * (levyLen * p.struggleFactor * p.stepsPerSecond * deltaTime);
                float3 biasOffset = (equilibrium - position) * (p.biasStrength * (1f - p.struggleFactor) * deltaTime);
                position += pinkOffset + levyOffset + biasOffset;
            }
        }

        /// <summary>
        /// Returns the Euclidean distance from <see cref="position"/> to <paramref name="equilibrium"/>.
        /// Useful for triggering gameplay events such as a grip-loss threshold.
        /// </summary>
        public float DistanceFromEquilibrium(float3 equilibrium) => math.distance(position, equilibrium);
    }
}
