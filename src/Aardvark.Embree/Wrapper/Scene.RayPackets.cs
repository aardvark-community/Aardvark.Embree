using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class Scene
{
    /// <summary>
    /// Intersects a packet of 4 rays with the scene (zero-copy version).
    /// Writes 4 RayHit results to the provided span (2-4× faster than 4 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 4)</param>
    /// <param name="directions">Ray directions (must have Length >= 4)</param>
    /// <param name="results">Output span for results (must have Length >= 4)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    public unsafe void Intersect4(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<RayHit> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 4)
            throw new ArgumentException("origins span must have at least 4 elements", nameof(origins));
        if (directions.Length < 4)
            throw new ArgumentException("directions span must have at least 4 elements", nameof(directions));
        if (results.Length < 4)
            throw new ArgumentException("results span must have at least 4 elements", nameof(results));

        // Allocate 16-byte aligned memory for RTCRayHit4 (required by Embree 4 SIMD)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit4>() + 15];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 16-byte boundary: (ptr + 15) & ~15 rounds up to next multiple of 16
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
            RTCRayHit4* rayHitPtr = (RTCRayHit4*)alignedPtr;

            // Initialize 4 rays from spans
            for (int i = 0; i < 4; i++)
            {
                rayHitPtr->ray.org_x[i] = origins[i].X;
                rayHitPtr->ray.org_y[i] = origins[i].Y;
                rayHitPtr->ray.org_z[i] = origins[i].Z;
                rayHitPtr->ray.tnear[i] = tmin;

                rayHitPtr->ray.dir_x[i] = directions[i].X;
                rayHitPtr->ray.dir_y[i] = directions[i].Y;
                rayHitPtr->ray.dir_z[i] = directions[i].Z;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = tmax;
                rayHitPtr->ray.mask[i] = mask;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                // Initialize hit to invalid
                rayHitPtr->hit.geomID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.primID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.instID[i] = RTC_INVALID_GEOMETRY_ID;
            }

            // Set validity mask (all rays valid = -1) with 16-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 4 + 15];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 16-byte boundary for SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 15) & ~15L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 4; i++)
                    valid[i] = -1;

                // Prepare intersection arguments
                var args = new RTCIntersectArguments()
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero,
                };

                var argsPtr = &args;

                // Call Embree rtcIntersect4
                EmbreeAPI.rtcIntersect4(valid, Handle, rayHitPtr, argsPtr);

                // Check device errors
                m_device.CheckError("Scene.Intersect4");

                // Extract results into output span
                for (int i = 0; i < 4; i++)
                {
                    if (rayHitPtr->hit.geomID[i] != RTC_INVALID_GEOMETRY_ID)
                    {
                        results[i].T = rayHitPtr->ray.tfar[i];
                        results[i].Coord = new V2f(rayHitPtr->hit.u[i], rayHitPtr->hit.v[i]);
                        results[i].Normal = new V3f(
                            rayHitPtr->hit.Ng_x[i],
                            rayHitPtr->hit.Ng_y[i],
                            rayHitPtr->hit.Ng_z[i]
                        );
                        results[i].PrimitiveId = rayHitPtr->hit.primID[i];
                        results[i].GeometryId = rayHitPtr->hit.geomID[i];
                        results[i].InstanceId = rayHitPtr->hit.instID[i];
                    }
                    else
                    {
                        // No hit - set default invalid values
                        results[i].T = float.MaxValue;
                        results[i].Coord = V2f.Zero;
                        results[i].Normal = V3f.Zero;
                        results[i].PrimitiveId = RTC_INVALID_GEOMETRY_ID;
                        results[i].GeometryId = RTC_INVALID_GEOMETRY_ID;
                        results[i].InstanceId = RTC_INVALID_GEOMETRY_ID;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intersects a packet of 4 rays with the scene.
    /// Returns array of 4 RayHit results (2-4× faster than 4 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 4)</param>
    /// <param name="directions">Ray directions (must have Length >= 4)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    /// <returns>Array of 4 RayHit results</returns>
    public RayHit[] Intersect4(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new RayHit[4];
        Intersect4(origins, directions, results, tmin, tmax, mask);
        return results;
    }

    /// <summary>
    /// Tests a packet of 4 rays for occlusion with the scene (zero-copy version).
    /// Writes 4 bool results to the provided span (2-4× faster than 4 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 4)</param>
    /// <param name="directions">Ray directions (must have Length >= 4)</param>
    /// <param name="results">Output span for results (must have Length >= 4)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    public unsafe void Occluded4(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<bool> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 4)
            throw new ArgumentException("origins span must have at least 4 elements", nameof(origins));
        if (directions.Length < 4)
            throw new ArgumentException("directions span must have at least 4 elements", nameof(directions));
        if (results.Length < 4)
            throw new ArgumentException("results span must have at least 4 elements", nameof(results));

        // Allocate 16-byte aligned memory for RTCRay4 (required by Embree 4 SIMD)
        int structSize = Marshal.SizeOf<RTCRay4>();
        Span<byte> buffer = stackalloc byte[structSize + 15];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 16-byte boundary: (ptr + 15) & ~15 rounds up to next multiple of 16
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
            RTCRay4* rayPacket = (RTCRay4*)alignedPtr;

            // Initialize 4 rays from spans
            for (int i = 0; i < 4; i++)
            {
                rayPacket->org_x[i] = origins[i].X;
                rayPacket->org_y[i] = origins[i].Y;
                rayPacket->org_z[i] = origins[i].Z;
                rayPacket->tnear[i] = tmin;

                rayPacket->dir_x[i] = directions[i].X;
                rayPacket->dir_y[i] = directions[i].Y;
                rayPacket->dir_z[i] = directions[i].Z;
                rayPacket->time[i] = 0.0f;

                rayPacket->tfar[i] = tmax;
                rayPacket->mask[i] = mask;
                rayPacket->id[i] = (uint)i;
                rayPacket->flags[i] = 0;
            }

            // Set validity mask (all rays valid = -1) with 16-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 4 + 15];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 16-byte boundary for SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 15) & ~15L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 4; i++)
                    valid[i] = -1;

                // Initialize RTCOccludedArguments
                var args = new RTCOccludedArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    occluded = IntPtr.Zero
                };

                // Call Embree rtcOccluded4
                EmbreeAPI.rtcOccluded4(valid, Handle, rayPacket, &args);

                // Check for device errors
                m_device.CheckError("Scene.Occluded4");

                // Extract occlusion results into output span
                // In Embree, tfar is set to negative infinity if ray is occluded
                for (int i = 0; i < 4; i++)
                {
                    results[i] = rayPacket->tfar[i] == float.NegativeInfinity;
                }
            }
        }
    }

    /// <summary>
    /// Tests a packet of 4 rays for occlusion with the scene.
    /// Returns array of 4 bools indicating occlusion (2-4× faster than 4 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 4)</param>
    /// <param name="directions">Ray directions (must have Length >= 4)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    /// <returns>Array of 4 bools where true indicates the ray is occluded</returns>
    public bool[] Occluded4(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new bool[4];
        Occluded4(origins, directions, results, tmin, tmax, mask);
        return results;
    }

    /// <summary>
    /// Intersects a packet of 8 rays with the scene (AVX2 optimized, zero-copy version).
    /// Writes 8 RayHit results to the provided span (2-4× faster than 8 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 8)</param>
    /// <param name="directions">Ray directions (must have Length >= 8)</param>
    /// <param name="results">Output span for results (must have Length >= 8)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    public unsafe void Intersect8(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<RayHit> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 8)
            throw new ArgumentException("origins span must have at least 8 elements", nameof(origins));
        if (directions.Length < 8)
            throw new ArgumentException("directions span must have at least 8 elements", nameof(directions));
        if (results.Length < 8)
            throw new ArgumentException("results span must have at least 8 elements", nameof(results));

        // Allocate 32-byte aligned memory for RTCRayHit8 (required by Embree 4 AVX2)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit8>() + 31];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 32-byte boundary: (ptr + 31) & ~31 rounds up to next multiple of 32
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 31) & ~31L);
            RTCRayHit8* rayHitPtr = (RTCRayHit8*)alignedPtr;

            // Initialize 8 rays from spans
            for (int i = 0; i < 8; i++)
            {
                rayHitPtr->ray.org_x[i] = origins[i].X;
                rayHitPtr->ray.org_y[i] = origins[i].Y;
                rayHitPtr->ray.org_z[i] = origins[i].Z;
                rayHitPtr->ray.tnear[i] = tmin;

                rayHitPtr->ray.dir_x[i] = directions[i].X;
                rayHitPtr->ray.dir_y[i] = directions[i].Y;
                rayHitPtr->ray.dir_z[i] = directions[i].Z;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = tmax;
                rayHitPtr->ray.mask[i] = mask;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                // Initialize hit to invalid
                rayHitPtr->hit.geomID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.primID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.instID[i] = RTC_INVALID_GEOMETRY_ID;
            }

            // Set validity mask (all rays valid = -1) with 32-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 8 + 31];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 32-byte boundary for AVX2 SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 31) & ~31L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 8; i++)
                    valid[i] = -1;

                // Prepare intersection arguments
                var args = new RTCIntersectArguments()
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero,
                };

                var argsPtr = &args;

                // Call Embree rtcIntersect8
                EmbreeAPI.rtcIntersect8(valid, Handle, rayHitPtr, argsPtr);

                // Check device errors
                m_device.CheckError("Scene.Intersect8");

                // Extract results into output span
                for (int i = 0; i < 8; i++)
                {
                    if (rayHitPtr->hit.geomID[i] != RTC_INVALID_GEOMETRY_ID)
                    {
                        results[i].T = rayHitPtr->ray.tfar[i];
                        results[i].Coord = new V2f(rayHitPtr->hit.u[i], rayHitPtr->hit.v[i]);
                        results[i].Normal = new V3f(
                            rayHitPtr->hit.Ng_x[i],
                            rayHitPtr->hit.Ng_y[i],
                            rayHitPtr->hit.Ng_z[i]
                        );
                        results[i].PrimitiveId = rayHitPtr->hit.primID[i];
                        results[i].GeometryId = rayHitPtr->hit.geomID[i];
                        results[i].InstanceId = rayHitPtr->hit.instID[i];
                    }
                    else
                    {
                        // No hit - set default invalid values
                        results[i].T = float.MaxValue;
                        results[i].Coord = V2f.Zero;
                        results[i].Normal = V3f.Zero;
                        results[i].PrimitiveId = RTC_INVALID_GEOMETRY_ID;
                        results[i].GeometryId = RTC_INVALID_GEOMETRY_ID;
                        results[i].InstanceId = RTC_INVALID_GEOMETRY_ID;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intersects a packet of 8 rays with the scene (AVX2 optimized).
    /// Returns array of 8 RayHit results (2-4× faster than 8 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 8)</param>
    /// <param name="directions">Ray directions (must have Length >= 8)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    /// <returns>Array of 8 RayHit results</returns>
    public RayHit[] Intersect8(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new RayHit[8];
        Intersect8(origins, directions, results, tmin, tmax, mask);
        return results;
    }

    /// <summary>
    /// Tests a packet of 8 rays for occlusion with the scene (AVX2 optimized, zero-copy version).
    /// Writes 8 bool results to the provided span (2-4× faster than 8 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 8)</param>
    /// <param name="directions">Ray directions (must have Length >= 8)</param>
    /// <param name="results">Output span for results (must have Length >= 8)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    public unsafe void Occluded8(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<bool> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 8)
            throw new ArgumentException("origins span must have at least 8 elements", nameof(origins));
        if (directions.Length < 8)
            throw new ArgumentException("directions span must have at least 8 elements", nameof(directions));
        if (results.Length < 8)
            throw new ArgumentException("results span must have at least 8 elements", nameof(results));

        // Allocate 32-byte aligned memory for RTCRay8 (required by Embree 4 AVX2)
        int structSize = Marshal.SizeOf<RTCRay8>();
        Span<byte> buffer = stackalloc byte[structSize + 31];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 32-byte boundary: (ptr + 31) & ~31 rounds up to next multiple of 32
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 31) & ~31L);
            RTCRay8* rayPacket = (RTCRay8*)alignedPtr;

            // Initialize 8 rays from spans
            for (int i = 0; i < 8; i++)
            {
                rayPacket->org_x[i] = origins[i].X;
                rayPacket->org_y[i] = origins[i].Y;
                rayPacket->org_z[i] = origins[i].Z;
                rayPacket->tnear[i] = tmin;

                rayPacket->dir_x[i] = directions[i].X;
                rayPacket->dir_y[i] = directions[i].Y;
                rayPacket->dir_z[i] = directions[i].Z;
                rayPacket->time[i] = 0.0f;

                rayPacket->tfar[i] = tmax;
                rayPacket->mask[i] = mask;
                rayPacket->id[i] = (uint)i;
                rayPacket->flags[i] = 0;
            }

            // Set validity mask (all rays valid = -1) with 32-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 8 + 31];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 32-byte boundary for AVX2 SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 31) & ~31L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 8; i++)
                    valid[i] = -1;

                // Initialize RTCOccludedArguments
                var args = new RTCOccludedArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    occluded = IntPtr.Zero
                };

                // Call Embree rtcOccluded8
                EmbreeAPI.rtcOccluded8(valid, Handle, rayPacket, &args);

                // Check for device errors
                m_device.CheckError("Scene.Occluded8");

                // Extract occlusion results into output span
                // In Embree, tfar is set to negative infinity if ray is occluded
                for (int i = 0; i < 8; i++)
                {
                    results[i] = rayPacket->tfar[i] == float.NegativeInfinity;
                }
            }
        }
    }

    /// <summary>
    /// Tests a packet of 8 rays for occlusion with the scene (AVX2 optimized).
    /// Returns array of 8 bools indicating occlusion (2-4× faster than 8 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 8)</param>
    /// <param name="directions">Ray directions (must have Length >= 8)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    /// <returns>Array of 8 bools where true indicates the ray is occluded</returns>
    public bool[] Occluded8(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new bool[8];
        Occluded8(origins, directions, results, tmin, tmax, mask);
        return results;
    }

    /// <summary>
    /// Intersects a packet of 16 rays with the scene (AVX-512 optimized, zero-copy version).
    /// Writes 16 RayHit results to the provided span (2-4× faster than 16 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 16)</param>
    /// <param name="directions">Ray directions (must have Length >= 16)</param>
    /// <param name="results">Output span for results (must have Length >= 16)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    public unsafe void Intersect16(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<RayHit> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 16)
            throw new ArgumentException("origins span must have at least 16 elements", nameof(origins));
        if (directions.Length < 16)
            throw new ArgumentException("directions span must have at least 16 elements", nameof(directions));
        if (results.Length < 16)
            throw new ArgumentException("results span must have at least 16 elements", nameof(results));

        // Allocate 64-byte aligned memory for RTCRayHit16 (required by Embree 4 AVX-512)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit16>() + 63];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 64-byte boundary: (ptr + 63) & ~63 rounds up to next multiple of 64
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 63) & ~63L);
            RTCRayHit16* rayHitPtr = (RTCRayHit16*)alignedPtr;

            // Initialize 16 rays from spans
            for (int i = 0; i < 16; i++)
            {
                rayHitPtr->ray.org_x[i] = origins[i].X;
                rayHitPtr->ray.org_y[i] = origins[i].Y;
                rayHitPtr->ray.org_z[i] = origins[i].Z;
                rayHitPtr->ray.tnear[i] = tmin;

                rayHitPtr->ray.dir_x[i] = directions[i].X;
                rayHitPtr->ray.dir_y[i] = directions[i].Y;
                rayHitPtr->ray.dir_z[i] = directions[i].Z;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = tmax;
                rayHitPtr->ray.mask[i] = mask;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                // Initialize hit to invalid
                rayHitPtr->hit.geomID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.primID[i] = RTC_INVALID_GEOMETRY_ID;
                rayHitPtr->hit.instID[i] = RTC_INVALID_GEOMETRY_ID;
            }

            // Set validity mask (all rays valid = -1) with 64-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 64-byte boundary for AVX-512 SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                // Prepare intersection arguments
                var args = new RTCIntersectArguments()
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero,
                };

                var argsPtr = &args;

                // Call Embree rtcIntersect16
                EmbreeAPI.rtcIntersect16(valid, Handle, rayHitPtr, argsPtr);

                // Check device errors
                m_device.CheckError("Scene.Intersect16");

                // Extract results into output span
                for (int i = 0; i < 16; i++)
                {
                    if (rayHitPtr->hit.geomID[i] != RTC_INVALID_GEOMETRY_ID)
                    {
                        results[i].T = rayHitPtr->ray.tfar[i];
                        results[i].Coord = new V2f(rayHitPtr->hit.u[i], rayHitPtr->hit.v[i]);
                        results[i].Normal = new V3f(
                            rayHitPtr->hit.Ng_x[i],
                            rayHitPtr->hit.Ng_y[i],
                            rayHitPtr->hit.Ng_z[i]
                        );
                        results[i].PrimitiveId = rayHitPtr->hit.primID[i];
                        results[i].GeometryId = rayHitPtr->hit.geomID[i];
                        results[i].InstanceId = rayHitPtr->hit.instID[i];
                    }
                    else
                    {
                        // No hit - set default invalid values
                        results[i].T = float.MaxValue;
                        results[i].Coord = V2f.Zero;
                        results[i].Normal = V3f.Zero;
                        results[i].PrimitiveId = RTC_INVALID_GEOMETRY_ID;
                        results[i].GeometryId = RTC_INVALID_GEOMETRY_ID;
                        results[i].InstanceId = RTC_INVALID_GEOMETRY_ID;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intersects a packet of 16 rays with the scene (AVX-512 optimized).
    /// Returns array of 16 RayHit results (2-4× faster than 16 individual rays).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 16)</param>
    /// <param name="directions">Ray directions (must have Length >= 16)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for filtering (default: 0xFFFFFFFF)</param>
    /// <returns>Array of 16 RayHit results</returns>
    public RayHit[] Intersect16(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new RayHit[16];
        Intersect16(origins, directions, results, tmin, tmax, mask);
        return results;
    }

    /// <summary>
    /// Tests a packet of 16 rays for occlusion with the scene (AVX-512 optimized, zero-copy version).
    /// Writes 16 bool results to the provided span (2-4× faster than 16 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 16)</param>
    /// <param name="directions">Ray directions (must have Length >= 16)</param>
    /// <param name="results">Output span for results (must have Length >= 16)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    public unsafe void Occluded16(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        Span<bool> results,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        // Validate inputs
        if (origins.Length < 16)
            throw new ArgumentException("origins span must have at least 16 elements", nameof(origins));
        if (directions.Length < 16)
            throw new ArgumentException("directions span must have at least 16 elements", nameof(directions));
        if (results.Length < 16)
            throw new ArgumentException("results span must have at least 16 elements", nameof(results));

        // Allocate 64-byte aligned memory for RTCRay16 (required by Embree 4 AVX-512)
        int structSize = Marshal.SizeOf<RTCRay16>();
        Span<byte> buffer = stackalloc byte[structSize + 63];

        fixed (byte* bufferPtr = buffer)
        {
            // Align to 64-byte boundary: (ptr + 63) & ~63 rounds up to next multiple of 64
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 63) & ~63L);
            RTCRay16* rayPacket = (RTCRay16*)alignedPtr;

            // Initialize 16 rays from spans
            for (int i = 0; i < 16; i++)
            {
                rayPacket->org_x[i] = origins[i].X;
                rayPacket->org_y[i] = origins[i].Y;
                rayPacket->org_z[i] = origins[i].Z;
                rayPacket->tnear[i] = tmin;

                rayPacket->dir_x[i] = directions[i].X;
                rayPacket->dir_y[i] = directions[i].Y;
                rayPacket->dir_z[i] = directions[i].Z;
                rayPacket->time[i] = 0.0f;

                rayPacket->tfar[i] = tmax;
                rayPacket->mask[i] = mask;
                rayPacket->id[i] = (uint)i;
                rayPacket->flags[i] = 0;
            }

            // Set validity mask (all rays valid = -1) with 64-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                // Align to 64-byte boundary for AVX-512 SIMD operations
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                // Initialize RTCOccludedArguments
                var args = new RTCOccludedArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    occluded = IntPtr.Zero
                };

                // Call Embree rtcOccluded16
                EmbreeAPI.rtcOccluded16(valid, Handle, rayPacket, &args);

                // Check for device errors
                m_device.CheckError("Scene.Occluded16");

                // Extract occlusion results into output span
                // In Embree, tfar is set to negative infinity if ray is occluded
                for (int i = 0; i < 16; i++)
                {
                    results[i] = rayPacket->tfar[i] == float.NegativeInfinity;
                }
            }
        }
    }

    /// <summary>
    /// Tests a packet of 16 rays for occlusion with the scene (AVX-512 optimized).
    /// Returns array of 16 bools indicating occlusion (2-4× faster than 16 individual tests).
    /// </summary>
    /// <param name="origins">Ray origins (must have Length >= 16)</param>
    /// <param name="directions">Ray directions (must have Length >= 16)</param>
    /// <param name="tmin">Minimum ray distance</param>
    /// <param name="tmax">Maximum ray distance</param>
    /// <param name="mask">Ray mask for selective intersection (default: all bits set)</param>
    /// <returns>Array of 16 bools where true indicates the ray is occluded</returns>
    public bool[] Occluded16(
        ReadOnlySpan<V3f> origins,
        ReadOnlySpan<V3f> directions,
        float tmin = 0.0f,
        float tmax = float.MaxValue,
        uint mask = 0xFFFFFFFF)
    {
        var results = new bool[16];
        Occluded16(origins, directions, results, tmin, tmax, mask);
        return results;
    }
}
