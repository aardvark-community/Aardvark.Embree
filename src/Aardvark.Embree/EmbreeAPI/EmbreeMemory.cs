using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Embree-specific memory helpers for aligned allocations.
/// </summary>
public static class EmbreeMemory
{
    /// <summary>
    /// Allocates an aligned RTCRayHit block suitable for Embree.
    /// On macOS Intel, uses 32-byte alignment to avoid crashes observed with 16-byte alignment.
    /// </summary>
    public static IntPtr AllocRayHit(out nuint size, out nuint alignment)
    {
        alignment = GetRayHitAlignment();
        size = RoundUpToAlignment((nuint)Marshal.SizeOf<RTCRayHit>(), alignment);
        var alignmentValue = (ulong)alignment;
        var totalSize = (ulong)size + alignmentValue - 1 + (ulong)IntPtr.Size;
        var raw = Marshal.AllocHGlobal((IntPtr)(long)totalSize);
        if (raw == IntPtr.Zero)
            throw new InvalidOperationException("AllocHGlobal returned null.");

        var rawAddress = (ulong)raw.ToInt64();
        var alignedAddress = (rawAddress + (ulong)IntPtr.Size + (alignmentValue - 1)) & ~(alignmentValue - 1);
        var aligned = new IntPtr((long)alignedAddress);

        Marshal.WriteIntPtr(IntPtr.Subtract(aligned, IntPtr.Size), raw);
        return aligned;
    }

    /// <summary>
    /// Frees memory allocated by <see cref="AllocRayHit"/>.
    /// </summary>
    public static void FreeAligned(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return;

        var raw = Marshal.ReadIntPtr(IntPtr.Subtract(ptr, IntPtr.Size));
        Marshal.FreeHGlobal(raw);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCRayHit pointer meets the alignment requirement.
    /// </summary>
    public static void ValidateRayHitAlignment(IntPtr rayHitPtr, string context = null)
    {
        if (rayHitPtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(rayHitPtr));

        var alignment = GetRayHitAlignment();
        var address = (ulong)rayHitPtr.ToInt64();
        if (address % alignment != 0)
        {
            var location = string.IsNullOrWhiteSpace(context) ? string.Empty : $" ({context})";
            throw new InvalidOperationException(
                $"RTCRayHit heap pointer must be {alignment}-byte aligned on this platform{location}. " +
                "Use EmbreeMemory.AllocRayHit or ensure manual alignment.");
        }
    }

    /// <summary>
    /// Validates that a heap-allocated RTCRayHit4 pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidateRayHit4Alignment(IntPtr rayHitPtr, string context = null)
    {
        ValidateAlignment(rayHitPtr, 16, "RTCRayHit4", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCRayHit8 pointer meets the 32-byte alignment requirement.
    /// </summary>
    public static void ValidateRayHit8Alignment(IntPtr rayHitPtr, string context = null)
    {
        ValidateAlignment(rayHitPtr, 32, "RTCRayHit8", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCRayHit16 pointer meets the 64-byte alignment requirement.
    /// </summary>
    public static void ValidateRayHit16Alignment(IntPtr rayHitPtr, string context = null)
    {
        ValidateAlignment(rayHitPtr, 64, "RTCRayHit16", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCPointQuery pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidatePointQueryAlignment(IntPtr queryPtr, string context = null)
    {
        ValidateAlignment(queryPtr, 16, "RTCPointQuery", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCPointQuery4 pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidatePointQuery4Alignment(IntPtr queryPtr, string context = null)
    {
        ValidateAlignment(queryPtr, 16, "RTCPointQuery4", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCPointQuery8 pointer meets the 32-byte alignment requirement.
    /// </summary>
    public static void ValidatePointQuery8Alignment(IntPtr queryPtr, string context = null)
    {
        ValidateAlignment(queryPtr, 32, "RTCPointQuery8", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCPointQuery16 pointer meets the 64-byte alignment requirement.
    /// </summary>
    public static void ValidatePointQuery16Alignment(IntPtr queryPtr, string context = null)
    {
        ValidateAlignment(queryPtr, 64, "RTCPointQuery16", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCPointQueryContext pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidatePointQueryContextAlignment(IntPtr contextPtr, string context = null)
    {
        ValidateAlignment(contextPtr, 16, "RTCPointQueryContext", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCBounds pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidateBoundsAlignment(IntPtr boundsPtr, string context = null)
    {
        ValidateAlignment(boundsPtr, 16, "RTCBounds", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCLinearBounds pointer meets the 16-byte alignment requirement.
    /// </summary>
    public static void ValidateLinearBoundsAlignment(IntPtr boundsPtr, string context = null)
    {
        ValidateAlignment(boundsPtr, 16, "RTCLinearBounds", context);
    }

    /// <summary>
    /// Validates that a heap-allocated RTCBuildPrimitive pointer meets the 32-byte alignment requirement.
    /// </summary>
    public static void ValidateBuildPrimitiveAlignment(IntPtr primitivesPtr, string context = null)
    {
        ValidateAlignment(primitivesPtr, 32, "RTCBuildPrimitive", context);
    }

    private static nuint GetRayHitAlignment()
    {
        if (IsMacosIntel())
        {
            return 32;
        }

        return 16;
    }

    private static bool IsMacosIntel()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
               RuntimeInformation.ProcessArchitecture == Architecture.X64;
    }

    private static void ValidateAlignment(IntPtr ptr, int alignment, string typeName, string context)
    {
        if (ptr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(ptr));

        var address = (ulong)ptr.ToInt64();
        if (address % (ulong)alignment != 0)
        {
            var location = string.IsNullOrWhiteSpace(context) ? string.Empty : $" ({context})";
            throw new InvalidOperationException(
                $"{typeName} pointer must be {alignment}-byte aligned on this platform{location}. " +
                "Use an aligned allocator or ensure manual alignment.");
        }
    }

    private static nuint RoundUpToAlignment(nuint size, nuint alignment)
    {
        var mask = alignment - 1;
        return (size + mask) & ~mask;
    }
}
