using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree
{
    public class EmbreeBuffer : IDisposable
    {
        private readonly ulong m_size; // size in bytes
        private readonly IntPtr m_dataPtr;
        
        public IntPtr Handle { get; private set; }

        public EmbreeBuffer(Device device, Array data)
        {
            var elementType = data.GetType().GetElementType();
            m_size = (ulong)Marshal.SizeOf(elementType) * (ulong)data.Length;

            Handle = EmbreeAPI.rtcNewBuffer(device.Handle, m_size);
            m_dataPtr = EmbreeAPI.rtcGetBufferData(Handle);

            Upload(data);

            device.CheckError("CreateBuffer");
        }

        void Upload(Array data)
        {
            var sourcePtr = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                unsafe
                {
                    Buffer.MemoryCopy((void*)sourcePtr.AddrOfPinnedObject(), (void*)m_dataPtr, m_size, m_size);
                }
            }
            finally
            { 
                sourcePtr.Free();
            }
        }

        public void Update(Array data)
        {
            var elementType = data.GetType().GetElementType();
            var size = (ulong)Marshal.SizeOf(elementType) * (ulong)data.Length;
            if (size != m_size) throw new ArgumentException("data size does not match");
            Upload(data);
        }

        public void Dispose()
        {
            EmbreeAPI.rtcReleaseBuffer(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
