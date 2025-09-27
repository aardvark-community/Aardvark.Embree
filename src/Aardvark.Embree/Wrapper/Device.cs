using Aardvark.Base;
using System;

namespace Aardvark.Embree;

public class Device : IDisposable
{
    public IntPtr Handle { get; private set; }

    public int ThreadCount { get; }

    /// <summary>
    /// Creates a raytracing device
    /// </summary>
    /// <param name="theradCount">0 uses all thread</param>
    public Device(int threadCount = 0)
    {
        ThreadCount = threadCount;

        var config = String.Format("threads={0}", threadCount);
        Handle = EmbreeAPI.rtcNewDevice(config);
    }

    /// <summary>
    /// Checks and reports if there is a DeviceError.
    /// </summary>
    /// <returns>true if there was an error</returns>
    public bool CheckError(string msg)
    {
        var err = EmbreeAPI.rtcGetDeviceError(Handle);
        if (err != RTCDeviceError.None)
        {
            Report.Warn("[Embree] {0}: {1}", err, msg);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Embree API Version
    /// </summary>
    public Version Version
    {
        get
        {
            // two decimal digits per component
            var version = (int)EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.Version);
            var major = version / 10000;
            var minor = (version / 100) - major * 100;
            var patch = version % 100;
            return new Version(major, minor, patch);
        }
    }

    public bool RayMaskSupported
    {
        get
        {
            var supported = EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.RayMaskSupported);
            return supported != 0;
        }
    }

    public bool FilterFunctionSupported
    {
        get
        {
            var supported = EmbreeAPI.rtcGetDeviceProperty(Handle, RTCDeviceProperty.FilterFunctionSupported);
            return supported != 0;
        }
    }

    public void Dispose()
    {
        EmbreeAPI.rtcReleaseDevice(Handle);
        Handle = IntPtr.Zero;
    }

    //public Scene CreateScene()
    //{
    //}

    //public Buffer CreateBuffer()
    //{
    //}

    //public EmbreeGeometry CreateGeometry()
    //{
    //}
}
