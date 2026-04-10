using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Calci
{
    /// <summary>
    /// Blob asset containing pre-baked noise values sampled from a texture's red channel.
    /// Values are remapped from the texture's [0, 1] range to [-1, 1] and stored as floats.
    /// <para>
    /// Use <see cref="NoiseTextureBlobBuilder"/> to create instances from a <c>UnityEngine.Texture2D</c>.
    /// Use <see cref="NoiseTextureSampler"/> to read values at runtime in Burst-compiled code.
    /// </para>
    /// </summary>
    public struct NoiseTextureBlob
    {
        /// <summary>
        /// Flattened noise values in [-1, 1], stored row-major (x / column varies fastest).
        /// Length equals <see cref="width"/> * <see cref="height"/>.
        /// </summary>
        public BlobArray<float> values;

        /// <summary>The width (number of columns) of the original texture.</summary>
        public int width;

        /// <summary>The height (number of rows) of the original texture.</summary>
        public int height;
    }

    /// <summary>
    /// Burst-safe runtime accessor for a <see cref="NoiseTextureBlob"/>.
    /// <para>
    /// Supports two sampling modes:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Sequential LUT</b> via <see cref="Next"/>: advances an internal index through the
    /// flattened array, wrapping at the end. Ideal for 1D use cases such as vibration sequences.
    /// </description></item>
    /// <item><description>
    /// <b>Spatial UV</b> via <see cref="Sample"/>: nearest-neighbour lookup at a normalised
    /// UV coordinate with repeat (wrap) addressing. Ideal for 2D spatial effects.
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This struct is unmanaged and can be stored directly on ECS entities as <c>IComponentData</c>.
    /// </para>
    /// </summary>
    public struct NoiseTextureSampler
    {
        BlobAssetReference<NoiseTextureBlob> m_blob;
        int                                  m_index;

        /// <summary>
        /// Constructs a sampler from an existing blob reference.
        /// The sequential index is initialised to 0.
        /// </summary>
        /// <param name="blob">A valid, created blob reference.</param>
        public NoiseTextureSampler(BlobAssetReference<NoiseTextureBlob> blob)
        {
            CheckBlobValid(blob);
            m_blob  = blob;
            m_index = 0;
        }

        /// <summary>
        /// Returns the next value in sequential LUT order, advancing the internal index.
        /// Wraps back to the beginning once the end of the array is reached.
        /// </summary>
        /// <returns>A noise value in [-1, 1].</returns>
        public float Next()
        {
            CheckBlobValid(m_blob);
            ref var blob = ref m_blob.Value;
            float   v    = blob.values[m_index];
            m_index      = (m_index + 1) % blob.values.Length;
            return v;
        }

        /// <summary>
        /// Samples a value at a normalised UV coordinate using nearest-neighbour lookup.
        /// Both UV components are wrapped (repeat addressing) so values outside [0, 1) tile correctly.
        /// </summary>
        /// <param name="uv">
        /// Normalised texture coordinate. Component x maps to the column axis, y to the row axis.
        /// </param>
        /// <returns>A noise value in [-1, 1].</returns>
        public float Sample(float2 uv)
        {
            CheckBlobValid(m_blob);
            ref var blob = ref m_blob.Value;
            // frac equivalent: wrap UV into [0, 1) then convert to integer texel coordinates.
            float2 wrapped = uv - math.floor(uv);
            int    px      = (int)(wrapped.x * blob.width)  % blob.width;
            int    py      = (int)(wrapped.y * blob.height) % blob.height;
            return blob.values[py * blob.width + px];
        }

        /// <summary>Resets the sequential index back to 0.</summary>
        public void Reset() => m_index = 0;

        /// <summary>
        /// Returns the underlying blob reference.
        /// Useful for passing to another sampler instance or storing on a component.
        /// </summary>
        public BlobAssetReference<NoiseTextureBlob> BlobReference => m_blob;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckBlobValid(BlobAssetReference<NoiseTextureBlob> blob)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!blob.IsCreated)
                throw new System.InvalidOperationException(
                    "NoiseTextureSampler: the blob reference is null or has not been created. " +
                    "Build a blob via NoiseTextureBlobBuilder before constructing a sampler.");
#endif
        }
    }
}
