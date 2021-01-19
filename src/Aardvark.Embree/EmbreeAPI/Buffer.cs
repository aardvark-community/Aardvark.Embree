using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree
{
    public partial class EmbreeAPI
    {
        /// <summary>
		/// Creates a new buffer. 
		/// </summary>
        [DllImport("embree3.dll")]
		public static extern IntPtr rtcNewBuffer(IntPtr device, ulong byteSize); // TODO use nuint

        /// <summary>
		/// Creates a new shared buffer. 
		/// </summary>
        [DllImport("embree3.dll")]
		public static extern IntPtr rtcNewSharedBuffer(IntPtr device, IntPtr ptr, ulong byteSize); // TODO use nuint

        /// <summary>
		/// Returns a pointer to the buffer data. 
		/// </summary>
        [DllImport("embree3.dll")]
		public static extern IntPtr rtcGetBufferData(IntPtr buffer);

        /// <summary>
		/// Retains the buffer (increments the reference count). 
		/// </summary>
        [DllImport("embree3.dll")]
		public static extern void rtcRetainBuffer(IntPtr buffer);

        /// <summary>
		/// Releases the buffer (decrements the reference count). 
		/// </summary>
        [DllImport("embree3.dll")]
		public static extern void rtcReleaseBuffer(IntPtr buffer);
    }
}
