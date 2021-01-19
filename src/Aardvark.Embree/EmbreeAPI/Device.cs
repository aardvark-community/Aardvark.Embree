using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree
{
    public partial class EmbreeAPI
    {
        /// <summary>
        /// Creates a new Embree device.
        /// </summary>
        [DllImport("embree3.dll", CharSet=CharSet.Ansi)]
        public static extern IntPtr rtcNewDevice(string config = null);

        /// <summary>
        /// Retains the Embree device (increments the reference count).
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcRetainDevice(IntPtr device);

        /// <summary>
        /// Releases an Embree device (decrements the reference count).
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcReleaseDevice(IntPtr device);

        /// <summary>
        /// Returns the error code.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern RTCDeviceError rtcGetDeviceError(IntPtr device);

        /// <summary>
        /// Gets a device property.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern long rtcGetDeviceProperty(IntPtr device, RTCDeviceProperty prop); // TODO: nint instead of long

        /// <summary>
        /// Sets a device property.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcSetDeviceProperty(IntPtr device, RTCDeviceProperty prop, long value); // TODO: nint instead of long
    }
}
