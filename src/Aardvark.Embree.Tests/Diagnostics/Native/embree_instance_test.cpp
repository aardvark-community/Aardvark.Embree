// Minimal Embree 4 instance geometry test
// Purpose: Verify instance geometry works in native Embree 4 and identify API call sequence
#include <embree4/rtcore.h>
#include <iostream>
#include <cmath>

void printRayHitInfo(const RTCRayHit& rayhit) {
    std::cout << "Ray origin: (" << rayhit.ray.org_x << ", " << rayhit.ray.org_y << ", " << rayhit.ray.org_z << ")\n";
    std::cout << "Ray direction: (" << rayhit.ray.dir_x << ", " << rayhit.ray.dir_y << ", " << rayhit.ray.dir_z << ")\n";
    std::cout << "Hit geomID: " << rayhit.hit.geomID << " (valid=" << (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID) << ")\n";
    std::cout << "Hit primID: " << rayhit.hit.primID << "\n";
    std::cout << "Hit instID[0]: " << rayhit.hit.instID[0] << "\n";
    std::cout << "Hit tfar: " << rayhit.ray.tfar << "\n";
}

int main() {
    std::cout << "=== Embree 4 Instance Geometry Test ===\n\n";

    // 1. Create device
    std::cout << "Step 1: Creating Embree device...\n";
    RTCDevice device = rtcNewDevice(NULL);
    if (!device) {
        std::cerr << "ERROR: Failed to create Embree device\n";
        return 1;
    }
    std::cout << "  Device created successfully\n\n";

    // 2. Create source scene with triangle
    std::cout << "Step 2: Creating source scene with triangle...\n";
    RTCScene sourceScene = rtcNewScene(device);
    RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);

    // Set triangle vertices (triangle at origin in XY plane)
    float* vertices = (float*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3, sizeof(float)*3, 3);
    vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;  // v0
    vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;  // v1
    vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;  // v2
    std::cout << "  Triangle vertices set: (0,0,0), (1,0,0), (0,1,0)\n";

    // Set triangle indices
    unsigned* indices = (unsigned*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT3, sizeof(unsigned)*3, 1);
    indices[0] = 0; indices[1] = 1; indices[2] = 2;
    std::cout << "  Triangle indices set: 0, 1, 2\n";

    rtcCommitGeometry(geom);
    unsigned int geomID = rtcAttachGeometry(sourceScene, geom);
    std::cout << "  Geometry committed and attached to source scene (geomID=" << geomID << ")\n";
    rtcCommitScene(sourceScene);
    std::cout << "  Source scene committed\n\n";

    // 3. Create instance geometry
    std::cout << "Step 3: Creating instance geometry...\n";
    RTCGeometry instance = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_INSTANCE);
    std::cout << "  Instance geometry created\n";

    rtcSetGeometryInstancedScene(instance, sourceScene);
    std::cout << "  Instance linked to source scene via rtcSetGeometryInstancedScene\n";

    rtcSetGeometryTimeStepCount(instance, 1);
    std::cout << "  Time step count set to 1\n";

    // Set identity transform (row-major 3x4 matrix)
    float transform[12] = {
        1.0f, 0.0f, 0.0f, 0.0f,  // row 0: [1 0 0 | 0]
        0.0f, 1.0f, 0.0f, 0.0f,  // row 1: [0 1 0 | 0]
        0.0f, 0.0f, 1.0f, 0.0f   // row 2: [0 0 1 | 0]
    };
    rtcSetGeometryTransform(instance, 0, RTC_FORMAT_FLOAT3X4_ROW_MAJOR, transform);
    std::cout << "  Identity transform set (RTC_FORMAT_FLOAT3X4_ROW_MAJOR)\n";

    rtcCommitGeometry(instance);
    std::cout << "  Instance geometry committed\n\n";

    // 4. Create top-level scene with instance
    std::cout << "Step 4: Creating top-level scene with instance...\n";
    RTCScene topScene = rtcNewScene(device);
    unsigned int instID = rtcAttachGeometry(topScene, instance);
    std::cout << "  Instance attached to top-level scene (instID=" << instID << ")\n";
    rtcCommitScene(topScene);
    std::cout << "  Top-level scene committed\n\n";

    // 5. Test ray intersection
    std::cout << "Step 5: Testing ray intersection...\n";
    RTCRayHit rayhit;
    rayhit.ray.org_x = 0.25f; rayhit.ray.org_y = 0.25f; rayhit.ray.org_z = 1.0f;
    rayhit.ray.dir_x = 0.0f; rayhit.ray.dir_y = 0.0f; rayhit.ray.dir_z = -1.0f;
    rayhit.ray.tnear = 0.0f;
    rayhit.ray.tfar = INFINITY;
    rayhit.ray.time = 0.0f;
    rayhit.ray.mask = 0xFFFFFFFF;
    rayhit.ray.id = 0;
    rayhit.ray.flags = 0;
    rayhit.hit.geomID = RTC_INVALID_GEOMETRY_ID;
    rayhit.hit.primID = RTC_INVALID_GEOMETRY_ID;
    rayhit.hit.instID[0] = RTC_INVALID_GEOMETRY_ID;

    std::cout << "  Ray: origin=(0.25, 0.25, 1.0), direction=(0, 0, -1)\n";

    RTCIntersectArguments args;
    rtcInitIntersectArguments(&args);

    rtcIntersect1(topScene, &rayhit, &args);
    std::cout << "  rtcIntersect1 called\n\n";

    // 6. Check result
    std::cout << "Step 6: Checking results...\n";
    printRayHitInfo(rayhit);

    std::cout << "\n=== RESULT ===\n";
    if (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID) {
        std::cout << "SUCCESS: Ray intersected instance geometry!\n";
        std::cout << "  Hit distance: " << rayhit.ray.tfar << "\n";
        std::cout << "  Expected: ~1.0 (ray travels from z=1.0 to z=0.0)\n";
    } else {
        std::cout << "FAILURE: Ray did not intersect instance geometry\n";
        std::cout << "  This indicates instance geometry is not working properly\n";
    }

    // Cleanup
    std::cout << "\nCleaning up...\n";
    rtcReleaseScene(topScene);
    rtcReleaseScene(sourceScene);
    rtcReleaseGeometry(instance);
    rtcReleaseGeometry(geom);
    rtcReleaseDevice(device);
    std::cout << "Done.\n";

    return (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID) ? 0 : 1;
}
