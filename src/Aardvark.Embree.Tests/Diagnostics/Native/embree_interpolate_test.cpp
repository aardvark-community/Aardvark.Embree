#include <embree4/rtcore.h>
#include <iostream>
#include <cstdlib>
#include <cmath>

// Simple triangle mesh for interpolation testing
void createTriangleGeometry(RTCDevice device, RTCScene scene) {
    RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);

    // Vertices: simple triangle at origin
    float* vertices = (float*)rtcSetNewGeometryBuffer(geom,
        RTC_BUFFER_TYPE_VERTEX, 0,
        RTC_FORMAT_FLOAT3, 3 * sizeof(float), 3);

    vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;  // v0
    vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;  // v1
    vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;  // v2

    // Indices: single triangle
    unsigned* indices = (unsigned*)rtcSetNewGeometryBuffer(geom,
        RTC_BUFFER_TYPE_INDEX, 0,
        RTC_FORMAT_UINT3, 3 * sizeof(unsigned), 1);

    indices[0] = 0;
    indices[1] = 1;
    indices[2] = 2;

    rtcCommitGeometry(geom);
    rtcAttachGeometry(scene, geom);
    rtcReleaseGeometry(geom);
}

int main() {
    std::cout << "=== Embree rtcInterpolate C++ Reference Test ===" << std::endl;

    // Initialize device
    RTCDevice device = rtcNewDevice(NULL);
    if (!device) {
        std::cerr << "ERROR: Failed to create Embree device" << std::endl;
        return 1;
    }

    // Create scene
    RTCScene scene = rtcNewScene(device);
    createTriangleGeometry(device, scene);
    rtcCommitScene(scene);

    // Get geometry back from scene
    RTCGeometry geom = rtcGetGeometry(scene, 0);
    if (!geom) {
        std::cerr << "ERROR: rtcGetGeometry returned NULL" << std::endl;
        return 1;
    }

    std::cout << "Geometry created and attached successfully" << std::endl;
    std::cout << "  Geometry handle: " << geom << std::endl;

    // Test 1: Interpolate at triangle center (u=0.333, v=0.333)
    {
        std::cout << "\n=== Test 1: Interpolate at center (u=0.333, v=0.333) ===" << std::endl;

        RTCInterpolateArguments args;
        rtcInitInterpolateArguments(&args);

        args.geometry = geom;
        args.primID = 0;
        args.u = 0.333f;
        args.v = 0.333f;
        args.bufferType = RTC_BUFFER_TYPE_VERTEX;
        args.bufferSlot = 0;
        args.P = (float*)std::malloc(3 * sizeof(float));
        args.dPdu = nullptr;
        args.dPdv = nullptr;
        args.ddPdudu = nullptr;
        args.ddPdvdv = nullptr;
        args.ddPdudv = nullptr;
        args.valueCount = 3;

        rtcInterpolate(&args);

        RTCError err = rtcGetDeviceError(device);
        if (err != RTC_ERROR_NONE) {
            std::cerr << "ERROR after rtcInterpolate: " << err << std::endl;
            return 1;
        }

        std::cout << "  Result: (" << args.P[0] << ", " << args.P[1] << ", " << args.P[2] << ")" << std::endl;
        std::cout << "  Expected: ~(0.333, 0.333, 0.0)" << std::endl;

        // Verify result
        if (std::abs(args.P[0] - 0.333f) < 0.01f &&
            std::abs(args.P[1] - 0.333f) < 0.01f &&
            std::abs(args.P[2] - 0.0f) < 0.01f) {
            std::cout << "  ✓ PASS" << std::endl;
        } else {
            std::cout << "  ✗ FAIL" << std::endl;
        }

        std::free(args.P);
    }

    // Test 2: Interpolate with derivatives
    {
        std::cout << "\n=== Test 2: Interpolate with derivatives ===" << std::endl;

        RTCInterpolateArguments args;
        rtcInitInterpolateArguments(&args);

        args.geometry = geom;
        args.primID = 0;
        args.u = 0.5f;
        args.v = 0.5f;
        args.bufferType = RTC_BUFFER_TYPE_VERTEX;
        args.bufferSlot = 0;
        args.P = (float*)std::malloc(3 * sizeof(float));
        args.dPdu = (float*)std::malloc(3 * sizeof(float));
        args.dPdv = (float*)std::malloc(3 * sizeof(float));
        args.ddPdudu = nullptr;
        args.ddPdvdv = nullptr;
        args.ddPdudv = nullptr;
        args.valueCount = 3;

        rtcInterpolate(&args);

        RTCError err = rtcGetDeviceError(device);
        if (err != RTC_ERROR_NONE) {
            std::cerr << "ERROR after rtcInterpolate: " << err << std::endl;
            return 1;
        }

        std::cout << "  Position: (" << args.P[0] << ", " << args.P[1] << ", " << args.P[2] << ")" << std::endl;
        std::cout << "  dPdu: (" << args.dPdu[0] << ", " << args.dPdu[1] << ", " << args.dPdu[2] << ")" << std::endl;
        std::cout << "  dPdv: (" << args.dPdv[0] << ", " << args.dPdv[1] << ", " << args.dPdv[2] << ")" << std::endl;
        std::cout << "  ✓ PASS (no crash, derivatives computed)" << std::endl;

        std::free(args.P);
        std::free(args.dPdu);
        std::free(args.dPdv);
    }

    // Cleanup
    rtcReleaseScene(scene);
    rtcReleaseDevice(device);

    std::cout << "\n=== All tests PASSED ===" << std::endl;
    return 0;
}
