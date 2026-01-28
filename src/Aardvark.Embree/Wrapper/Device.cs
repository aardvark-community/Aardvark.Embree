using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Represents an Embree device, the entry point to the Embree ray tracing API.
/// </summary>
/// <remarks>
/// <para>
/// A Device creates and manages all Embree resources (scenes, geometries, buffers).
/// The device is not thread-safe for modification operations, but scenes created by the device
/// support thread-safe ray queries after being committed.
/// </para>
/// <para>
/// Must be disposed to release native Embree resources. Dispose the device after all dependent
/// scenes and geometries have been disposed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var device = new Device(threadCount: 0); // Use all CPU threads
/// Console.WriteLine($"Embree version: {device.Version}");
///
/// using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
/// // Attach geometries and commit scene...
/// </code>
/// </example>
public class Device : IDisposable
{
    private IntPtr m_handle;
    private bool m_disposed = false;
    private readonly object m_disposeLock = new();

    /// <summary>
    /// Throws ObjectDisposedException if this device has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (m_disposed)
            throw new ObjectDisposedException(nameof(Device));
    }

    /// <summary>
    /// Gets the native Embree device handle.
    /// </summary>
    /// <remarks>
    /// This handle is required when calling low-level Embree API functions.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    public IntPtr Handle
    {
        get
        {
            ThrowIfDisposed();
            return m_handle;
        }
        private set => m_handle = value;
    }

    /// <summary>
    /// Gets the number of threads configured for this device.
    /// </summary>
    /// <remarks>
    /// A value of 0 indicates that Embree uses all available CPU threads.
    /// </remarks>
    public int ThreadCount { get; }

    /// <summary>
    /// Creates a new Embree ray tracing device.
    /// </summary>
    /// <param name="threadCount">Number of threads for parallel operations. Use 0 to automatically use all available CPU threads.</param>
    /// <remarks>
    /// <para>
    /// The device is the entry point to Embree and is required to create scenes and geometries.
    /// Embree will use the specified number of threads for BVH construction and ray queries.
    /// </para>
    /// <para>
    /// This constructor initializes the native Embree device and checks for creation errors.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if threadCount is negative.</exception>
    /// <example>
    /// <code>
    /// // Use all available CPU cores
    /// using var device = new Device();
    ///
    /// // Use exactly 4 threads
    /// using var device = new Device(threadCount: 4);
    /// </code>
    /// </example>
    public Device(int threadCount = 0)
    {
        if (threadCount < 0)
            throw new ArgumentOutOfRangeException(nameof(threadCount), "Thread count must be non-negative");

        ThreadCount = threadCount;

        var config = $"threads={threadCount}";
        m_handle = EmbreeAPI.rtcNewDevice(config);
        CheckError("Device creation");
    }

    /// <summary>
    /// Checks for Embree device errors and reports them.
    /// </summary>
    /// <param name="msg">Context message to include in error report.</param>
    /// <returns>True if an error was detected, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method queries the Embree device for error state. Critical system errors (OutOfMemory, UnsupportedCPU,
    /// Unknown) throw exceptions immediately. Other errors (InvalidOperation, InvalidArgument, Cancelled) are logged
    /// as warnings to allow graceful degradation.
    /// </para>
    /// <para>
    /// Call this method after operations that may fail (geometry attachment, scene commit, etc.) to detect
    /// and report errors immediately.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    /// <exception cref="OutOfMemoryException">Thrown if Embree reports out of memory error.</exception>
    /// <exception cref="NotSupportedException">Thrown if CPU does not support required instruction sets.</exception>
    /// <exception cref="InvalidOperationException">Thrown for unknown Embree errors.</exception>
    public bool CheckError(string msg)
    {
        ThrowIfDisposed();
        var err = EmbreeAPI.rtcGetDeviceError(Handle);
        if (err != RTCDeviceError.None)
        {
            // Get detailed error message if available (Embree 4.3.3+)
            var detailPtr = EmbreeAPI.rtcGetDeviceLastErrorMessage(Handle);
            var detail = detailPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(detailPtr) : null;

            var errorMsg = !string.IsNullOrEmpty(detail)
                ? $"[Embree] {err}: {msg} - {detail}"
                : $"[Embree] {err}: {msg}";

            // Only throw for truly critical system errors that cannot be recovered from
            switch (err)
            {
                case RTCDeviceError.OutOfMemory:
                    throw new OutOfMemoryException(errorMsg);

                case RTCDeviceError.UnsupportedCPU:
                    throw new NotSupportedException(errorMsg + (!string.IsNullOrEmpty(detail) ? "" : " - CPU does not support required instruction sets (SSE2 minimum)"));

                case RTCDeviceError.Unknown:
                    throw new InvalidOperationException(errorMsg + (!string.IsNullOrEmpty(detail) ? "" : " - Unknown Embree error occurred"));

                // Other errors (InvalidOperation, InvalidArgument, Cancelled) are logged as warnings
                // These may indicate programming errors but can potentially be recovered from
                default:
                    Report.Warn(errorMsg);
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets a string representation of an Embree error code (Embree 4.3.3+).
    /// </summary>
    /// <param name="error">The error code to convert to string.</param>
    /// <returns>A string describing the error.</returns>
    /// <remarks>
    /// This is a static method that does not require a device instance.
    /// Useful for error reporting and logging.
    /// </remarks>
    public static string GetErrorString(RTCDeviceError error)
    {
        var ptr = EmbreeAPI.rtcGetErrorString(error);
        return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : error.ToString();
    }

    /// <summary>
    /// Sets a memory monitor callback to track Embree memory allocations and deallocations.
    /// </summary>
    /// <param name="callback">Callback function invoked for memory operations. Pass null to disable monitoring.</param>
    /// <remarks>
    /// <para>
    /// The callback is invoked twice for each allocation: before (post=false) and after (post=true).
    /// For deallocations, bytes will be negative. Returning false from the callback when post=false
    /// can abort the allocation.
    /// </para>
    /// <para>
    /// Useful for tracking memory usage, implementing custom memory limits, or debugging memory issues.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    /// <example>
    /// <code>
    /// using var device = new Device();
    /// long totalAllocated = 0;
    ///
    /// device.SetMemoryMonitorFunction((bytes, post) => {
    ///     if (post) {
    ///         totalAllocated += bytes;
    ///         Console.WriteLine($"Memory: {totalAllocated / (1024 * 1024)} MB");
    ///     }
    ///     return true; // Allow allocation
    /// });
    /// </code>
    /// </example>
    public void SetMemoryMonitorFunction(Func<long, bool, bool> callback)
    {
        ThrowIfDisposed();

        if (callback == null)
        {
            EmbreeAPI.rtcSetDeviceMemoryMonitorFunction(Handle, null, IntPtr.Zero);
        }
        else
        {
            // Wrap the user callback to match the native signature
            RTCMemoryMonitorFunction nativeCallback = (ptr, bytes, post) => callback(bytes, post);
            EmbreeAPI.rtcSetDeviceMemoryMonitorFunction(Handle, nativeCallback, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Sets a custom error callback for the device.
    /// </summary>
    /// <param name="callback">Callback function invoked when errors occur. Pass null to disable custom error handling.</param>
    /// <remarks>
    /// <para>
    /// The callback is invoked whenever an Embree error occurs, allowing custom error handling,
    /// logging, or diagnostics. The callback receives the error code and a descriptive message.
    /// </para>
    /// <para>
    /// Useful for integrating Embree error reporting with application logging systems or
    /// implementing custom error recovery strategies.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    /// <example>
    /// <code>
    /// using var device = new Device();
    ///
    /// device.SetErrorFunction((error, message) => {
    ///     Console.WriteLine($"Embree Error [{error}]: {message}");
    ///     // Custom error handling logic here
    /// });
    /// </code>
    /// </example>
    public void SetErrorFunction(Action<RTCDeviceError, string> callback)
    {
        ThrowIfDisposed();

        if (callback == null)
        {
            EmbreeAPI.rtcSetDeviceErrorFunction(Handle, null, IntPtr.Zero);
        }
        else
        {
            // Wrap the user callback to match the native signature
            RTCErrorFunction nativeCallback = (userPtr, code, message) => callback(code, message);
            EmbreeAPI.rtcSetDeviceErrorFunction(Handle, nativeCallback, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Gets the Embree API version.
    /// </summary>
    /// <remarks>
    /// Returns the version of the Embree library loaded by this device (e.g., 4.4.0).
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    public Version Version
    {
        get
        {
            ThrowIfDisposed();
            // two decimal digits per component
            var version = (int)EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.Version);
            var major = version / 10000;
            var minor = (version / 100) - major * 100;
            var patch = version % 100;
            return new Version(major, minor, patch);
        }
    }

    /// <summary>
    /// Gets whether ray masking is supported by this device.
    /// </summary>
    /// <remarks>
    /// When true, rays can be selectively filtered by geometry using ray masks.
    /// This feature is hardware and configuration dependent.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    public bool RayMaskSupported
    {
        get
        {
            ThrowIfDisposed();
            var supported = EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.RayMaskSupported);
            return supported != 0;
        }
    }

    /// <summary>
    /// Gets whether filter functions are supported by this device.
    /// </summary>
    /// <remarks>
    /// When true, custom filter callbacks can be used to programmatically accept or reject ray hits.
    /// This feature is hardware and configuration dependent.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
    public bool FilterFunctionSupported
    {
        get
        {
            ThrowIfDisposed();
            var supported = EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.FilterFunctionSupported);
            return supported != 0;
        }
    }

    /// <summary>
    /// Releases all resources used by the device.
    /// </summary>
    /// <remarks>
    /// Disposes the native Embree device handle. Dispose all dependent scenes and geometries before disposing the device.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the device and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    /// <remarks>
    /// This method is thread-safe and uses double-check locking to ensure the device is disposed exactly once.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        lock (m_disposeLock)
        {
            if (m_disposed)
                return;

            if (m_handle != IntPtr.Zero)
            {
                EmbreeAPI.rtcReleaseDevice(m_handle);
                m_handle = IntPtr.Zero;
            }

            m_disposed = true;
        }
    }
}
