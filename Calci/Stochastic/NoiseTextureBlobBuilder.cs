using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Latios.Calci
{
    /// <summary>
    /// Managed utility for building a <see cref="NoiseTextureBlob"/> from a
    /// <c>UnityEngine.Texture2D</c>.
    /// <para>
    /// <b>This class is NOT Burst-compatible.</b> Call it from managed main-thread code,
    /// typically inside an <c>IBaker.Bake()</c> method or an editor tool.
    /// The resulting <see cref="BlobAssetReference{T}"/> can then be stored on an entity
    /// and read from Burst-compiled jobs via <see cref="NoiseTextureSampler"/>.
    /// </para>
    /// </summary>
    public static class NoiseTextureBlobBuilder
    {
        /// <summary>
        /// Builds a <see cref="BlobAssetReference{NoiseTextureBlob}"/> from the red channel
        /// of a <c>Texture2D</c>. Each red-channel value in [0, 1] is remapped to [-1, 1].
        /// <para>
        /// The texture must have <b>Read/Write Enabled</b> set in its import settings,
        /// otherwise <c>GetPixels</c> will throw at runtime.
        /// </para>
        /// <para>
        /// The caller is responsible for disposing the returned blob when it is no longer needed.
        /// When building inside a baker, pass the blob to
        /// <c>baker.AddBlobAsset</c> / <c>BlobAssetStore</c> and let the baker manage lifetime.
        /// </para>
        /// </summary>
        /// <param name="texture">Source texture. Alpha, G, and B channels are ignored.</param>
        /// <param name="allocator">
        /// Allocator for the blob. Use <c>Allocator.Persistent</c> for runtime data or
        /// <c>Allocator.Temp</c> when the blob will be immediately handed to a baker.
        /// </param>
        /// <returns>A blob asset reference the caller owns and must dispose.</returns>
        public static BlobAssetReference<NoiseTextureBlob> Build(
            Texture2D texture,
            Allocator allocator = Allocator.Persistent)
        {
            int     w      = texture.width;
            int     h      = texture.height;
            Color[] pixels = texture.GetPixels(0);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<NoiseTextureBlob>();
            root.width  = w;
            root.height = h;

            var arr = builder.Allocate(ref root.values, w * h);
            for (int i = 0; i < pixels.Length; i++)
                arr[i] = pixels[i].r * 2f - 1f;

            var result = builder.CreateBlobAssetReference<NoiseTextureBlob>(allocator);
            builder.Dispose();
            return result;
        }

        /// <summary>
        /// Allocation-free variant using raw <c>Color32</c> data obtained via
        /// <c>Texture2D.GetRawTextureData&lt;Color32&gt;()</c>.
        /// Prefer this overload in baking systems to avoid the managed <c>Color[]</c> allocation
        /// that <see cref="Build(Texture2D, Allocator)"/> incurs.
        /// <para>
        /// The texture must be in a format whose first byte is the red channel (e.g. RGBA32, RGB24).
        /// </para>
        /// </summary>
        /// <param name="rawPixels">
        /// Raw pixel data as <c>Color32</c>. Obtain via <c>texture.GetRawTextureData&lt;Color32&gt;()</c>.
        /// </param>
        /// <param name="width">Width of the texture in pixels.</param>
        /// <param name="height">Height of the texture in pixels.</param>
        /// <param name="allocator">Allocator for the blob.</param>
        /// <returns>A blob asset reference the caller owns and must dispose.</returns>
        public static BlobAssetReference<NoiseTextureBlob> BuildFromRawColor32(
            NativeArray<Color32> rawPixels,
            int                  width,
            int                  height,
            Allocator            allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<NoiseTextureBlob>();
            root.width  = width;
            root.height = height;

            var arr = builder.Allocate(ref root.values, width * height);
            for (int i = 0; i < rawPixels.Length; i++)
                arr[i] = rawPixels[i].r * (2f / 255f) - 1f;

            var result = builder.CreateBlobAssetReference<NoiseTextureBlob>(allocator);
            builder.Dispose();
            return result;
        }
    }
}
