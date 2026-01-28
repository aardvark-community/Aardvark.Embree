using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// P/Invoke bindings for Intel Embree 4 ray tracing library
/// </summary>
public partial class EmbreeAPI
{
        /// <summary>
	/// Creates a new buffer. 
	/// </summary>
        [DllImport("embree4")]
	public static extern IntPtr rtcNewBuffer(IntPtr device, nuint byteSize);

        /// <summary>
	/// Creates a new shared buffer.
	/// </summary>
        [DllImport("embree4")]
	public static extern IntPtr rtcNewSharedBuffer(IntPtr device, IntPtr ptr, nuint byteSize);

        /// <summary>
	/// Returns a pointer to the buffer data.
	/// </summary>
        [DllImport("embree4")]
	public static extern IntPtr rtcGetBufferData(IntPtr buffer);

        /// <summary>
	/// Retains the buffer (increments the reference count).
	/// </summary>
        [DllImport("embree4")]
	public static extern void rtcRetainBuffer(IntPtr buffer);

        /// <summary>
	/// Releases the buffer (decrements the reference count).
	/// </summary>
        [DllImport("embree4")]
	public static extern void rtcReleaseBuffer(IntPtr buffer);
    }
