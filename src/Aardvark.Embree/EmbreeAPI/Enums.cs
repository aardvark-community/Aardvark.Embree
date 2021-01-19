using System;

namespace Aardvark.Embree
{
    public enum RTCDeviceError : uint
    {
        /// <summary>
        /// No error occurred (RTC_ERROR_NONE).
        /// </summary>
        None = 0,

        /// <summary>
        /// An unknown error has occurred (RTC_ERROR_UNKNOWN).
        /// </summary>
        Unknow = 1,

        /// <summary>
        /// An invalid argument was specified (RTC_ERROR_INVALID_ARGUMENT).
        /// </summary>
        InvalidArgument = 2,

        /// <summary>
        /// The operation is not allowed for the specified object (RTC_ERROR_INVALID_OPERATION).
        /// </summary>
        InvaildOperation = 3,

        /// <summary>
        /// There is not enough memory left to complete the operation (RTC_ERROR_OUT_OF_MEMORY).
        /// </summary>
        OutOfMemory = 4,

        /// <summary>
        /// The CPU is not supported as it does not support the lowest ISA Embree is compiled for (RTC_ERROR_UNSUPPORTED_CPU).
        /// </summary>
        UnsupportedCPU = 5,

        /// <summary>
        /// The operation got canceled by a memory monitor callback or progress monitor callback function (RTC_ERROR_CANCELLED).
        /// </summary>
        Cancelled = 6
    }

    public enum RTCDeviceProperty : uint
    {
        Version = 0, // RTC_DEVICE_PROPERTY_VERSION
        VersionMajor = 1, // RTC_DEVICE_PROPERTY_VERSION_MAJOR
        VersionMinor = 2, // RTC_DEVICE_PROPERTY_VERSION_MINOR
        VersionPatch = 3, // RTC_DEVICE_PROPERTY_VERSION_PATCH

        NativeRay4Supported = 32, // RTC_DEVICE_PROPERTY_NATIVE_RAY4_SUPPORTED
        NativeRay8Supported = 33, // RTC_DEVICE_PROPERTY_NATIVE_RAY8_SUPPORTED
        NativeRay16Supported = 34, // RTC_DEVICE_PROPERTY_NATIVE_RAY16_SUPPORTED
        NativeRayStreamSupported = 35, // RTC_DEVICE_PROPERTY_RAY_STREAM_SUPPORTED

        BackfaceCullingCurvesEnabled = 63, // RTC_DEVICE_PROPERTY_BACKFACE_CULLING_CURVES_ENABLED
        RayMaskSupported = 64, //RTC_DEVICE_PROPERTY_RAY_MASK_SUPPORTED
        BackfaceCullingEnabled = 65, //RTC_DEVICE_PROPERTY_BACKFACE_CULLING_ENABLED
        FilterFunctionSupported = 66, //RTC_DEVICE_PROPERTY_FILTER_FUNCTION_SUPPORTED
        IgnoreInvalidRaysEnabled = 67, //RTC_DEVICE_PROPERTY_IGNORE_INVALID_RAYS_ENABLED
        CompactPolysEnabled = 68, //RTC_DEVICE_PROPERTY_COMPACT_POLYS_ENABLED

        TriangleGeometrySupported = 96, //RTC_DEVICE_PROPERTY_TRIANGLE_GEOMETRY_SUPPORTED
        QuadGeometrySupported = 97, //RTC_DEVICE_PROPERTY_QUAD_GEOMETRY_SUPPORTED
        SubdivisionGeometrySupported = 98, //RTC_DEVICE_PROPERTY_SUBDIVISION_GEOMETRY_SUPPORTED
        CurveGeometrySupported = 99, //RTC_DEVICE_PROPERTY_CURVE_GEOMETRY_SUPPORTED
        UserGeometrySupported = 100, //RTC_DEVICE_PROPERTY_USER_GEOMETRY_SUPPORTED
        PointGeometrySupported = 101, //RTC_DEVICE_PROPERTY_POINT_GEOMETRY_SUPPORTED

        TaskingSystem = 128, //RTC_DEVICE_PROPERTY_TASKING_SYSTEM
        JointCommitSupported = 129, //RTC_DEVICE_PROPERTY_JOIN_COMMIT_SUPPORTED
        ParallelCommitSupported = 130, //RTC_DEVICE_PROPERTY_PARALLEL_COMMIT_SUPPORTED
    }

    [Flags]
    public enum RTCSceneFlags : uint
    {
        None = 0x0, //RTC_SCENE_FLAG_NONE
        Dynamic = 0x1, //RTC_SCENE_FLAG_DYNAMIC
        Compact = 0x2, //RTC_SCENE_FLAG_COMPACT
        Robust = 0x4, // RTC_SCENE_FLAG_ROBUST
        ContextFilterFunction = 0x8, // RTC_SCENE_FLAG_CONTEXT_FILTER_FUNCTION
    }

    public enum RTCBuildQuality : uint
    {
        Low = 0, // RTC_BUILD_QUALITY_LOW
        Medium = 1, // RTC_BUILD_QUALITY_MEDIUM
        High = 2, // RTC_BUILD_QUALITY_HIGH
        Refit = 3, //RTC_BUILD_QUALITY_REFIT
    }

    /// <summary>
    /// Intersection context flags
    /// </summary>
    [Flags]
    public enum RTCIntersectContextFlags : uint
    {
        None = 0, // RTC_INTERSECT_CONTEXT_FLAG_NONE
        Incoherent = 0, // optimize for incoherent rays (RTC_INTERSECT_CONTEXT_FLAG_INCOHERENT)
        Coherent = 1, // optimize for coherent rays (RTC_INTERSECT_CONTEXT_FLAG_COHERENT)
    }

    /// <summary>
    /// Types of geometries 
    /// </summary>
    public enum RTCGeometryType : uint
    {
        /// <summary>
        /// triangle mesh (RTC_GEOMETRY_TYPE_TRIANGLE)
        /// </summary>
        Triangle = 0,
        /// <summary>
        /// quad (triangle pair) mesh (RTC_GEOMETRY_TYPE_QUAD)
        /// </summary>
        Quad = 1,
        /// <summary>
        /// grid mesh (RTC_GEOMETRY_TYPE_GRID)
        /// </summary>
        Grid = 2,

        /// <summary>
        /// Catmull-Clark subdivision surface (RTC_GEOMETRY_TYPE_SUBDIVISION)
        /// </summary>
        Subdivision = 8,

        /// <summary>
        /// Cone linear curves - discontinuous at edge boundaries (RTC_GEOMETRY_TYPE_CONE_LINEAR_CURVE)
        /// </summary>
        ConeLinearCurve = 15,
        /// <summary>
        /// Round (rounded cone like) linear curves (RTC_GEOMETRY_TYPE_ROUND_LINEAR_CURVE)
        /// </summary>
        RoundLinearCurve = 16,
        /// <summary>
        /// flat (ribbon-like) linear curves (RTC_GEOMETRY_TYPE_FLAT_LINEAR_CURVE)
        /// </summary>
        FlatLinearCurve = 17,

        /// <summary>
        /// round (tube-like) Bezier curves (RTC_GEOMETRY_TYPE_ROUND_BEZIER_CURVE)
        /// </summary>
        RoundBezierCurve = 24,
        /// <summary>
        /// flat (ribbon-like) Bezier curves (RTC_GEOMETRY_TYPE_FLAT_BEZIER_CURVE)
        /// </summary>
        FlatBezierCurve = 25,
        /// <summary>
        /// flat normal-oriented Bezier curves (RTC_GEOMETRY_TYPE_NORMAL_ORIENTED_BEZIER_CURVE)
        /// </summary>
        NormalOrientedBezierCurve = 26,

        /// <summary>
        /// round (tube-like) B-spline curves (RTC_GEOMETRY_TYPE_ROUND_BSPLINE_CURVE)
        /// </summary>
        RoundBsplineCurve = 32,
        /// <summary>
        /// flat (ribbon-like) B-spline curves (RTC_GEOMETRY_TYPE_FLAT_BSPLINE_CURVE)
        /// </summary>
        FlatBsplineCurve = 33,
        /// <summary>
        /// flat normal-oriented B-spline curves (RTC_GEOMETRY_TYPE_NORMAL_ORIENTED_BSPLINE_CURVE)
        /// </summary>
        NormalOrientedBsplineCurve = 34,

        /// <summary>
        /// round (tube-like) Hermite curves (RTC_GEOMETRY_TYPE_ROUND_HERMITE_CURVE)
        /// </summary>
        RoundHermiteCurve = 40,
        /// <summary>
        /// flat (ribbon-like) Hermite curves (RTC_GEOMETRY_TYPE_FLAT_HERMITE_CURVE)
        /// </summary>
        FlatHermiteCurve = 41,
        /// <summary>
        /// flat normal-oriented Hermite curves (RTC_GEOMETRY_TYPE_NORMAL_ORIENTED_HERMITE_CURVE)
        /// </summary>
        NormalOrientedHermiteCuve = 42,

        /// <summary>
        /// RTC_GEOMETRY_TYPE_SPHERE_POINT
        /// </summary>
        SpherePoint = 50,
        /// <summary>
        /// RTC_GEOMETRY_TYPE_DISC_POINT
        /// </summary>
        DiscPoint = 51,
        /// <summary>
        /// RTC_GEOMETRY_TYPE_ORIENTED_DISC_POINT
        /// </summary>
        OrientedDiscPoint = 52,

        /// <summary>
        /// round (tube-like) Catmull-Rom curves (RTC_GEOMETRY_TYPE_ROUND_CATMULL_ROM_CURVE)
        /// </summary>
        RoundCatmullRomCurve = 58,
        /// <summary>
        /// flat (ribbon-like) Catmull-Rom curves (RTC_GEOMETRY_TYPE_FLAT_CATMULL_ROM_CURVE)
        /// </summary>
        FlatCatmullRomCurve = 59,
        /// <summary>
        /// flat normal-oriented Catmull-Rom curves (RTC_GEOMETRY_TYPE_NORMAL_ORIENTED_CATMULL_ROM_CURVE)
        /// </summary>
        NormalOrientedCatmullRomCurve = 60,

        /// <summary>
        /// user-defined geometry (RTC_GEOMETRY_TYPE_USER)
        /// </summary>
        User = 120,
        /// <summary>
        /// scene instance (RTC_GEOMETRY_TYPE_INSTANCE)
        /// </summary>
        Instance = 121,
    }

    /// <summary>
    /// Interpolation modes for subdivision surfaces 
    /// </summary>
    public enum RTCSubdivisionMode : uint
    {
        NoBoundary = 0, // RTC_SUBDIVISION_MODE_NO_BOUNDARY
        SmoothBoundary = 1, // RTC_SUBDIVISION_MODE_SMOOTH_BOUNDARY
        PinCorners = 2, // RTC_SUBDIVISION_MODE_PIN_CORNERS
        PinBoundary = 3, // RTC_SUBDIVISION_MODE_PIN_BOUNDARY
        PinAll = 4, // RTC_SUBDIVISION_MODE_PIN_ALL
    }

    /// <summary>
    /// Types of buffers 
    /// </summary>
    public enum RTCBufferType
    {
        Index = 0, // RTC_BUFFER_TYPE_INDEX
        Vertex = 1, // RTC_BUFFER_TYPE_VERTEX
        VertexAttirbute = 2, // RTC_BUFFER_TYPE_VERTEX_ATTRIBUTE
        Normal = 3, // RTC_BUFFER_TYPE_NORMAL
        Tangent = 4, // RTC_BUFFER_TYPE_TANGENT
        NormalDerivative = 5, // RTC_BUFFER_TYPE_NORMAL_DERIVATIVE

        Grid = 8, // RTC_BUFFER_TYPE_GRID

        Face = 16, // RTC_BUFFER_TYPE_FACE
        Level = 17, // RTC_BUFFER_TYPE_LEVEL
        EdgeCreaseIndex = 18, // RTC_BUFFER_TYPE_EDGE_CREASE_INDEX
        EdgeCreaseWeight = 19, // RTC_BUFFER_TYPE_EDGE_CREASE_WEIGHT
        VertexCreaseIndex = 20, // RTC_BUFFER_TYPE_VERTEX_CREASE_INDEX
        VertexCreaseWeight = 21, // RTC_BUFFER_TYPE_VERTEX_CREASE_WEIGHT
        Hole = 22, // RTC_BUFFER_TYPE_HOLE

        Flags = 32, // RTC_BUFFER_TYPE_FLAGS
    }

    /// <summary>
    /// Formats of buffers and other data structures 
    /// </summary>
    public enum RTCFormat : uint
    {
        UNDEFINED = 0,

        // 8-bit unsigned integer
        UCHAR = 0x1001,
        UCHAR2,
        UCHAR3,
        UCHAR4,

        // 8-bit signed integer 
        CHAR = 0x2001,
        CHAR2,
        CHAR3,
        CHAR4,

        // 16-bit unsigned integer 
        USHORT = 0x3001,
        USHORT2,
        USHORT3,
        USHORT4,

        // 16-bit signed integer 
        SHORT = 0x4001,
        SHORT2,
        SHORT3,
        SHORT4,

        // 32-bit unsigned integer 
        UINT = 0x5001,
        UINT2,
        UINT3,
        UINT4,

        // 32-bit signed integer 
        INT = 0x6001,
        INT2,
        INT3,
        INT4,

        // 64-bit unsigned integer 
        ULLONG = 0x7001,
        ULLONG2,
        ULLONG3,
        ULLONG4,

        // 64-bit signed integer 
        LLONG = 0x8001,
        LLONG2,
        LLONG3,
        LLONG4,

        // 32-bit float 
        FLOAT = 0x9001,
        FLOAT2,
        FLOAT3,
        FLOAT4,
        FLOAT5,
        FLOAT6,
        FLOAT7,
        FLOAT8,
        FLOAT9,
        FLOAT10,
        FLOAT11,
        FLOAT12,
        FLOAT13,
        FLOAT14,
        FLOAT15,
        FLOAT16,

        // 32-bit float matrix (row-major order) 
        FLOAT2X2_ROW_MAJOR = 0x9122,
        FLOAT2X3_ROW_MAJOR = 0x9123,
        FLOAT2X4_ROW_MAJOR = 0x9124,
        FLOAT3X2_ROW_MAJOR = 0x9132,
        FLOAT3X3_ROW_MAJOR = 0x9133,
        FLOAT3X4_ROW_MAJOR = 0x9134,
        FLOAT4X2_ROW_MAJOR = 0x9142,
        FLOAT4X3_ROW_MAJOR = 0x9143,
        FLOAT4X4_ROW_MAJOR = 0x9144,

        // 32-bit float matrix (column-major order) 
        FLOAT2X2_COLUMN_MAJOR = 0x9222,
        FLOAT2X3_COLUMN_MAJOR = 0x9223,
        FLOAT2X4_COLUMN_MAJOR = 0x9224,
        FLOAT3X2_COLUMN_MAJOR = 0x9232,
        FLOAT3X3_COLUMN_MAJOR = 0x9233,
        FLOAT3X4_COLUMN_MAJOR = 0x9234,
        FLOAT4X2_COLUMN_MAJOR = 0x9242,
        FLOAT4X3_COLUMN_MAJOR = 0x9243,
        FLOAT4X4_COLUMN_MAJOR = 0x9244,

        // special 12-byte format for grids 
        GRID = 0xA001
    }
}
