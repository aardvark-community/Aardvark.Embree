using System;

namespace Aardvark.Embree;

/// <summary>
/// Error codes returned by Embree operations.
/// Check after API calls using rtcGetDeviceError or Device.CheckError.
/// </summary>
public enum RTCDeviceError : uint
{
    /// <summary>
    /// No error occurred (RTC_ERROR_NONE).
    /// </summary>
    None = 0,

    /// <summary>
    /// An unknown error has occurred (RTC_ERROR_UNKNOWN).
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// An invalid argument was specified (RTC_ERROR_INVALID_ARGUMENT).
    /// </summary>
    InvalidArgument = 2,

    /// <summary>
    /// The operation is not allowed for the specified object (RTC_ERROR_INVALID_OPERATION).
    /// </summary>
    InvalidOperation = 3,

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

/// <summary>
/// Properties that can be queried from an Embree device.
/// Use rtcGetDeviceProperty to retrieve property values.
/// </summary>
public enum RTCDeviceProperty : uint
{
    /// <summary>Full version number (RTC_DEVICE_PROPERTY_VERSION)</summary>
    Version = 0,
    /// <summary>Major version number (RTC_DEVICE_PROPERTY_VERSION_MAJOR)</summary>
    VersionMajor = 1,
    /// <summary>Minor version number (RTC_DEVICE_PROPERTY_VERSION_MINOR)</summary>
    VersionMinor = 2,
    /// <summary>Patch version number (RTC_DEVICE_PROPERTY_VERSION_PATCH)</summary>
    VersionPatch = 3,

    /// <summary>Whether native 4-wide ray packets are supported (RTC_DEVICE_PROPERTY_NATIVE_RAY4_SUPPORTED)</summary>
    NativeRay4Supported = 32,
    /// <summary>Whether native 8-wide ray packets are supported (RTC_DEVICE_PROPERTY_NATIVE_RAY8_SUPPORTED)</summary>
    NativeRay8Supported = 33,
    /// <summary>Whether native 16-wide ray packets are supported (RTC_DEVICE_PROPERTY_NATIVE_RAY16_SUPPORTED)</summary>
    NativeRay16Supported = 34,
    /// <summary>Whether native ray stream API is supported (RTC_DEVICE_PROPERTY_RAY_STREAM_SUPPORTED)</summary>
    NativeRayStreamSupported = 35,

    /// <summary>Whether backface culling for curves is enabled (RTC_DEVICE_PROPERTY_BACKFACE_CULLING_CURVES_ENABLED)</summary>
    BackfaceCullingCurvesEnabled = 63,
    /// <summary>Whether ray masking is supported (RTC_DEVICE_PROPERTY_RAY_MASK_SUPPORTED)</summary>
    RayMaskSupported = 64,
    /// <summary>Whether backface culling is enabled (RTC_DEVICE_PROPERTY_BACKFACE_CULLING_ENABLED)</summary>
    BackfaceCullingEnabled = 65,
    /// <summary>Whether filter functions are supported (RTC_DEVICE_PROPERTY_FILTER_FUNCTION_SUPPORTED)</summary>
    FilterFunctionSupported = 66,
    /// <summary>Whether invalid rays are ignored (RTC_DEVICE_PROPERTY_IGNORE_INVALID_RAYS_ENABLED)</summary>
    IgnoreInvalidRaysEnabled = 67,
    /// <summary>Whether compact polygon representation is enabled (RTC_DEVICE_PROPERTY_COMPACT_POLYS_ENABLED)</summary>
    CompactPolysEnabled = 68,

    /// <summary>Whether triangle geometry is supported (RTC_DEVICE_PROPERTY_TRIANGLE_GEOMETRY_SUPPORTED)</summary>
    TriangleGeometrySupported = 96,
    /// <summary>Whether quad geometry is supported (RTC_DEVICE_PROPERTY_QUAD_GEOMETRY_SUPPORTED)</summary>
    QuadGeometrySupported = 97,
    /// <summary>Whether subdivision surface geometry is supported (RTC_DEVICE_PROPERTY_SUBDIVISION_GEOMETRY_SUPPORTED)</summary>
    SubdivisionGeometrySupported = 98,
    /// <summary>Whether curve geometry is supported (RTC_DEVICE_PROPERTY_CURVE_GEOMETRY_SUPPORTED)</summary>
    CurveGeometrySupported = 99,
    /// <summary>Whether user-defined geometry is supported (RTC_DEVICE_PROPERTY_USER_GEOMETRY_SUPPORTED)</summary>
    UserGeometrySupported = 100,
    /// <summary>Whether point geometry is supported (RTC_DEVICE_PROPERTY_POINT_GEOMETRY_SUPPORTED)</summary>
    PointGeometrySupported = 101,

    /// <summary>The tasking system in use (RTC_DEVICE_PROPERTY_TASKING_SYSTEM)</summary>
    TaskingSystem = 128,
    /// <summary>Whether joint scene commit is supported (RTC_DEVICE_PROPERTY_JOIN_COMMIT_SUPPORTED)</summary>
    JointCommitSupported = 129,
    /// <summary>Whether parallel scene commit is supported (RTC_DEVICE_PROPERTY_PARALLEL_COMMIT_SUPPORTED)</summary>
    ParallelCommitSupported = 130,
}

/// <summary>
/// Flags that control scene behavior and optimization strategy.
/// Can be combined using bitwise OR.
/// </summary>
[Flags]
public enum RTCSceneFlags : uint
{
    /// <summary>No special flags (RTC_SCENE_FLAG_NONE)</summary>
    None = 0x0,
    /// <summary>Optimize for dynamic scenes with frequent geometry updates (RTC_SCENE_FLAG_DYNAMIC)</summary>
    Dynamic = 0x1,
    /// <summary>Use compact BVH representation to reduce memory usage (RTC_SCENE_FLAG_COMPACT)</summary>
    Compact = 0x2,
    /// <summary>Use more robust but slower intersection algorithms (RTC_SCENE_FLAG_ROBUST)</summary>
    Robust = 0x4,
    /// <summary>Enable context parameter in filter functions (RTC_SCENE_FLAG_CONTEXT_FILTER_FUNCTION)</summary>
    ContextFilterFunction = 0x8,
}

/// <summary>
/// BVH build quality levels. Higher quality improves ray tracing performance but increases build time.
/// </summary>
public enum RTCBuildQuality : uint
{
    /// <summary>Low quality, fast BVH build (RTC_BUILD_QUALITY_LOW)</summary>
    Low = 0,
    /// <summary>Medium quality, balanced build time and performance (RTC_BUILD_QUALITY_MEDIUM)</summary>
    Medium = 1,
    /// <summary>High quality, slower build but best ray tracing performance (RTC_BUILD_QUALITY_HIGH)</summary>
    High = 2,
    /// <summary>Refit existing BVH after geometry updates, fastest for dynamic scenes (RTC_BUILD_QUALITY_REFIT)</summary>
    Refit = 3,
}

/// <summary>
/// Ray query flags that control ray traversal optimization strategy.
/// </summary>
/// <remarks>
/// Use None (or Incoherent) for instance geometries and randomly distributed rays.
/// Use Coherent for primary rays or other rays with similar directions.
/// </remarks>
[Flags]
public enum RTCRayQueryFlags : uint
{
    /// <summary>No special optimization, suitable for instance geometries (RTC_RAY_QUERY_FLAG_NONE)</summary>
    None = 0,
    /// <summary>Optimize for incoherent (randomly distributed) rays (RTC_RAY_QUERY_FLAG_INCOHERENT)</summary>
    Incoherent = 0,
    /// <summary>Optimize for coherent (similar direction) rays like primary rays (RTC_RAY_QUERY_FLAG_COHERENT)</summary>
    Coherent = 1,
}

/// <summary>
/// Intersection context flags (deprecated, use RTCRayQueryFlags)
/// </summary>
[Flags]
[Obsolete("Use RTCRayQueryFlags instead")]
public enum RTCIntersectContextFlags : uint
{
    /// <summary>No special optimization</summary>
    None = 0,
    /// <summary>Optimize for incoherent rays</summary>
    Incoherent = 0,
    /// <summary>Optimize for coherent rays</summary>
    Coherent = 1,
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
    NormalOrientedHermiteCurve = 42,

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
    /// <summary>
    /// instance array geometry (RTC_GEOMETRY_TYPE_INSTANCE_ARRAY)
    /// Memory-efficient representation of large numbers of instances
    /// </summary>
    InstanceArray = 122,
}

/// <summary>
/// Interpolation modes for subdivision surface boundaries.
/// Controls how edges and vertices are handled during subdivision.
/// </summary>
public enum RTCSubdivisionMode : uint
{
    /// <summary>No special boundary handling (RTC_SUBDIVISION_MODE_NO_BOUNDARY)</summary>
    NoBoundary = 0,
    /// <summary>Smooth boundary edges (RTC_SUBDIVISION_MODE_SMOOTH_BOUNDARY)</summary>
    SmoothBoundary = 1,
    /// <summary>Pin (fix) corner vertices only (RTC_SUBDIVISION_MODE_PIN_CORNERS)</summary>
    PinCorners = 2,
    /// <summary>Pin all boundary edges (RTC_SUBDIVISION_MODE_PIN_BOUNDARY)</summary>
    PinBoundary = 3,
    /// <summary>Pin all boundary edges and vertices (RTC_SUBDIVISION_MODE_PIN_ALL)</summary>
    PinAll = 4,
}

/// <summary>
/// Buffer types for geometry data.
/// Different geometry types require different buffer types.
/// </summary>
public enum RTCBufferType
{
    /// <summary>Index buffer for triangle/quad indices (RTC_BUFFER_TYPE_INDEX)</summary>
    Index = 0,
    /// <summary>Vertex position buffer (RTC_BUFFER_TYPE_VERTEX)</summary>
    Vertex = 1,
    /// <summary>Custom vertex attribute buffer (RTC_BUFFER_TYPE_VERTEX_ATTRIBUTE)</summary>
    VertexAttribute = 2,
    /// <summary>Vertex normal buffer (RTC_BUFFER_TYPE_NORMAL)</summary>
    Normal = 3,
    /// <summary>Vertex tangent buffer (RTC_BUFFER_TYPE_TANGENT)</summary>
    Tangent = 4,
    /// <summary>Normal derivative buffer (RTC_BUFFER_TYPE_NORMAL_DERIVATIVE)</summary>
    NormalDerivative = 5,

    /// <summary>Grid topology buffer for grid geometries (RTC_BUFFER_TYPE_GRID)</summary>
    Grid = 8,

    /// <summary>Face buffer for subdivision surfaces (RTC_BUFFER_TYPE_FACE)</summary>
    Face = 16,
    /// <summary>Subdivision level buffer (RTC_BUFFER_TYPE_LEVEL)</summary>
    Level = 17,
    /// <summary>Edge crease index buffer for subdivision surfaces (RTC_BUFFER_TYPE_EDGE_CREASE_INDEX)</summary>
    EdgeCreaseIndex = 18,
    /// <summary>Edge crease weight buffer for subdivision surfaces (RTC_BUFFER_TYPE_EDGE_CREASE_WEIGHT)</summary>
    EdgeCreaseWeight = 19,
    /// <summary>Vertex crease index buffer for subdivision surfaces (RTC_BUFFER_TYPE_VERTEX_CREASE_INDEX)</summary>
    VertexCreaseIndex = 20,
    /// <summary>Vertex crease weight buffer for subdivision surfaces (RTC_BUFFER_TYPE_VERTEX_CREASE_WEIGHT)</summary>
    VertexCreaseWeight = 21,
    /// <summary>Hole buffer marking faces as holes in subdivision surfaces (RTC_BUFFER_TYPE_HOLE)</summary>
    Hole = 22,

    /// <summary>Transform buffer for instance arrays (RTC_BUFFER_TYPE_TRANSFORM)</summary>
    Transform = 23,

    /// <summary>Flags buffer for various geometry properties (RTC_BUFFER_TYPE_FLAGS)</summary>
    Flags = 32,
}

/// <summary>
/// Data formats for buffers and other data structures.
/// Specifies the type, component count, and layout of data.
/// </summary>
public enum RTCFormat : uint
{
    /// <summary>Undefined format</summary>
    UNDEFINED = 0,

    /// <summary>Single 8-bit unsigned integer</summary>
    UCHAR = 0x1001,
    /// <summary>2-component 8-bit unsigned integer vector</summary>
    UCHAR2,
    /// <summary>3-component 8-bit unsigned integer vector</summary>
    UCHAR3,
    /// <summary>4-component 8-bit unsigned integer vector</summary>
    UCHAR4,

    /// <summary>Single 8-bit signed integer</summary>
    CHAR = 0x2001,
    /// <summary>2-component 8-bit signed integer vector</summary>
    CHAR2,
    /// <summary>3-component 8-bit signed integer vector</summary>
    CHAR3,
    /// <summary>4-component 8-bit signed integer vector</summary>
    CHAR4,

    /// <summary>Single 16-bit unsigned integer</summary>
    USHORT = 0x3001,
    /// <summary>2-component 16-bit unsigned integer vector</summary>
    USHORT2,
    /// <summary>3-component 16-bit unsigned integer vector</summary>
    USHORT3,
    /// <summary>4-component 16-bit unsigned integer vector</summary>
    USHORT4,

    /// <summary>Single 16-bit signed integer</summary>
    SHORT = 0x4001,
    /// <summary>2-component 16-bit signed integer vector</summary>
    SHORT2,
    /// <summary>3-component 16-bit signed integer vector</summary>
    SHORT3,
    /// <summary>4-component 16-bit signed integer vector</summary>
    SHORT4,

    /// <summary>Single 32-bit unsigned integer</summary>
    UINT = 0x5001,
    /// <summary>2-component 32-bit unsigned integer vector</summary>
    UINT2,
    /// <summary>3-component 32-bit unsigned integer vector</summary>
    UINT3,
    /// <summary>4-component 32-bit unsigned integer vector</summary>
    UINT4,

    /// <summary>Single 32-bit signed integer</summary>
    INT = 0x6001,
    /// <summary>2-component 32-bit signed integer vector</summary>
    INT2,
    /// <summary>3-component 32-bit signed integer vector</summary>
    INT3,
    /// <summary>4-component 32-bit signed integer vector</summary>
    INT4,

    /// <summary>Single 64-bit unsigned integer</summary>
    ULLONG = 0x7001,
    /// <summary>2-component 64-bit unsigned integer vector</summary>
    ULLONG2,
    /// <summary>3-component 64-bit unsigned integer vector</summary>
    ULLONG3,
    /// <summary>4-component 64-bit unsigned integer vector</summary>
    ULLONG4,

    /// <summary>Single 64-bit signed integer</summary>
    LLONG = 0x8001,
    /// <summary>2-component 64-bit signed integer vector</summary>
    LLONG2,
    /// <summary>3-component 64-bit signed integer vector</summary>
    LLONG3,
    /// <summary>4-component 64-bit signed integer vector</summary>
    LLONG4,

    /// <summary>Single 32-bit float</summary>
    FLOAT = 0x9001,
    /// <summary>2-component 32-bit float vector</summary>
    FLOAT2,
    /// <summary>3-component 32-bit float vector</summary>
    FLOAT3,
    /// <summary>4-component 32-bit float vector</summary>
    FLOAT4,
    /// <summary>5-component 32-bit float vector</summary>
    FLOAT5,
    /// <summary>6-component 32-bit float vector</summary>
    FLOAT6,
    /// <summary>7-component 32-bit float vector</summary>
    FLOAT7,
    /// <summary>8-component 32-bit float vector</summary>
    FLOAT8,
    /// <summary>9-component 32-bit float vector</summary>
    FLOAT9,
    /// <summary>10-component 32-bit float vector</summary>
    FLOAT10,
    /// <summary>11-component 32-bit float vector</summary>
    FLOAT11,
    /// <summary>12-component 32-bit float vector</summary>
    FLOAT12,
    /// <summary>13-component 32-bit float vector</summary>
    FLOAT13,
    /// <summary>14-component 32-bit float vector</summary>
    FLOAT14,
    /// <summary>15-component 32-bit float vector</summary>
    FLOAT15,
    /// <summary>16-component 32-bit float vector</summary>
    FLOAT16,

    /// <summary>2x2 matrix, 32-bit float, row-major order</summary>
    FLOAT2X2_ROW_MAJOR = 0x9122,
    /// <summary>2x3 matrix, 32-bit float, row-major order</summary>
    FLOAT2X3_ROW_MAJOR = 0x9123,
    /// <summary>2x4 matrix, 32-bit float, row-major order</summary>
    FLOAT2X4_ROW_MAJOR = 0x9124,
    /// <summary>3x2 matrix, 32-bit float, row-major order</summary>
    FLOAT3X2_ROW_MAJOR = 0x9132,
    /// <summary>3x3 matrix, 32-bit float, row-major order</summary>
    FLOAT3X3_ROW_MAJOR = 0x9133,
    /// <summary>3x4 matrix, 32-bit float, row-major order (used for instance transforms)</summary>
    FLOAT3X4_ROW_MAJOR = 0x9134,
    /// <summary>4x2 matrix, 32-bit float, row-major order</summary>
    FLOAT4X2_ROW_MAJOR = 0x9142,
    /// <summary>4x3 matrix, 32-bit float, row-major order</summary>
    FLOAT4X3_ROW_MAJOR = 0x9143,
    /// <summary>4x4 matrix, 32-bit float, row-major order</summary>
    FLOAT4X4_ROW_MAJOR = 0x9144,

    /// <summary>2x2 matrix, 32-bit float, column-major order</summary>
    FLOAT2X2_COLUMN_MAJOR = 0x9222,
    /// <summary>2x3 matrix, 32-bit float, column-major order</summary>
    FLOAT2X3_COLUMN_MAJOR = 0x9223,
    /// <summary>2x4 matrix, 32-bit float, column-major order</summary>
    FLOAT2X4_COLUMN_MAJOR = 0x9224,
    /// <summary>3x2 matrix, 32-bit float, column-major order</summary>
    FLOAT3X2_COLUMN_MAJOR = 0x9232,
    /// <summary>3x3 matrix, 32-bit float, column-major order</summary>
    FLOAT3X3_COLUMN_MAJOR = 0x9233,
    /// <summary>3x4 matrix, 32-bit float, column-major order</summary>
    FLOAT3X4_COLUMN_MAJOR = 0x9234,
    /// <summary>4x2 matrix, 32-bit float, column-major order</summary>
    FLOAT4X2_COLUMN_MAJOR = 0x9242,
    /// <summary>4x3 matrix, 32-bit float, column-major order</summary>
    FLOAT4X3_COLUMN_MAJOR = 0x9243,
    /// <summary>4x4 matrix, 32-bit float, column-major order</summary>
    FLOAT4X4_COLUMN_MAJOR = 0x9244,

    /// <summary>Special 12-byte format for grid geometries</summary>
    GRID = 0xA001
}
