# Diagnostic Tests for Native Interop Debugging

This directory contains low-level diagnostic tests used to isolate and debug complex native interop issues in Aardvark.Embree.

## Directory Structure

### `Native/`
Contains C++ reference implementations that establish known-working baselines using the native Embree API directly.

**Files:**
- `embree_instance_test.cpp` - C++ reference test for instance geometry
- `build_embree_test.bat` - Build script for C++ tests

### `PInvoke/`
Contains direct P/Invoke C# tests that bypass all wrapper classes to isolate interop issues.

**Files:**
- `DirectPInvokeInstanceTest.cs` - Exact C# replica of C++ instance test using direct P/Invoke
- `DirectInstanceTestHeapAlloc.cs` - Variant using heap allocation to rule out stack alignment issues
- `MixedPInvokeTest.cs` - Mixed approach combining direct P/Invoke with wrapper classes

## Purpose

These diagnostic tests implement a **multi-layer debugging methodology** for isolating bugs in native interop code:

1. **C++ Baseline (Native/)** - Establish that native Embree works correctly
2. **Direct P/Invoke (PInvoke/)** - Prove P/Invoke bindings are correct
3. **Mixed Testing (PInvoke/)** - Isolate which wrapper layer contains the bug
4. **Code Comparison** - Compare working and failing code to find the difference

## When to Use These Tests

Use this debugging approach when:
- High-level wrapper tests fail unexpectedly
- Behavior differs from native Embree documentation
- Suspecting memory layout or marshalling issues
- Need to isolate whether bug is in:
  - Native Embree library
  - P/Invoke bindings
  - C# wrapper classes
  - Data marshalling

## Case Study: Instance Geometry Bug (2025-11)

**Problem:** Instance geometry tests failing with no intersections detected.

**Root Cause:** Scene.Intersect() was using `RTCRayQueryFlags.Incoherent` which is incompatible with instance geometries. Required `RTCRayQueryFlags.None`.

**Resolution Path:**
1. C++ test (`Native/embree_instance_test.cpp`) proved native Embree works ✅
2. Direct P/Invoke test (`PInvoke/DirectPInvokeInstanceTest.cs`) proved bindings work ✅
3. Mixed test (`PInvoke/MixedPInvokeTest.cs`) isolated bug to Scene.Intersect() ❌
4. Code comparison revealed incorrect RTCIntersectArguments flags
5. Fixed Scene.Intersect() and Scene.Occluded() flags → all tests pass ✅

**Outcome:** 229/229 tests passing (100%)

**See Also:** `docs/DEBUGGING_METHODOLOGY.md` for detailed step-by-step process.

## Running These Tests

### C++ Tests
```batch
cd src\Aardvark.Embree.Tests\Diagnostics\Native
build_embree_test.bat
embree_instance_test.exe
```

### C# Diagnostic Tests
```bash
dotnet test --filter "FullyQualifiedName~Diagnostics.PInvoke"
```

## Important Notes

- These tests are **intentionally low-level** - they bypass abstraction layers for diagnostic purposes
- They contain **deliberate code duplication** to remain independent from wrapper changes
- They use **unsafe code and raw pointers** - this is necessary for diagnostic work
- Keep these tests **synchronized with wrapper API changes** to maintain diagnostic value
- **Do not refactor** to use wrapper classes - that defeats their purpose

## Future Debugging

When encountering new interop issues:

1. Create a C++ reference test in `Native/` first
2. Replicate in C# using direct P/Invoke in `PInvoke/`
3. Create mixed tests to isolate the failing layer
4. Compare working vs failing code
5. Fix the bug
6. **Keep the diagnostic tests** for future reference
7. Document the bug and resolution in this README

## References

- [DEBUGGING_METHODOLOGY.md](../../../docs/DEBUGGING_METHODOLOGY.md) - Detailed debugging strategy
- [HANDOFF.md](../../../HANDOFF.md) - Phase 2C: Instance geometry investigation
- [Embree API Documentation](https://www.embree.org/api.html)
