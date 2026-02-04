# Aardvark.Embree

[![Windows](https://github.com/aardvark-community/aardvark.embree/actions/workflows/windows.yml/badge.svg)](https://github.com/aardvark-community/aardvark.embree/actions/workflows/windows.yml)
[![Publish](https://github.com/aardvark-community/aardvark.embree/actions/workflows/publish.yml/badge.svg)](https://github.com/aardvark-community/aardvark.embree/actions/workflows/publish.yml)
[![Nuget](https://img.shields.io/nuget/vpre/Aardvark.Embree)](https://www.nuget.org/packages/Aardvark.Embree/)
[![Downloads](https://img.shields.io/nuget/dt/Aardvark.Embree)](https://www.nuget.org/packages/Aardvark.Embree/)

.NET bindings for Intel Embree 4 ray tracing kernels—ray-triangle intersection, occlusion tests, and closest-point queries with cross-platform native libraries included.

## API Coverage

**Implemented:**
- ✅ 100% of CPU-based ray tracing APIs
- ✅ All geometry types: triangles, quads, curves, points, subdivision surfaces, grids, instances, user-defined
- ✅ Motion blur, ray packets (SIMD), filter functions, displacement mapping, interpolation, collision detection

**Not Implemented:**
- ❌ GPU/SYCL APIs
- Requires Intel Arc GPU hardware + SYCL runtime + C++/SYCL interop

## Install

```bash
dotnet add package Aardvark.Embree
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Windows (x64), Linux (x64), or macOS (x64/ARM64)

### Heap Alignment (Direct P/Invoke)

Embree requires aligned ray/hit buffers for direct P/Invoke calls:
- `RTCRayHit`: 16-byte aligned on all platforms; macOS Intel uses 32-byte alignment in this wrapper to avoid crashes.
- `RTCRayHit4`: 16-byte aligned
- `RTCRayHit8`: 32-byte aligned
- `RTCRayHit16`: 64-byte aligned
- `RTCPointQuery` / `RTCPointQueryContext`: 16-byte aligned
- `RTCPointQuery8`: 32-byte aligned
- `RTCPointQuery16`: 64-byte aligned
- `RTCBuildPrimitive`: 32-byte aligned (BVH builder primitives)

Use the built-in helper:

```csharp
var ptr = EmbreeMemory.AllocRayHit(out _, out _);
EmbreeMemory.ValidateRayHitAlignment(ptr, "my direct P/Invoke");
```

The high-level wrapper (`Scene.Intersect`) already uses safe stack allocations.

### Your First Ray Trace

Minimal example:

```csharp
using Aardvark.Base;
using Aardvark.Embree;

// Initialize native library loader (required once at startup)
Aardvark.Base.Aardvark.Init();

// Device manages Embree's thread pool and memory
using var device = new Device();

var vertices = new V3f[]
{
    new V3f(0, 0, 0),
    new V3f(1, 0, 0),
    new V3f(0, 1, 0)
};
var indices = new int[] { 0, 1, 2 };

using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

// Scene groups geometries into a single BVH; use one per ray-traced "world" or instanced object
using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
scene.AttachGeometry(geometry);

// Commit builds the BVH; must be called after geometry changes, before queries
scene.Commit();

// Ray from (0.25, 0.25, 1) pointing down; should hit triangle at Z=0
var hit = new RayHit();
bool intersected = scene.Intersect(
    rayOrigin: new V3f(0.25f, 0.25f, 1.0f),
    rayDirection: new V3f(0, 0, -1),
    ref hit
);

if (intersected)
{
    Console.WriteLine($"Hit! Distance: {hit.T:F3}");
    var hitPoint = new V3f(0.25f, 0.25f, 1.0f) + new V3f(0, 0, -1) * hit.T;
    Console.WriteLine($"Hit point: ({hitPoint.X:F2}, {hitPoint.Y:F2}, {hitPoint.Z:F2})");
}
else
{
    Console.WriteLine("No intersection.");
}
```

### Expected Output

```
Hit! Distance: 1.000
Hit point: (0.25, 0.25, 0.00)
```

### Explanation

1. `Aardvark.Init()` - Extracts embedded native DLLs and registers loader
2. `Device` - Allocates Embree thread pool and memory manager
3. `TriangleGeometry` - Triangle data copied to Embree-managed memory
4. `Scene` - Creates BVH (bounding volume hierarchy) for O(log n) ray queries
5. `Commit()` - Finalizes BVH; required after geometry changes
6. `Intersect()` - BVH traversal finds hits in O(log n) time
7. Result - Distance 1.0 = ray traveled 1 unit before hitting Z=0

### Next Steps

- **More Examples**: See `src/Aardvark.Embree.Examples/` for examples including:
  - Multiple geometry types (curves, points, subdivision surfaces)
  - Motion blur and animation
  - Instancing and transformations
  - Performance optimization techniques
- **Advanced Features**: Continue reading the Usage section below for occlusion testing, closest point queries, and specialized geometries
- **API Documentation**: Review the API Reference section for complete method signatures

## Usage

```csharp
using Aardvark.Embree;

using var device = new Device();
var vertices = new[] { new V3f(0,0,0), new V3f(1,0,0), new V3f(0,1,0) };
var indices = new[] { 0, 1, 2 };
var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
scene.AttachGeometry(geometry);
scene.Commit();

// Intersect returns the closest hit along the ray
var hit = new RayHit();
bool intersected = scene.Intersect(
    rayOrigin: new V3f(0.25f, 0.25f, 1.0f),
    rayDirection: new V3f(0, 0, -1),
    ref hit
);

// GetClosestPoint finds the nearest surface point to a query location
// Note: currently supported for TriangleGeometry attached directly to the scene (instances are not supported).
var result = scene.GetClosestPoint(queryPoint: new V3f(0.5f, 0.5f, 1.0f));
if (result.IsValid) {
    var point = result.Point;
    var uv = result.UV;
    var distance = result.DistanceSquared.Sqrt();
}

// Occluded is faster than Intersect when you only need a boolean answer
bool occluded = scene.Occluded(
    rayOrigin: new V3f(0, 0, 1),
    rayDirection: new V3f(0, 0, -1),
    minT: 0.0f, maxT: 10.0f
);
```

### Curve Geometry

```csharp
using var device = new Device();

// Create curve vertices with position and radius
var curveVertices = new CurveVertex[]
{
    new CurveVertex(new V3f(0, 0, 0), 0.1f),
    new CurveVertex(new V3f(1, 1, 0), 0.15f),
    new CurveVertex(new V3f(2, 0, 0), 0.1f)
};

var indices = new uint[] { 0 };

// Round: swept sphere along curve; use for hair, cables, tubes
using var roundCurve = new RoundBezierCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);

// Flat: camera-facing ribbon; cheaper than round, good for grass/fur cards
using var flatCurve = new FlatBSplineCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);

// NormalOriented: ribbon with explicit orientation; for anisotropic shading
using var orientedCurve = new NormalOrientedHermiteCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);
```

### Point Geometry

```csharp
using var device = new Device();

// Spheres: true 3D spheres; use for particles, point clouds, atoms
var spherePoints = new Point[]
{
    new Point(new V3f(0, 0, 0), 0.5f),
    new Point(new V3f(1, 1, 1), 0.3f)
};
using var spheres = new SpherePointGeometry(device, spherePoints, RTCBuildQuality.High);

// Discs: flat circles facing ray origin; cheaper than spheres for splats
var discPoints = new Point[]
{
    new Point(new V3f(0, 0, 0), 0.5f)
};
using var discs = new DiscPointGeometry(device, discPoints, RTCBuildQuality.High);

// OrientedDiscs: flat circles with explicit normal; for oriented point clouds
var orientedPoints = new OrientedPoint[]
{
    new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
};
using var orientedDiscs = new OrientedDiscPointGeometry(device, orientedPoints, RTCBuildQuality.High);
```

### Subdivision Surfaces

```csharp
using var device = new Device();

// Coarse control cage; Embree tessellates at ray time
var vertices = new V3f[]
{
    new V3f(-1, -1, 0), new V3f(1, -1, 0),
    new V3f(1, 1, 0), new V3f(-1, 1, 0)
};

var indices = new uint[] { 0, 1, 2, 3 };
var faces = new uint[] { 4 };

// tessellationRate controls triangle density; higher = smoother but slower
using var subdiv = new SubdivisionGeometry(
    device, vertices, indices, faces,
    RTCBuildQuality.High,
    RTCSubdivisionMode.SmoothBoundary,
    tessellationRate: 4.0f
);

// Creases create sharp edges; weight infinity = perfectly sharp
var edgeIndices = new uint[] { 0, 1 };
var edgeWeights = new float[] { 5.0f };
subdiv.SetEdgeCreases(device, edgeIndices, edgeWeights);
```

### Grid Meshes

```csharp
using var device = new Device();

uint width = 10;
uint height = 10;

// Grids are more memory-efficient than indexed triangles for regular height fields
var vertices = new V3f[width * height];
for (uint y = 0; y < height; y++)
{
    for (uint x = 0; x < width; x++)
    {
        float z = (float)Math.Sin(x * 0.5f) * Math.Cos(y * 0.5f);
        vertices[y * width + x] = new V3f(x, y, z);
    }
}

using var grid = new GridGeometry(device, vertices, width, height, RTCBuildQuality.High);
```

### User-Defined Geometry

```csharp
using var device = new Device();

// boundsFunc: Embree calls this to build the BVH; return conservative AABB
RTCBoundsFunction boundsFunc = (args) =>
{
    var bounds = (RTCBounds*)args.bounds_o;
    bounds->lower_x = -1.0f; bounds->lower_y = -1.0f; bounds->lower_z = -1.0f;
    bounds->upper_x = 1.0f; bounds->upper_y = 1.0f; bounds->upper_z = 1.0f;
};

// intersectFunc: Embree calls this when a ray enters the AABB; you compute the actual hit
RTCIntersectFunction intersectFunc = (args) =>
{
    // Your intersection logic: compute hit.t, hit.u, hit.v and set args.valid if ray hits
};

using var userGeom = new UserGeometry(
    device,
    primitiveCount: 1,
    boundsFunc: boundsFunc,
    intersectFunc: intersectFunc,
    quality: RTCBuildQuality.High
);
```

### Zero-Copy Span<T> API

Span overloads copy directly into Embree buffers, avoiding intermediate array allocations:

```csharp
using var device = new Device();

// stackalloc avoids heap allocation entirely
Span<V3f> vertices = stackalloc V3f[3]
{
    new V3f(0, 0, 0),
    new V3f(1, 0, 0),
    new V3f(0, 1, 0)
};

Span<int> indices = stackalloc int[3] { 0, 1, 2 };

// Data copied directly to Embree-managed memory; no intermediate heap array
using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

Span<CurveVertex> curveVerts = stackalloc CurveVertex[2]
{
    new CurveVertex(new V3f(0, 0, 0), 0.1f),
    new CurveVertex(new V3f(1, 0, 0), 0.1f)
};
Span<uint> curveIndices = stackalloc uint[1] { 0 };
using var curve = new RoundLinearCurveGeometry(device, curveVerts, curveIndices, RTCBuildQuality.High);
```

### Motion Blur

Animate geometry across time with linear or multi-segment motion blur:

```csharp
using var device = new Device();

// Two vertex sets define start/end positions; Embree interpolates per-ray time
var verticesT0 = new V3f[]
{
    new V3f(-1, -1, 2),
    new V3f(1, -1, 2),
    new V3f(0, 1, 2)
};

var verticesT1 = new V3f[]
{
    new V3f(-1, -1, 4),
    new V3f(1, -1, 4),
    new V3f(0, 1, 4)
};

var indices = new int[] { 0, 1, 2 };

using var geom = new MotionBlurGeometry(device,
    new ReadOnlyMemory<V3f>(verticesT0),
    new ReadOnlyMemory<V3f>(verticesT1),
    new ReadOnlyMemory<int>(indices),
    RTCBuildQuality.High);

using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
scene.AttachGeometry(geom);
scene.Commit();

// time=0.5 means geometry is halfway between T0 and T1 positions
var hit = new RayHit();
bool intersected = scene.Intersect(
    rayOrigin: new V3f(0, 0, 0),
    rayDirection: new V3f(0, 0, 1),
    ref hit,
    minT: 0.0f,
    maxT: float.MaxValue,
    time: 0.5f
);

// N time steps for non-linear motion (e.g., acceleration, curved paths)
var timeSteps = new ReadOnlyMemory<V3f>[]
{
    new ReadOnlyMemory<V3f>(verticesT0),
    new ReadOnlyMemory<V3f>(verticesT1),
    new ReadOnlyMemory<V3f>(verticesT2),
    new ReadOnlyMemory<V3f>(verticesT3)
};
using var multiStepGeom = new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), RTCBuildQuality.High);

// SetTimeRange limits when geometry is visible; useful for appearing/disappearing objects
geom.SetTimeRange(0.2f, 0.8f);
geom.Commit();
```

### Dynamic Geometry Updates

Update vertex data without reallocating buffers; BVH is refitted rather than rebuilt:
Note: update methods require the same element count as the original buffers.

```csharp
using var device = new Device();

var vertices = new V3f[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
var indices = new int[] { 0, 1, 2 };

using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

// dynamic:true tells Embree to optimize for frequent updates (faster refit, slower traversal)
using var scene = new Scene(device, RTCBuildQuality.High, dynamic: true);
scene.AttachGeometry(geometry);
scene.Commit();

var newVertices = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1) };
geometry.UpdateVertices(new ReadOnlyMemory<V3f>(newVertices));
scene.Commit();

Span<V3f> updatedVertices = stackalloc V3f[3]
{
    new V3f(0, 0, 2),
    new V3f(1, 0, 2),
    new V3f(0, 1, 2)
};
geometry.UpdateVertices(updatedVertices);
scene.Commit();

// Direct pointer access avoids all copying; you write directly into Embree's buffer
unsafe
{
    V3f* vertexPtr = geometry.GetVertexDataPointer();
    vertexPtr[0] = new V3f(0, 0, 3);
    vertexPtr[1] = new V3f(1, 0, 3);
    vertexPtr[2] = new V3f(0, 1, 3);
    geometry.Commit();
    scene.Commit();
}
```

### Async Commit

Overlap CPU work with BVH building by using async commit:

```csharp
using var device = new Device();
using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

// Attach geometries
scene.AttachGeometry(geometry1);
scene.AttachGeometry(geometry2);

// Start BVH build in background
scene.CommitAsync();

// Do other work here while BVH builds
PrepareRenderData();
UpdateAnimations();

// Wait for BVH build to complete before ray queries
scene.CommitAsyncWait();

// Now safe to query
scene.Intersect(rayOrigin, rayDirection, ref hit);
```

Thread safety: `CommitAsync()` and `CommitAsyncWait()` are not thread-safe with respect to scene modifications. After `CommitAsyncWait()` returns, ray queries are thread-safe.

### Half-Edge Topology

Query subdivision surface connectivity for mesh analysis:

```csharp
using var device = new Device();

var vertices = new V3f[]
{
    new V3f(-1, -1, 0), new V3f(1, -1, 0),
    new V3f(1, 1, 0), new V3f(-1, 1, 0)
};
var indices = new uint[] { 0, 1, 2, 3 };
var faces = new uint[] { 4 };

using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
    RTCBuildQuality.High, RTCSubdivisionMode.SmoothBoundary, 4.0f);

// Get topology query object
var topology = subdiv.GetTopology();

// Walk around a face
foreach (var edgeID in topology.EnumerateFaceHalfEdges(faceID: 0))
{
    var oppositeEdge = topology.GetOppositeHalfEdge(edgeID);
    if (oppositeEdge != uint.MaxValue)
    {
        // Found adjacent face
        var neighborFace = topology.GetFace(oppositeEdge);
    }
    else
    {
        // Boundary edge (no neighbor)
    }
}

// Navigate topology
var firstEdge = topology.GetFirstHalfEdge(faceID: 0);
var nextEdge = topology.GetNextHalfEdge(firstEdge);
var prevEdge = topology.GetPreviousHalfEdge(firstEdge);
```

### Displacement Mapping

Add procedural detail to subdivision surfaces:

```csharp
using var device = new Device();

var vertices = new V3f[]
{
    new V3f(-1, -1, 0), new V3f(1, -1, 0),
    new V3f(1, 1, 0), new V3f(-1, 1, 0)
};
var indices = new uint[] { 0, 1, 2, 3 };
var faces = new uint[] { 4 };

using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
    RTCBuildQuality.High, RTCSubdivisionMode.SmoothBoundary, 4.0f);

// Set displacement function for procedural height mapping
unsafe
{
    RTCDisplacementFunctionN displacementFunc = (args) =>
    {
        var N = (int)args->N;
        var u = args->u;
        var v = args->v;
        var Ng_x = args->Ng_x;
        var Ng_y = args->Ng_y;
        var Ng_z = args->Ng_z;
        var posX = args->x;
        var posY = args->y;
        var posZ = args->z;

        for (int i = 0; i < N; i++)
        {
            // Sample height map or procedural function
            float height = (float)(Math.Sin(u[i] * 5.0f) * Math.Cos(v[i] * 5.0f)) * 0.2f;

            // Displace along normal
            posX[i] += Ng_x[i] * height;
            posY[i] += Ng_y[i] * height;
            posZ[i] += Ng_z[i] * height;
        }
    };

    subdiv.SetDisplacementFunction(displacementFunc);
}

subdiv.Commit(); // Must commit after setting displacement function
```

## Common Integration Patterns

### Working with Double Precision Geometry

Embree uses single precision (float) exclusively for all computations. Double precision is not supported. This design choice enables SIMD performance optimizations that are critical for real-time ray tracing. For applications that use double precision geometry, use coordinate offsetting to work within float precision constraints:

```csharp
using var device = new Device();

// Your application geometry (double precision)
var vertices = new V3d[]
{
    new V3d(1000000.5, 2000000.3, 3000000.8),  // Large world coordinates
    new V3d(1000001.2, 2000000.5, 3000001.1),
    new V3d(1000000.8, 2000001.0, 3000000.9)
};

// 1. Compute local origin to minimize precision loss
var boundingBox = new Box3d(vertices);
var center = boundingBox.Center;

// 2. Convert to local coordinates (V3d → V3f)
var localVertices = new V3f[vertices.Length];
for (int i = 0; i < vertices.Length; i++)
    localVertices[i] = (V3f)(vertices[i] - center);

// 3. Build Embree scene with local coordinates
var indices = new int[] { 0, 1, 2 };
using var geometry = new TriangleGeometry(device, localVertices, indices, RTCBuildQuality.High);
using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
scene.AttachGeometry(geometry);
scene.Commit();

// 4. Transform query rays to local space
var worldRayOrigin = new V3d(1000000.0, 2000000.0, 3000010.0);
var worldRayDirection = new V3d(0, 0, -1);

var localRayOrigin = (V3f)(worldRayOrigin - center);
var localRayDirection = (V3f)worldRayDirection;

var hit = new RayHit();
if (scene.Intersect(localRayOrigin, localRayDirection, ref hit))
{
    // 5. Transform results back to world space
    var localHitPoint = localRayOrigin + localRayDirection * hit.T;
    var worldHitPoint = new V3d(localHitPoint) + center;

    Console.WriteLine($"World hit point: {worldHitPoint}");
}
```

Precision: ±1e-7 relative error. Required when geometry spans large coordinates (>1e6 units).

### Backend Abstraction Pattern

Wrap Embree behind an interface:

```csharp
public interface IRayTracingBackend
{
    bool Intersect(V3d origin, V3d direction, out RayHit hit);
    bool Occluded(V3d origin, V3d direction, float maxDistance);
    V3d GetClosestPoint(V3d queryPoint, out float distanceSquared);
}

public class EmbreeBackend : IRayTracingBackend
{
    private readonly Device _device;
    private readonly Scene _scene;
    private readonly V3d _offset;  // Coordinate offset

    public EmbreeBackend(V3d[] vertices, int[] indices)
    {
        _device = new Device();

        // Compute local origin
        var bbox = new Box3d(vertices);
        _offset = bbox.Center;

        // Convert to local coordinates
        var localVertices = new V3f[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            localVertices[i] = (V3f)(vertices[i] - _offset);

        var geometry = new TriangleGeometry(_device, localVertices, indices, RTCBuildQuality.High);

        _scene = new Scene(_device, RTCBuildQuality.High, dynamic: false);
        _scene.AttachGeometry(geometry);
        _scene.Commit();
    }

    public bool Intersect(V3d origin, V3d direction, out RayHit hit)
    {
        hit = new RayHit();
        var localOrigin = (V3f)(origin - _offset);
        var localDirection = (V3f)direction;

        return _scene.Intersect(localOrigin, localDirection, ref hit);
    }

    public V3d GetClosestPoint(V3d queryPoint, out float distanceSquared)
    {
        var localQuery = (V3f)(queryPoint - _offset);
        var result = _scene.GetClosestPoint(localQuery);

        if (!result.IsValid)
        {
            distanceSquared = float.PositiveInfinity;
            return V3d.NaN;
        }

        distanceSquared = result.DistanceSquared;
        return new V3d(result.Point) + _offset;
    }

    public void Dispose()
    {
        _scene?.Dispose();
        _device?.Dispose();
    }
}
```

### Performance Optimization: Ray Batching

Process multiple rays efficiently using SIMD ray packets:

```csharp
using var device = new Device();
using var scene = CreateScene(device);

// Collect rays (e.g., camera rays, shadow rays)
var rayOrigins = new List<V3f>();
var rayDirections = new List<V3f>();

// ... fill ray lists ...

// Process in batches of 8 (AVX2)
const int batchSize = 8;
var results = new List<RayHit>();

for (int i = 0; i < rayOrigins.Count; i += batchSize)
{
    int count = Math.Min(batchSize, rayOrigins.Count - i);

    // Prepare batch
    var batchOrigins = new V3f[8];
    var batchDirections = new V3f[8];

    for (int j = 0; j < count; j++)
    {
        batchOrigins[j] = rayOrigins[i + j];
        batchDirections[j] = rayDirections[i + j];
    }

    // Fill remaining slots for full SIMD width
    for (int j = count; j < 8; j++)
    {
        batchOrigins[j] = batchOrigins[0];
        batchDirections[j] = batchDirections[0];
    }

    // Single SIMD call processes 8 rays (4-6x faster)
    var hits = scene.Intersect8(batchOrigins, batchDirections);

    // Extract valid results
    for (int j = 0; j < count; j++)
        results.Add(hits[j]);
}
```

### Dynamic Scenes for Animated Geometry

Update geometry efficiently without rebuilding the entire BVH:

```csharp
using var device = new Device();

// Initial setup
var vertices = new V3f[] { /* ... */ };
var indices = new int[] { /* ... */ };

using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

// Mark scene as dynamic for fast updates
using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: true);
scene.AttachGeometry(geometry);
scene.Commit();

// Animation loop
while (animating)
{
    // Update vertex positions
    UpdateVertexPositions(vertices);

    // Update Embree geometry (fast refit)
    geometry.UpdateVertices(new ReadOnlyMemory<V3f>(vertices));
    scene.Commit();  // 10-100x faster than full rebuild

    // Perform ray queries
    scene.Intersect(rayOrigin, rayDirection, ref hit);
}
```

**Performance tips:**
- Use `dynamic: true` for scenes with frequent updates
- Use `RTCBuildQuality.Medium` or `Refit` for dynamic scenes
- Call `UpdateVertices` instead of recreating geometry
- BVH refit is much faster than full rebuild

### Zero-Copy Updates with Direct Memory Access

For maximum performance, write directly to Embree's vertex buffer:

```csharp
using var device = new Device();

var vertices = new V3f[3] { /* ... */ };
var indices = new int[3] { 0, 1, 2 };

using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: true);
scene.AttachGeometry(geometry);
scene.Commit();

// Per-frame update with zero-copy
unsafe
{
    V3f* vertexPtr = geometry.GetVertexDataPointer();

    // Write directly into Embree's buffer (no copying)
    vertexPtr[0] = ComputeNewPosition(0);
    vertexPtr[1] = ComputeNewPosition(1);
    vertexPtr[2] = ComputeNewPosition(2);

    geometry.Commit();  // Mark geometry as modified
    scene.Commit();     // Refit BVH
}

scene.Intersect(rayOrigin, rayDirection, ref hit);
```

Zero allocation, zero copy, direct CPU cache access.

## API Reference

### Device
- `Device(int threadCount = 0)` - Create Embree device
- `Version` - Embree version
- `ThreadCount` - Number of threads
- `RayMaskSupported`, `FilterFunctionSupported` - Feature flags
- `CheckError(string msg)` - Check for Embree errors with detailed messages
- `GetErrorString(RTCDeviceError error)` - Convert error code to string (static)
- `SetMemoryMonitorFunction(Func<long, bool, bool> callback)` - Track memory allocations/deallocations
- `SetErrorFunction(Action<RTCDeviceError, string> callback)` - Custom error handling

### Scene
- `AttachGeometry(EmbreeGeometry)` - Add geometry
- `Commit()` - Build acceleration structure
- `Intersect(V3f origin, V3f direction, ref RayHit hit, ...)` - Ray cast
- `Intersect(V3f origin, V3f direction, ref RayHit hit, float minT, float maxT, float time, ...)` - Ray cast with motion blur time
- `Occluded(V3f origin, V3f direction, ...)` - Shadow ray
- `GetClosestPoint(V3f queryPoint, float maxRadius = float.MaxValue)` - Nearest point (TriangleGeometry only; instances not supported)
- `Bounds` - Scene bounding box

### Geometry Types

**Triangle and Quad Meshes:**
- `TriangleGeometry(Device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<int> indices, RTCBuildQuality)`
  - `UpdateVertices(ReadOnlyMemory<V3f>)` / `UpdateVertices(ReadOnlySpan<V3f>)` - Update vertex data
  - `GetVertexDataPointer()` - Direct pointer access
- `QuadGeometry(Device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<int> quadIndices, RTCBuildQuality)`
  - `UpdateVertices(ReadOnlyMemory<V3f>)` / `UpdateVertices(ReadOnlySpan<V3f>)` - Update vertex data
  - `GetVertexDataPointer()` - Direct pointer access

**Curve Geometries (15 types):**
- `RoundBezierCurveGeometry`, `FlatBezierCurveGeometry`, `NormalOrientedBezierCurveGeometry`
- `RoundBSplineCurveGeometry`, `FlatBSplineCurveGeometry`, `NormalOrientedBSplineCurveGeometry`
- `RoundHermiteCurveGeometry`, `FlatHermiteCurveGeometry`, `NormalOrientedHermiteCurveGeometry`
- `RoundCatmullRomCurveGeometry`, `FlatCatmullRomCurveGeometry`, `NormalOrientedCatmullRomCurveGeometry`
- `RoundLinearCurveGeometry`, `FlatLinearCurveGeometry`, `NormalOrientedLinearCurveGeometry`
- All curve types support:
  - `UpdateVertices(ReadOnlyMemory<CurveVertex>)` / `UpdateVertices(ReadOnlySpan<CurveVertex>)` - Update curve data
  - `GetVertexDataPointer()` - Direct pointer access

**Point Geometries:**
- `SpherePointGeometry(Device, ReadOnlyMemory<Point>, RTCBuildQuality)` - 3D spheres
  - `UpdatePoints(ReadOnlyMemory<Point>)` / `UpdatePoints(ReadOnlySpan<Point>)` - Update point data
  - `GetPointDataPointer()` - Direct pointer access
- `DiscPointGeometry(Device, ReadOnlyMemory<Point>, RTCBuildQuality)` - Flat discs
  - `UpdatePoints(ReadOnlyMemory<Point>)` / `UpdatePoints(ReadOnlySpan<Point>)` - Update point data
  - `GetPointDataPointer()` - Direct pointer access
- `OrientedDiscPointGeometry(Device, ReadOnlyMemory<OrientedPoint>, RTCBuildQuality)` - Oriented discs
  - `UpdatePoints(ReadOnlyMemory<OrientedPoint>)` / `UpdatePoints(ReadOnlySpan<OrientedPoint>)` - Update point data
  - `GetPointDataPointer()` - Direct pointer access

**Advanced Geometries:**
- `SubdivisionGeometry(Device, vertices, indices, faces, RTCBuildQuality)` - Catmull-Clark subdivision
- `GridGeometry(Device, ReadOnlyMemory<V3f> vertices, width, height, RTCBuildQuality)` - Grid meshes
- `MotionBlurGeometry(Device, verticesT0, verticesT1, indices, RTCBuildQuality)` - Linear motion blur
  - `MotionBlurGeometry(Device, vertexTimeSteps[], indices, RTCBuildQuality)` - Multi-segment motion blur
  - `SetTimeRange(float startTime, float endTime)` - Control visibility over time
- `InstanceGeometry(Device, EmbreeGeometry geometry, Affine3f transform, RTCBuildQuality quality)` - Instanced geometry with transforms
- `UserGeometry(Device, RTCBoundFunction, RTCIntersectFunction, ...)` - Custom geometry

### Buffers
- `EmbreeBuffer<T>.Create(Device, ReadOnlyMemory<T>)` - Geometry data buffer
- `Update(ReadOnlyMemory<T>)` / `Update(ReadOnlySpan<T>)` - Update buffer contents
- `GetDataPointer()` - Direct pointer access for advanced scenarios

### Interpolation
- `InterpolatePosition(uint primID, float u, float v)` - Interpolate vertex position
- `InterpolateNormal(uint primID, float u, float v)` - Interpolate vertex normal
- `InterpolateWithDerivatives(uint primID, float u, float v, out V3f pos, out V3f dPdu, out V3f dPdv)` - With partial derivatives
- `InterpolateBatch(uint[] primIDs, float[] us, float[] vs)` - Batch interpolation

### Collision Detection
- `Scene.Collide(Scene otherScene)` - Find overlapping primitives between two scenes
- Returns list of `CollisionResult` with GeometryId and PrimitiveId pairs

### Data Structures
- `RayHit` - Intersection result (T, Normal, Coord, PrimitiveId, GeometryId, InstanceId)
- `ClosestPointInfo` - Point query result (IsValid, Point, UV, DistanceSquared, GeomID, PrimID)
- `CurveVertex` - Curve control point (Position, Radius)
- `Point` - Point with position and radius
- `OrientedPoint` - Point with position, radius, and normal
- `RTCBuildQuality` - Low, Medium, High, Refit
- `RTCGeometryType` - Triangle, Quad, Curve, Point, Subdivision, Grid, Instance, User, etc.
- `RTCSubdivisionMode` - NoSubdivision, SmoothBoundary, PinCorners, PinBoundary, PinAll

## Platform Support

| Platform | Architecture | Native Libraries |
|----------|--------------|------------------|
| Windows  | x64          | embree4.dll, tbb12.dll |
| Linux    | x64          | libembree4.so.4, libtbb.so.12 |
| macOS    | x64          | libembree4.4.dylib, libtbb.12.dylib |
| macOS    | ARM64 (Apple Silicon) | libembree4.4.dylib, libtbb.12.dylib |

Native libraries included in NuGet package.

## Performance

Uses Embree 4.4.0 with Intel TBB for multi-threaded BVH traversal. Thread count defaults to CPU core count.

## Build & Test

```bash
# Windows
.\build.cmd

# Linux/macOS
./build.sh

# Tests
dotnet test --configuration Release
```

## License

Apache 2.0
