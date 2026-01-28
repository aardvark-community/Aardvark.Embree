using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Factory methods for creating typed Embree buffers.
/// </summary>
public static class EmbreeBuffer
{
    /// <summary>
    /// Creates a new Embree buffer from read-only memory.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type</typeparam>
    /// <param name="device">The Embree device</param>
    /// <param name="data">The data to upload to the buffer</param>
    /// <returns>A new EmbreeBuffer instance</returns>
    public static EmbreeBuffer<T> Create<T>(Device device, ReadOnlyMemory<T> data)
        where T : unmanaged
    {
        return new EmbreeBuffer<T>(device, data);
    }

    /// <summary>
    /// Creates a new Embree buffer from an array.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type</typeparam>
    /// <param name="device">The Embree device</param>
    /// <param name="data">The data to upload to the buffer</param>
    /// <returns>A new EmbreeBuffer instance</returns>
    public static EmbreeBuffer<T> Create<T>(Device device, T[] data)
        where T : unmanaged
    {
        return new EmbreeBuffer<T>(device, (ReadOnlyMemory<T>)data);
    }

    /// <summary>
    /// Creates a new Embree buffer from a read-only span (zero-copy).
    /// </summary>
    /// <typeparam name="T">The unmanaged element type</typeparam>
    /// <param name="device">The Embree device</param>
    /// <param name="data">The data to upload to the buffer</param>
    /// <returns>A new EmbreeBuffer instance</returns>
    public static EmbreeBuffer<T> Create<T>(Device device, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        return new EmbreeBuffer<T>(device, data);
    }
}

/// <summary>
/// Typed wrapper for Embree buffers. Manages native buffer lifetime and provides typed access.
/// </summary>
/// <typeparam name="T">The unmanaged element type stored in the buffer</typeparam>
public class EmbreeBuffer<T> : IDisposable
    where T : unmanaged
{
    private readonly IntPtr m_dataPtr;
    private readonly nuint m_size;
    private readonly ReadOnlyMemory<T> m_originalData;
    private IntPtr m_handle;
    private bool m_disposed = false;
    private readonly object m_disposeLock = new object();

    /// <summary>
    /// Throws ObjectDisposedException if this buffer has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (m_disposed)
            throw new ObjectDisposedException($"EmbreeBuffer<{typeof(T).Name}>");
    }

    /// <summary>
    /// Gets the native Embree buffer handle.
    /// </summary>
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
    /// Creates a new Embree buffer and uploads the provided data.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="data">The data to upload</param>
    public EmbreeBuffer(Device device, ReadOnlyMemory<T> data)
    {
        m_size = (nuint)Marshal.SizeOf(typeof(T)) * (nuint)data.Length;
        m_originalData = data;

        Handle = EmbreeAPI.rtcNewBuffer(device.Handle, m_size);
        device.CheckError("EmbreeBuffer.rtcNewBuffer");
        m_dataPtr = EmbreeAPI.rtcGetBufferData(Handle);
        device.CheckError("EmbreeBuffer.rtcGetBufferData");

        Upload(data);
    }

    /// <summary>
    /// Creates a new Embree buffer and uploads the provided data (zero-copy).
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="data">The data to upload</param>
    public EmbreeBuffer(Device device, ReadOnlySpan<T> data)
    {
        m_size = (nuint)Marshal.SizeOf(typeof(T)) * (nuint)data.Length;
        m_originalData = default; // No original data to keep reference to

        Handle = EmbreeAPI.rtcNewBuffer(device.Handle, m_size);
        device.CheckError("EmbreeBuffer.rtcNewBuffer");
        m_dataPtr = EmbreeAPI.rtcGetBufferData(Handle);
        device.CheckError("EmbreeBuffer.rtcGetBufferData");

        Upload(data);
    }

    private unsafe void Upload(ReadOnlyMemory<T> data)
    {
        using var handle = data.Pin();             // pins the actual backing store
        void* src = handle.Pointer;                // pointer to first element of the slice

        nuint bytes = (nuint)(data.Length * sizeof(T));
        Buffer.MemoryCopy(src, (void*)m_dataPtr, m_size, bytes);
    }

    private unsafe void Upload(ReadOnlySpan<T> data)
    {
        fixed (T* src = data)
        {
            nuint bytes = (nuint)(data.Length * sizeof(T));
            Buffer.MemoryCopy(src, (void*)m_dataPtr, m_size, bytes);
        }
    }

    /// <summary>
    /// Updates the buffer with new data. Data size must match the original buffer size.
    /// </summary>
    /// <param name="data">The new data to upload</param>
    public void Update(ReadOnlySpan<T> data)
    {
        ThrowIfDisposed();
        if (data.Length == 0)
            throw new ArgumentException("Data cannot be empty", nameof(data));

        var size = (nuint)Marshal.SizeOf(typeof(T)) * (nuint)data.Length;
        if (size != m_size)
            throw new ArgumentException($"Data size ({size} bytes) does not match buffer size ({m_size} bytes)", nameof(data));

        Upload(data);
    }

    /// <summary>
    /// Gets a pointer to the buffer data for direct manipulation.
    /// Use this for in-place updates, then call geometry.UpdateBuffer() to notify Embree.
    /// </summary>
    public unsafe T* GetDataPointer()
    {
        ThrowIfDisposed();
        return (T*)m_dataPtr;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        lock (m_disposeLock)
        {
            if (m_disposed)
                return;

            if (m_handle != IntPtr.Zero)
            {
                EmbreeAPI.rtcReleaseBuffer(m_handle);
                m_handle = IntPtr.Zero;
            }

            m_disposed = true;
        }
    }
}
