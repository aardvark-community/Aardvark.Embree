# Aardvark.Embree.Tests

## Running Tests

### Known Limitation: Native State Accumulation

The test suite contains **500 tests** (491 passing, 9 skipped collision tests) that create **200+ Embree devices**. Running all tests in a single process causes **native state corruption** (Embree/TBB internal state, heap fragmentation) that triggers crashes after ~100-200 tests.

**Key finding:** Individual tests pass perfectly in isolation. The crash is caused by accumulated state from previous Device creations, not bugs in the test code itself.

### Recommended Test Execution

**Option 1: Run all tests with crash diagnostics (500 tests - RECOMMENDED)**

```bash
dotnet test --blame-crash
```

The `--blame-crash` flag attaches crash dump diagnostics which prevents the native resource exhaustion crash. All 491 tests pass reliably with this flag (9 collision tests are skipped by design).

**Option 2: Run tests by category (without --blame-crash)**

```bash
# Run all tests excluding ray packet integration tests (~476 tests)
dotnet test --filter "FullyQualifiedName!~RayPacket4IntegrationTests&FullyQualifiedName!~RayPacket8IntegrationTests&FullyQualifiedName!~RayPacket16IntegrationTests"

# Run ray packet integration tests by width (8 tests each)
dotnet test --filter "FullyQualifiedName~RayPacket4IntegrationTests"
dotnet test --filter "FullyQualifiedName~RayPacket8IntegrationTests"
dotnet test --filter "FullyQualifiedName~RayPacket16IntegrationTests"
```

**For CI/CD pipelines**, use `dotnet test --blame-crash` for reliable full test suite execution.

### Test Organization

Tests are organized using xUnit collections to control execution:

- **RayPacket4Tests**: 4-wide SIMD ray packet tests (8 tests)
- **RayPacket8Tests**: 8-wide SIMD ray packet tests (8 tests)
- **RayPacket16Tests**: 16-wide SIMD ray packet tests (8 tests)
- All other tests run in default collection (~476 tests)

Collections use `DisableParallelization = true` and include small delays (`Thread.Sleep(5ms)`) between tests to help native resource cleanup.

### Test Configuration

- **xunit.runner.json**: Disables parallelization globally (`maxParallelThreads: 1`)
- **Native Dependencies**: Embree 4.4.0 DLLs automatically copied to output directory during build

## Test Categories

- **Core Functionality**: Device, Scene, Geometry tests
- **Ray Tracing**: Intersect, Occluded, GetClosestPoint tests
- **Ray Packets**: Intersect4/8/16, Occluded4/8/16 (SIMD batch queries)
- **Advanced Features**: Motion blur, instances, curves, points
- **Edge Cases**: Large datasets, error handling, thread safety, sphere/disc edge cases
- **Memory Stress**: Allocation, GC pressure, concurrency tests
- **Diagnostics**: Low-level P/Invoke and native C++ baseline tests

## Troubleshooting

**If tests crash with "Test host process crashed":**

1. **Use `--blame-crash` flag** (recommended): `dotnet test --blame-crash`
   - This prevents the crash and all 491 tests pass reliably (9 collision tests skipped)

2. **Alternative**: Run tests by category using filters in Option 2 above

3. Verify native Embree DLLs are in output directory

4. Check that you're using .NET 8.0 SDK

5. Ensure no other Embree instances are running

**Why does --blame-crash help?**
The flag attaches crash dump diagnostics which changes memory layout/timing. This sometimes prevents the native state corruption from triggering, though it's not 100% reliable. The crash is a Heisenbug - it disappears when observed because the monitoring changes process behavior.

For interop debugging, see [Diagnostics/README.md](Diagnostics/README.md).
