// Minimal Embree 4 displacement mapping test
// Purpose: Verify displacement callback works with native Embree 4 subdivision surfaces
#include <embree4/rtcore.h>
#include <iostream>
#include <cmath>

// Displacement function that pushes surface up by 0.5 units
void displacementFunc(const RTCDisplacementFunctionNArguments* args) {
    // For each evaluation point
    for (unsigned int i = 0; i < args->N; i++) {
        // Displace along geometric normal by 0.5 units
        args->P_x[i] += args->Ng_x[i] * 0.5f;
        args->P_y[i] += args->Ng_y[i] * 0.5f;
        args->P_z[i] += args->Ng_z[i] * 0.5f;
    }
}

void printRayHitInfo(const RTCRayHit& rayhit, const char* label) {
    std::cout << label << ":\n";
    std::cout << "  Hit: " << (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID ? "YES" : "NO") << "\n";
    if (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID) {
        std::cout << "  Hit point: (" << rayhit.ray.org_x + rayhit.ray.dir_x * rayhit.ray.tfar
                  << ", " << rayhit.ray.org_y + rayhit.ray.dir_y * rayhit.ray.tfar
                  << ", " << rayhit.ray.org_z + rayhit.ray.dir_z * rayhit.ray.tfar << ")\n";
        std::cout << "  tfar: " << rayhit.ray.tfar << "\n";
        std::cout << "  primID: " << rayhit.hit.primID << "\n";
    }
}

int main() {
    std::cout << "=== Embree 4 Displacement Mapping Test ===\n\n";

    // 1. Create device
    std::cout << "Step 1: Creating Embree device...\n";
    RTCDevice device = rtcNewDevice(NULL);
    if (!device) {
        std::cerr << "ERROR: Failed to create Embree device\n";
        return 1;
    }
    std::cout << "  Device created successfully\n\n";

    // 2. Create subdivision surface
    std::cout << "Step 2: Creating subdivision surface (simple quad at z=0)...\n";
    RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_SUBDIVISION);

    // Simple quad: 4 vertices, 4 indices, 1 face (quad)
    float* vertices = (float*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3, sizeof(float)*3, 4);
    vertices[0] = -1.0f; vertices[1] = -1.0f; vertices[2] = 0.0f;  // v0
    vertices[3] =  1.0f; vertices[4] = -1.0f; vertices[5] = 0.0f;  // v1
    vertices[6] =  1.0f; vertices[7] =  1.0f; vertices[8] = 0.0f;  // v2
    vertices[9] = -1.0f; vertices[10] = 1.0f; vertices[11] = 0.0f; // v3
    std::cout << "  Vertices set: 4 vertices forming quad at z=0\n";

    unsigned* indices = (unsigned*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT, sizeof(unsigned), 4);
    indices[0] = 0; indices[1] = 1; indices[2] = 2; indices[3] = 3;
    std::cout << "  Indices set: 0, 1, 2, 3\n";

    unsigned* faces = (unsigned*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_FACE, 0, RTC_FORMAT_UINT, sizeof(unsigned), 1);
    faces[0] = 4; // One quad face (4 vertices)
    std::cout << "  Face set: 1 quad (4 vertices)\n";

    rtcSetGeometrySubdivisionMode(geom, 0, RTC_SUBDIVISION_MODE_SMOOTH_BOUNDARY);
    rtcSetGeometryTessellationRate(geom, 4.0f);
    std::cout << "  Subdivision mode: SMOOTH_BOUNDARY, tessellation rate: 4.0\n";

    // 3. Set displacement function
    std::cout << "\nStep 3: Setting displacement function...\n";
    rtcSetGeometryDisplacementFunction(geom, (RTCDisplacementFunctionN)displacementFunc);
    std::cout << "  Displacement function set (displaces by +0.5 along normal)\n";

    rtcCommitGeometry(geom);
    std::cout << "  Geometry committed\n\n";

    // 4. Create scene and attach geometry
    std::cout << "Step 4: Creating scene...\n";
    RTCScene scene = rtcNewScene(device);
    unsigned int geomID = rtcAttachGeometry(scene, geom);
    std::cout << "  Geometry attached to scene (geomID=" << geomID << ")\n";
    rtcCommitScene(scene);
    std::cout << "  Scene committed\n\n";

    // 5. Test ray intersection (ray shooting down from z=1.0)
    std::cout << "Step 5: Testing ray intersection...\n";
    std::cout << "  Expected: Ray should hit displaced surface at z > 0 (not at z=0)\n\n";

    RTCRayHit rayhit;
    rayhit.ray.org_x = 0.0f; rayhit.ray.org_y = 0.0f; rayhit.ray.org_z = 1.0f;
    rayhit.ray.dir_x = 0.0f; rayhit.ray.dir_y = 0.0f; rayhit.ray.dir_z = -1.0f;
    rayhit.ray.tnear = 0.0f;
    rayhit.ray.tfar = INFINITY;
    rayhit.ray.time = 0.0f;
    rayhit.ray.mask = 0xFFFFFFFF;
    rayhit.ray.id = 0;
    rayhit.ray.flags = 0;
    rayhit.hit.geomID = RTC_INVALID_GEOMETRY_ID;
    rayhit.hit.primID = RTC_INVALID_GEOMETRY_ID;

    std::cout << "  Ray: origin=(0, 0, 1), direction=(0, 0, -1)\n";

    RTCIntersectArguments args;
    rtcInitIntersectArguments(&args);
    args.flags = RTC_RAY_QUERY_FLAG_INCOHERENT;

    rtcIntersect1(scene, &rayhit, &args);
    printRayHitInfo(rayhit, "  Result");

    // 6. Verify displacement worked
    std::cout << "\nStep 6: Verification...\n";
    bool success = false;
    if (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID) {
        float hitZ = rayhit.ray.org_z + rayhit.ray.dir_z * rayhit.ray.tfar;
        std::cout << "  Hit Z coordinate: " << hitZ << "\n";

        // Displacement should push surface up from z=0, so hit should be at z > 0
        // With displacement of +0.5 along normal (0,0,1), we expect hit near z=0.5
        if (hitZ > 0.1f && hitZ < 0.9f) {
            std::cout << "  SUCCESS: Hit point is displaced (z > 0), displacement function worked!\n";
            success = true;
        } else {
            std::cout << "  WARNING: Hit point not at expected displaced position\n";
        }
    } else {
        std::cout << "  FAILED: No intersection found\n";
    }

    // Cleanup
    rtcReleaseGeometry(geom);
    rtcReleaseScene(scene);
    rtcReleaseDevice(device);

    std::cout << "\n=== Test " << (success ? "PASSED" : "FAILED") << " ===\n";
    return success ? 0 : 1;
}
