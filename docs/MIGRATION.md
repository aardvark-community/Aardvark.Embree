# Migration Guide: Embree 3.x to Embree 4.x

Upgrade path from Aardvark.Embree 0.3.x (Embree 3.x) to 0.4.0+ (Embree 4.4.0).

## Overview

Breaking changes:
- `ClosestPointInfo.DistanceSquared` changed from `double` to `float`
- `rtcSetGeometryPrimitiveCount` removed
- Enum spelling corrections (Unknow→Unknown, InvaildOperation→InvalidOperation, etc.)
- Instance geometry requires `RTCRayQueryFlags.None` instead of `Incoherent`

New features:
- Motion blur (time-varying geometry)
- SIMD ray packets (Intersect4/8/16)
- Span<T> constructors
- 15 curve types, 3 point types, grid meshes
- Native interpolation with derivatives

---

## Breaking Changes

### 1. ClosestPointInfo Return Type Change

`GetClosestPoint` method return type changed.

#### Embree 3.x

```csharp
ClosestPointInfo GetClosestPoint(V3f point)

// ClosestPointInfo fields:
public struct ClosestPointInfo
{
    public bool IsValid;
    public V3f Point;
    public V2f UV;
    public double DistanceSquared;  // <-- DOUBLE (wrapper cast from float)
    public uint PrimID;
}
```

#### Embree 4.x

```csharp
ClosestPointInfo GetClosestPoint(V3f point, float maxRadius = float.PositiveInfinity)

// ClosestPointInfo fields:
public struct ClosestPointInfo
{
    public bool IsValid;
    public V3f Point;
    public V2f UV;
    public float DistanceSquared;  // <-- FLOAT (actual Embree precision)
    public uint GeomID;            // <-- NEW field
    public uint PrimID;
}
```

Embree computes with float precision internally. Embree 3 wrapper cast to double; Embree 4 exposes actual precision.

**Migration:**

```csharp
// Embree 3.x code (implicit conversion)
var result = scene.GetClosestPoint(queryPoint);
double distanceSquared = result.DistanceSquared;

// Embree 4.x code (explicit cast required)
var result = scene.GetClosestPoint(queryPoint);
double distanceSquared = (double)result.DistanceSquared;
```

No precision loss. Both versions compute with float (~7 decimal digits).

---

### 2. API Removals

#### rtcSetGeometryPrimitiveCount (REMOVED)

Function removed. Primitive count inferred from buffer sizes.

```csharp
// Embree 3.x (REMOVE THIS)
EmbreeAPI.rtcSetGeometryPrimitiveCount(geomHandle, triangleCount);

// Embree 4.x (automatic - no action needed)
// Primitive count inferred from buffer size: vertexCount / indicesPerPrimitive
```


---

### 3. Enum Spelling Corrections

Typos corrected:

#### RTCDeviceError

```csharp
// Embree 3.x (old)
RTCDeviceError.Unknow              // ❌ Typo
RTCDeviceError.InvaildOperation    // ❌ Typo

// Embree 4.x (corrected)
RTCDeviceError.Unknown             // ✅ Fixed
RTCDeviceError.InvalidOperation    // ✅ Fixed
```

#### RTCBufferType

```csharp
// Embree 3.x (old)
RTCBufferType.VertexAttirbute      // ❌ Typo

// Embree 4.x (corrected)
RTCBufferType.VertexAttribute      // ✅ Fixed
```

#### RTCGeometryType

```csharp
// Embree 3.x (old)
RTCGeometryType.NormalOrientedHermiteCuve   // ❌ Typo

// Embree 4.x (corrected)
RTCGeometryType.NormalOrientedHermiteCurve  // ✅ Fixed
```

Find/Replace:
- `Unknow` → `Unknown`
- `InvaildOperation` → `InvalidOperation`
- `VertexAttirbute` → `VertexAttribute`
- `NormalOrientedHermiteCuve` → `NormalOrientedHermiteCurve`

---

### 4. Instance Geometry Flag Changes

Instance geometries require `RTCRayQueryFlags.None` instead of `Incoherent`.

```csharp
// Embree 3.x
scene.Intersect(ray, RTCRayQueryFlags.Incoherent);

// Embree 4.x
scene.Intersect(ray, RTCRayQueryFlags.None);
```

Using `Incoherent` with instances causes intersection failures.

Flag usage:
- `RTCRayQueryFlags.None`: Instance geometries, incoherent rays
- `RTCRayQueryFlags.Coherent`: Primary/shadow rays (performance optimization)

---

### 5. RTCIntersectArguments Structure Changes

Query argument structures changed.

#### Embree 3.x (Deprecated)

```csharp
// Embree 3.x used RTCIntersectContext (now obsolete)
var context = new RTCIntersectContext
{
    flags = RTCIntersectContextFlags.Incoherent,
    // ...
};
```

#### New Embree 4 API

```csharp
// Embree 4.x uses RTCIntersectArguments
var args = new RTCIntersectArguments
{
    flags = RTCRayQueryFlags.None,
    feature_mask = 0xFFFFFFFF,
    context = IntPtr.Zero,
    filter = IntPtr.Zero,
    intersect = IntPtr.Zero
};
```

High-level API (Scene.Intersect/Occluded): No action needed.
Low-level P/Invoke: Replace `RTCIntersectContext` with `RTCIntersectArguments`.

---

## Precision Considerations

Embree uses float precision exclusively. Double precision not supported (SIMD performance constraint).

Precision: ~7 decimal digits (±1e-7 relative error).

### Coordinate Offsetting for Double-Precision Applications

```csharp
// 1. Compute local origin (e.g., bounding box center)
var center = boundingBox.Center;  // V3d

// 2. Convert geometry to local coordinates (V3d → V3f)
var localVertices = new V3f[vertices.Length];
for (int i = 0; i < vertices.Length; i++)
    localVertices[i] = (V3f)(vertices[i] - center);

// 3. Build Embree scene with local coordinates
var geometry = new TriangleGeometry(device, localVertices, indices, RTCBuildQuality.High);
var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
scene.AttachGeometry(geometry);
scene.Commit();

// 4. Transform query rays to local space
var localOrigin = (V3f)(rayOrigin - center);
scene.Intersect(localOrigin, rayDirection, ref hit);

// 5. Transform results back to world space
var worldHitPoint = new V3d(hit.T * rayDirection + localOrigin) + center;
```

Required when geometry spans large coordinates (>1e6 units). Maintains ±1e-7 relative precision in local space.

Advanced techniques for extreme precision needs:
1. Hierarchical offsetting (partition world into chunks)
2. Double-precision post-processing (Embree for coarse query, refine with double)
3. Conservative BVH (enlarge boxes to prevent float ray-box miss)

---

## New Features

### Motion Blur

```csharp
// Define geometry at t=0 and t=1
var verticesT0 = new V3f[] { /* positions at time 0 */ };
var verticesT1 = new V3f[] { /* positions at time 1 */ };
var indices = new int[] { 0, 1, 2 };

// Create motion blur geometry
using var geom = new MotionBlurGeometry(device,
    new ReadOnlyMemory<V3f>(verticesT0),
    new ReadOnlyMemory<V3f>(verticesT1),
    new ReadOnlyMemory<int>(indices),
    RTCBuildQuality.High);

// Ray intersection at t=0.5 (geometry interpolated to midpoint)
scene.Intersect(rayOrigin, rayDirection, ref hit, time: 0.5f);
```

 Motion blur rendering, animated scenes, temporal anti-aliasing.

### Span<T> Zero-Copy API

```csharp
// Stack-allocated vertices (no heap allocation)
Span<V3f> vertices = stackalloc V3f[3]
{
    new V3f(0, 0, 0),
    new V3f(1, 0, 0),
    new V3f(0, 1, 0)
};

Span<int> indices = stackalloc int[3] { 0, 1, 2 };

// Create geometry from spans (zero-copy)
using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
```

 High-frequency geometry updates, real-time procedural generation.

### Ray Packets (SIMD)

```csharp
// Batch 8 rays together
var origins = new V3f[8] { /* 8 ray origins */ };
var directions = new V3f[8] { /* 8 ray directions */ };

// Process all 8 rays in single SIMD call (up to 6x faster)
RayHit[] hits = scene.Intersect8(origins, directions);
```

Performance:
- `Intersect4` (SSE): ~2-3x vs single ray
- `Intersect8` (AVX2): ~4-6x
- `Intersect16` (AVX-512): ~8-12x

### Geometry Types

15 curve types (Bezier, BSpline, Hermite, Catmull-Rom, Linear):
```csharp
var curveVertices = new CurveVertex[]
{
    new CurveVertex(new V3f(0, 0, 0), 0.1f),  // position + radius
    new CurveVertex(new V3f(1, 1, 0), 0.15f)
};
using var curve = new RoundBezierCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);
```

3 point types (Sphere, Disc, OrientedDisc):
```csharp
var points = new Point[]
{
    new Point(new V3f(0, 0, 0), 0.5f)  // position + radius
};
using var spheres = new SpherePointGeometry(device, points, RTCBuildQuality.High);
```

 ### Native Interpolation

```csharp
// Interpolate position at barycentric coordinates (u, v)
var position = geometry.InterpolatePosition(primID, u, v);

// Interpolate with partial derivatives (for normal mapping, etc.)
geometry.InterpolateWithDerivatives(primID, u, v, out V3f pos, out V3f dPdu, out V3f dPdv);
```

 ---

## Migration Checklist

### Step 1: Update Package Reference

```xml
<!-- Update your .csproj -->
<PackageReference Include="Aardvark.Embree" Version="0.4.0" />
```

### Step 2: Fix GetClosestPoint Usage

```csharp
// Search for all GetClosestPoint calls
// If storing DistanceSquared in double, add explicit cast:
var result = scene.GetClosestPoint(queryPoint);
double distanceSquared = (double)result.DistanceSquared;  // Add cast
```

### Step 3: Remove Obsolete API Calls

```csharp
// ❌ Remove all calls to rtcSetGeometryPrimitiveCount
// (Search your codebase for "rtcSetGeometryPrimitiveCount")
```

### Step 4: Fix Enum Spelling

Run Find/Replace across your solution:
- [ ] `RTCDeviceError.Unknow` → `RTCDeviceError.Unknown`
- [ ] `RTCDeviceError.InvaildOperation` → `RTCDeviceError.InvalidOperation`
- [ ] `RTCBufferType.VertexAttirbute` → `RTCBufferType.VertexAttribute`
- [ ] `RTCGeometryType.NormalOrientedHermiteCuve` → `RTCGeometryType.NormalOrientedHermiteCurve`

### Step 5: Update Instance Geometry Flags

```csharp
// ❌ Find and replace in scenes with instance geometries:
scene.Intersect(ray, RTCRayQueryFlags.Incoherent);

// ✅ Replace with:
scene.Intersect(ray, RTCRayQueryFlags.None);
```

**How to identify instance geometry usage**:
- Search for `InstanceGeometry` or `GeometryInstance` in your code
- Search for `RTCGeometryType.Instance`
- Check any scenes with transformed/reused geometry

### Step 6: Test All Ray Intersection Code Paths

Run your existing tests and verify:
- [ ] Ray intersections return correct results
- [ ] Instance geometries hit/miss detection works
- [ ] Occlusion queries produce expected results
- [ ] No regression in render output

### Step 7: Update to Modern APIs (Optional)

Consider modernizing your code to use new Embree 4 features:

```csharp
// Replace arrays with Span<T> for zero-copy performance
- using var geometry = new TriangleGeometry(device, verticesArray, indicesArray, quality);
+ Span<V3f> vertices = /* ... */;
+ using var geometry = new TriangleGeometry(device, vertices, indices, quality);

// Replace GeometryInstance (deprecated) with InstanceGeometry
- using var instance = new GeometryInstance(device, geometry, transform);
+ using var instance = new InstanceGeometry(device, geometry, transform, RTCBuildQuality.High);
```

---

## Troubleshooting

### Problem: "Unable to load DLL 'embree4'"

**Cause**: Native library initialization not called.

**Solution**: Add `Aardvark.Init()` at application startup:

```csharp
using Aardvark.Base;

// Call once before any Embree operations
Aardvark.Init();
```

### Problem: Instance geometry rays miss when they should hit

**Cause**: Using wrong RTCRayQueryFlags.

**Solution**: Change flags to `RTCRayQueryFlags.None`:

```csharp
// ❌ Wrong (Embree 3 style)
scene.Intersect(ray, RTCRayQueryFlags.Incoherent);

// ✅ Correct (Embree 4)
scene.Intersect(ray, RTCRayQueryFlags.None);
```

### Problem: Compiler error "RTCDeviceError.Unknow does not exist"

**Cause**: Enum spelling corrections.

**Solution**: Update enum references:
- `Unknow` → `Unknown`
- `InvaildOperation` → `InvalidOperation`

### Problem: "rtcSetGeometryPrimitiveCount not found"

**Cause**: API removed in Embree 4.

**Solution**: Delete all calls to `rtcSetGeometryPrimitiveCount`. Primitive count is automatic.

---

## Performance Optimization Tips

After migrating, consider these optimizations:

### 1. Use Ray Packets for Batch Queries

```csharp
// Instead of N single-ray queries:
for (int i = 0; i < 8; i++)
    scene.Intersect(origins[i], directions[i], ref hits[i]);

// Use SIMD batch query (4-6x faster):
RayHit[] hits = scene.Intersect8(origins, directions);
```

### 2. Use Span<T> for Geometry Updates

```csharp
// Instead of allocating arrays:
var newVertices = new V3f[count];
// ... populate array ...
geometry.UpdateVertices(new ReadOnlyMemory<V3f>(newVertices));

// Use stack-allocated spans (zero allocation):
Span<V3f> newVertices = stackalloc V3f[count];
// ... populate span ...
geometry.UpdateVertices(newVertices);
```

### 3. Enable Dynamic Scenes for Frequent Updates

```csharp
// For scenes with frequent geometry changes:
using var scene = new Scene(device, RTCBuildQuality.High, dynamic: true);
// ... add geometry ...
scene.Commit();

// Update geometry
geometry.UpdateVertices(newVertices);
scene.Commit();  // Fast refit (not full rebuild)
```

---

## Getting Help

If you encounter issues during migration:

### Documentation
- **README.md**: API reference and examples
- **DEBUGGING_METHODOLOGY.md**: Troubleshooting native interop issues

### Examples
- **src/Aardvark.Embree.Examples/**: Comprehensive examples demonstrating all features
  - Basic ray tracing
  - Instance geometries
  - Motion blur
  - Ray packets
  - Curve and point geometries
  - Subdivision surfaces
  - Performance benchmarks

### Issue Reporting
- GitHub Issues: https://github.com/aardvark-community/Aardvark.Embree/issues
- Include: OS, .NET version, code snippet, error message

### Common Resources
- Embree 4 API Documentation: https://www.embree.org/api.html
- Embree 4 Release Notes: https://github.com/embree/embree/releases/tag/v4.4.0

---

## Summary

**Minimal Migration** (most users):
1. Update package to 0.4.0+
2. Fix enum spelling (Find/Replace)
3. Change instance geometry flags to `RTCRayQueryFlags.None`
4. Test ray intersection code paths

**Time estimate**: 15-30 minutes for typical projects.

**Full Migration** (optional modernization):
1. Adopt Span<T> API for zero-copy performance
2. Use ray packets for batch queries
3. Add motion blur for animated scenes
4. Leverage new geometry types (curves, points)

**Time estimate**: 1-4 hours depending on feature adoption.

Embree 4 provides substantial performance and feature improvements. The migration effort is minimal for the significant gains in rendering speed, memory efficiency, and API capabilities.
