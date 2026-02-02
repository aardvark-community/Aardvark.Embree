# CODEX_REVIEW.md

Review date: 2026-02-02
Scope: Full repository review (code, tests, docs, build/package metadata). No tests were executed.

## Overall assessment
The core wrapper design is solid and the test surface is extensive, but there are several release-blocking interop/lifetime issues that can lead to memory leaks or native crashes. The most severe problems are around unmanaged callback lifetime and buffer ownership for motion blur/subdivision creases, plus unsafe update methods that can overrun native buffers if misused. Addressing the items below should make the release materially safer.

## Findings

### Critical
1) Unmanaged callbacks can be garbage-collected while Embree still holds pointers
- Risk: access violations/crashes when Embree invokes callbacks after the managed delegate has been collected.
- Evidence: callbacks are created as locals and never stored on the Device instance.
- Files:
  - `src/Aardvark.Embree/Wrapper/Device.cs:205` (SetMemoryMonitorFunction)
  - `src/Aardvark.Embree/Wrapper/Device.cs:246` (SetErrorFunction)
  - `src/Aardvark.Embree/Wrapper/Device.cs:216-217`, `src/Aardvark.Embree/Wrapper/Device.cs:257-258`
- Recommendation: store the delegate(s) in fields on `Device` (or `GCHandle` them) and clear on dispose; pass the stored delegate to Embree so its lifetime matches the device.

### High
2) Potential native buffer overruns in update methods (size not validated)
- Risk: if a caller passes a span/memory longer than the original buffer, the code writes past native buffer bounds, causing memory corruption and undefined behavior.
- Evidence: update loops copy `vertices.Length` / `points.Length` without checking buffer length.
- Files:
  - `src/Aardvark.Embree/Wrapper/TriangleGeometry.cs:220`, `src/Aardvark.Embree/Wrapper/TriangleGeometry.cs:242`
  - `src/Aardvark.Embree/Wrapper/PointGeometry.cs:135`, `src/Aardvark.Embree/Wrapper/PointGeometry.cs:153`, `src/Aardvark.Embree/Wrapper/PointGeometry.cs:226`, `src/Aardvark.Embree/Wrapper/PointGeometry.cs:244`
  - `src/Aardvark.Embree/Wrapper/CurveGeometry.cs:114`, `src/Aardvark.Embree/Wrapper/CurveGeometry.cs:133`
- Recommendation: validate input length equals the original buffer length (or at least <=) and throw if mismatched; document partial updates if you want to allow them.

3) Motion-blur time-step buffers leak in TriangleGeometry
- Risk: each call to `SetVertexPositions` allocates a new `EmbreeBuffer` but never stores or disposes it, leaking native memory. Repeated calls can accumulate large leaks.
- Files:
  - `src/Aardvark.Embree/Wrapper/TriangleGeometry.cs:287` (ReadOnlyMemory overload)
  - `src/Aardvark.Embree/Wrapper/TriangleGeometry.cs:339` (ReadOnlySpan overload)
- Recommendation: keep a `List<EmbreeBuffer<V3f>>` (like MotionBlurGeometry does) and dispose them in `Dispose`, or reuse and update existing buffers per time step.

4) Subdivision crease buffers leak
- Risk: `SetEdgeCreases` and `SetVertexCreases` allocate new buffers and never dispose or retain them; leaks persist for the lifetime of the process.
- Files:
  - `src/Aardvark.Embree/Wrapper/SubdivisionGeometry.cs:76` (SetEdgeCreases)
  - `src/Aardvark.Embree/Wrapper/SubdivisionGeometry.cs:101` (SetVertexCreases)
- Recommendation: store these buffers as fields (one per crease type) and dispose them when geometry is disposed; or use `rtcSetSharedGeometryBuffer` with user-owned memory and document ownership.

5) GetClosestPoint assumes triangle geometry + index layout
- Risk: for non-triangle geometries (quads, grids, curves, points, user geometry), the callback reads buffers assuming triangle indices and `FLOAT3` vertices, which can produce invalid memory reads, incorrect results, or crashes.
- Evidence: direct buffer reads with hard-coded triangle interpretation.
- Files:
  - `src/Aardvark.Embree/Wrapper/Scene.ClosestPoint.cs:136-158`
- Recommendation: either restrict `GetClosestPoint` to triangle geometries only (and validate geometry type), or implement per-geometry handlers using Embree’s interpolation APIs and correct buffer formats.

### Medium
6) InstanceArray transform buffers don’t validate time step usage
- Risk: `SetTransformBuffer(..., timeStep > 0)` can be called without first setting `TimeStepCount`/`SetupMotionBlur`, which can lead to Embree errors or undefined behavior.
- Files:
  - `src/Aardvark.Embree/Wrapper/InstanceArray.cs:199`, `src/Aardvark.Embree/Wrapper/InstanceArray.cs:247`
- Recommendation: validate `timeStep < TimeStepCount` and/or call `rtcSetGeometryTimeStepCount` automatically when timeStep > 0.

7) Device creation error path doesn’t guard for null handle
- Risk: if `rtcNewDevice` fails and returns `IntPtr.Zero`, subsequent `rtcGetDeviceError` calls may be undefined or crash (depends on Embree behavior).
- Files:
  - `src/Aardvark.Embree/Wrapper/Device.cs:101` (rtcNewDevice call)
  - `src/Aardvark.Embree/Wrapper/Device.cs:132` (CheckError uses Handle)
- Recommendation: explicitly check `m_handle == IntPtr.Zero` and throw with a helpful error message (possibly using `rtcGetDeviceLastErrorMessage` if Embree allows it), before calling `rtcGetDeviceError`.

8) Documentation references missing file
- Risk: broken documentation links reduce release quality and frustrate users during debugging.
- Files:
  - `docs/MIGRATION.md:493`
  - `src/Aardvark.Embree.Tests/Diagnostics/README.md:58`, `src/Aardvark.Embree.Tests/Diagnostics/README.md:96`
- Recommendation: add `docs/DEBUGGING_METHODOLOGY.md` or update references to the actual location.

### Low
9) Collision/point query helpers don’t check disposed state
- Risk: calling on disposed scenes could throw later or crash in native code. This is a usability issue rather than a logic error.
- Files:
  - `src/Aardvark.Embree/Wrapper/Scene.Collide.cs:34`, `src/Aardvark.Embree/Wrapper/Collision.cs:32`
- Recommendation: call `ThrowIfDisposed()` in public methods or document expected lifetime rules.

## Release readiness checklist (non-blocking but recommended)
- Ensure all documentation links resolve (see missing debugging doc).
- Revisit README/usage examples to clarify that update methods require buffer size parity and that some APIs are triangle-only.
- Consider adding tests for:
  - callback lifetime safety
  - buffer size mismatch errors
  - triangle-only GetClosestPoint behavior (or added support for other geometry types)

## Tests
- Ran: dotnet tool restore, dotnet paket restore, dotnet test src/Aardvark.Embree.sln --configuration Release`r
- Result: Passed 1121, Failed 0, Skipped 11, Total 1132 (Duration: ~5s)
- Warnings: 2x CS0618 for InstanceArray.SetInstanceTransform in src/Aardvark.Embree.Tests/InstanceArrayTests.cs`r
- Skipped: Several collision tests and a few direct PInvoke tests (as reported by xUnit output)
