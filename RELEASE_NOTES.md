### 0.4.1
- Enhanced error handling with detailed error messages (Embree 4.3.3+ APIs):
  - Device.CheckError() now includes detailed error messages from rtcGetDeviceLastErrorMessage
  - Added Device.GetErrorString() static method for converting error codes to descriptive strings
  - Improved exception messages for OutOfMemory, UnsupportedCPU, and Unknown errors
- Added memory monitoring API:
  - Device.SetMemoryMonitorFunction() for tracking Embree memory allocations and deallocations
  - Useful for profiling, implementing memory limits, and debugging memory issues
- Added custom error callback API:
  - Device.SetErrorFunction() for custom error handling and logging integration
  - Allows applications to intercept Embree errors and implement custom recovery strategies
- Optimized Span<T> zero-copy operations: Added Upload(ReadOnlySpan<T>) overloads in EmbreeBuffer, eliminated unnecessary allocations
- Added InstanceGeometry scene configuration: Exposed RTCSceneFlags parameter for advanced scene control
- Fixed RTCBuildQuality.Refit bug: DiscGeometry, SphereGeometry, and GridGeometry don't support Refit quality
- Completed test parameterization: All 29 test files now parameterized across RTCBuildQuality.Low/Medium/High where orthogonal
- Completed zero-copy ray packet API: All Intersect4/8/16 and Occluded4/8/16 methods now have Span<T> overloads
  - Scene.Intersect4/8/16(ReadOnlySpan<V3f>, ReadOnlySpan<V3f>, Span<RayHit>)
  - Scene.Occluded4/8/16(ReadOnlySpan<V3f>, ReadOnlySpan<V3f>, Span<bool>)
  - All array-returning variants refactored to call span versions internally
- Test suite: 1,102/1,102 passing (+581 from comprehensive parameterization), 9 skipped (collision API pending stability)

### 0.4.0
- [Embree 4 Upgrade] Upgraded from Embree 3.x to Embree 4.4.0
- Added motion blur support (2-step and multi-step time intervals)
- Added Span<T> and ReadOnlySpan<T> API for zero-copy buffer operations
- Added ray packet API (Intersect4/8/16, Occluded4/8/16 for SIMD acceleration)
- Added geometry update APIs (UpdateVertices, UpdateIndices, UpdateNormals)
- Added 15 curve geometry types (Bezier, BSpline, Hermite, Catmull-Rom, Linear × Flat/Round/Normal-oriented)
- Added 3 point geometry types (Sphere, Disc, OrientedDisc)
- Added instance geometry with transformation matrix support
- Added collision detection API (Scene.Collide)
- Added interpolation API for attribute interpolation
- Added comprehensive Examples project with 8 interactive demonstrations
- Added memory stress testing (allocation, GC pressure, concurrency)
- Added sphere/disc edge case tests (tangent rays, interior origins, near-miss scenarios)
- Documented point query limitation with instance geometries (Embree API constraint)
- BREAKING: Enum spelling fixes (Unknow→Unknown, InvaildOperation→InvalidOperation, VertexAttirbute→VertexAttribute, NormalOrientedHermiteCuve→NormalOrientedHermiteCurve)
- BREAKING: Removed rtcSetGeometryPrimitiveCount (no longer exists in Embree 4)
- Instance geometries now require RTCRayQueryFlags.None (not Incoherent)
- Improved error handling with Device.CheckError() after all native calls
- Updated native libraries to Embree 4.4.0 (embree4.dll, tbb12.dll, tbbmalloc.dll)
- Callback error handling now uses Report.Warn instead of Console.Error
- Fixed instance geometry intersection (flag configuration issue)
- Fixed interpolation crash (struct marshalling alignment)
- Fixed motion blur time range handling
- Fixed thread-safe disposal pattern across all wrapper classes
- 491/491 tests passing, 9 skipped (collision API pending stability)
- Multi-layer diagnostic test infrastructure
- C++ baseline tests for native API verification

### 0.3.7
- various cleanups

### 0.3.6
- EmbreeBuffer is now generic and takes ReadOnlyMemory<T>
- TriangleGeometry now takes ReadOnlyMemory instead of Arrays

### 0.3.5
- GetClosestPoint now also returns barycentric coordinates UV

### 0.3.4
- Scene.GetClosestPoint(V3f queryPoint)

### 0.3.3
- updated tools and packages
- fixed CA* messages (VS22)

### 0.3.2
- updated to Aardvark 5.3

### 0.3.2-prerelease0001
- updated to Aardvark 5.3 (prerelease)
 
### 0.3.1
- added native Linux binaries

### 0.3.0
- updated to Aardvark 5.2

### 0.2.0
- updated to Embree 3.12.2
- updated to Aardvark 5.1
- changed Intersect/Occluded input arguments to float

### 0.1.0
- initial