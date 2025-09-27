using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public static class EmbreeBuffer
{
    public static EmbreeBuffer<T> Create<T>(Device device, ReadOnlyMemory<T> data)
        where T : unmanaged
    {
        return new EmbreeBuffer<T>(device, data);
    }

    public static EmbreeBuffer<T> Create<T>(Device device, T[] data)
        where T : unmanaged
    {
        return new EmbreeBuffer<T>(device, data);
    }
}

public class EmbreeBuffer<T> : IDisposable
    where T : unmanaged
{
    private readonly IntPtr m_dataPtr;
    private readonly ulong m_size;

    public IntPtr Handle { get; private set; }

    public EmbreeBuffer(Device device, ReadOnlyMemory<T> data)
    {
        m_size = (ulong)Marshal.SizeOf(typeof(T)) * (ulong)data.Length;

        Handle = EmbreeAPI.rtcNewBuffer(device.Handle, m_size);
        m_dataPtr = EmbreeAPI.rtcGetBufferData(Handle);

        Upload(data);

        device.CheckError("CreateBuffer");
    }

    private unsafe void Upload(ReadOnlyMemory<T> data)
    {
        using var handle = data.Pin();             // pins the actual backing store
        void* src = handle.Pointer;                // pointer to first element of the slice

        nuint bytes = (nuint)(data.Length * sizeof(T));
        Buffer.MemoryCopy(src, (void*)m_dataPtr, m_size, bytes);
    }

    public void Update(ReadOnlyMemory<T> data)
    {
        var size = (ulong)Marshal.SizeOf(typeof(T)) * (ulong)data.Length;
        if (size != m_size) throw new ArgumentException("data size does not match");
        Upload(data);
    }

    public void Dispose()
    {
        EmbreeAPI.rtcReleaseBuffer(Handle);
        Handle = IntPtr.Zero;
    }
}
