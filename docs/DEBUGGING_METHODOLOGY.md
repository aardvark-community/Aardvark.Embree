# Debugging Methodology

This document provides a minimal, repeatable workflow for diagnosing native interop issues in this repository.
Use it alongside the diagnostics tests under `src/Aardvark.Embree.Tests/Diagnostics`.

## 1) Reproduce with the smallest test
- Start with the closest existing diagnostic test in `src/Aardvark.Embree.Tests/Diagnostics`.
- If needed, clone a test and reduce it until it reproduces reliably.

## 2) Verify native library loading
- Ensure the correct platform native binaries are being used (Windows/Linux/macOS).
- Check that the Embree and TBB binaries in `libs/Native/Aardvark.Embree/...` match your platform.

## 3) Validate struct layouts
- Run the struct layout tests (e.g. `StructSize*`, `Alignment*`) to confirm packing/size.
- Compare against Embree headers in `include/embree4`.

## 4) Validate buffers and formats
- Confirm buffer type, format, stride, and item counts match Embree expectations.
- Use the diagnostics P/Invoke tests as a reference for known-good formats.

## 5) Isolate callback issues
- Keep delegates alive for the full native lifetime (store them on the owning object).
- Avoid lambdas that capture short-lived locals for callbacks used by Embree.

## 6) Add a regression test
- Add a focused test in `src/Aardvark.Embree.Tests` that fails without the fix.
- Keep it deterministic and fast; avoid multi-threaded flakiness where possible.

## References
- Diagnostics README: `src/Aardvark.Embree.Tests/Diagnostics/README.md`
- Embree headers: `include/embree4`
