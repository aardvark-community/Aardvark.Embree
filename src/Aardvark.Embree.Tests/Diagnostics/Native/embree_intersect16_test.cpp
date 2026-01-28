// embree_intersect16_test.cpp
// Minimal test to reproduce the 16-ray scenario: 8 hit, 8 miss
// Tests native Embree 4 API directly without any wrapper layer

#include <embree4/rtcore.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Simple triangle vertices: unit triangle at Z=0
struct Vertex { float x, y, z, a; };
struct Triangle { int v0, v1, v2; };

int main()
{
    printf("=== Embree Intersect16 Native Test ===\n");
    printf("Testing 16 rays: 8 should hit, 8 should miss\n\n");

    // Initialize Embree device
    RTCDevice device = rtcNewDevice(NULL);
    if (!device) {
        printf("ERROR: Failed to create Embree device\n");
        return 1;
    }
    printf("Device created successfully\n");

    // Create scene
    RTCScene scene = rtcNewScene(device);
    if (!scene) {
        printf("ERROR: Failed to create scene\n");
        rtcReleaseDevice(device);
        return 1;
    }
    printf("Scene created successfully\n");

    // Create triangle geometry
    RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);
    if (!geom) {
        printf("ERROR: Failed to create geometry\n");
        rtcReleaseScene(scene);
        rtcReleaseDevice(device);
        return 1;
    }
    printf("Geometry created successfully\n");

    // Set vertex buffer: simple triangle at Z=0
    Vertex* vertices = (Vertex*)rtcSetNewGeometryBuffer(geom,
        RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3,
        sizeof(Vertex), 3);
    vertices[0].x = 0.0f; vertices[0].y = 0.0f; vertices[0].z = 0.0f;
    vertices[1].x = 1.0f; vertices[1].y = 0.0f; vertices[1].z = 0.0f;
    vertices[2].x = 0.0f; vertices[2].y = 1.0f; vertices[2].z = 0.0f;
    printf("Triangle vertices: (0,0,0), (1,0,0), (0,1,0)\n");

    // Set index buffer
    Triangle* triangles = (Triangle*)rtcSetNewGeometryBuffer(geom,
        RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT3,
        sizeof(Triangle), 1);
    triangles[0].v0 = 0;
    triangles[0].v1 = 1;
    triangles[0].v2 = 2;

    // Commit geometry
    rtcCommitGeometry(geom);
    printf("Geometry committed\n");

    // Attach geometry to scene
    rtcAttachGeometry(scene, geom);
    rtcReleaseGeometry(geom);

    // Commit scene
    rtcCommitScene(scene);
    printf("Scene committed\n\n");

    // Prepare 16 rays: 8 hit, 8 miss
    // Layout: org_x, org_y, org_z, tnear, dir_x, dir_y, dir_z, time, tfar, mask, id, flags
    // Hit rays: pointing down at triangle from Z=1
    // Miss rays: pointing up away from triangle

    __declspec(align(64)) float org_x[16], org_y[16], org_z[16];
    __declspec(align(64)) float dir_x[16], dir_y[16], dir_z[16];
    __declspec(align(64)) float tnear[16], tfar[16], time[16];
    __declspec(align(64)) unsigned int mask[16], id[16], flags[16];
    __declspec(align(64)) float Ng_x[16], Ng_y[16], Ng_z[16];
    __declspec(align(64)) float u[16], v[16];
    __declspec(align(64)) unsigned int primID[16], geomID[16], instID[16];

    // Initialize 8 rays that hit (indices 0-7)
    for (int i = 0; i < 8; i++) {
        org_x[i] = 0.25f + i * 0.05f;  // Inside triangle
        org_y[i] = 0.25f;
        org_z[i] = 1.0f;                // Above triangle
        dir_x[i] = 0.0f;
        dir_y[i] = 0.0f;
        dir_z[i] = -1.0f;               // Pointing down
        tnear[i] = 0.0f;
        tfar[i] = 1e30f;
        time[i] = 0.0f;
        mask[i] = 0xFFFFFFFF;
        id[i] = i;
        flags[i] = 0;
        primID[i] = RTC_INVALID_GEOMETRY_ID;
        geomID[i] = RTC_INVALID_GEOMETRY_ID;
        instID[i] = RTC_INVALID_GEOMETRY_ID;
    }

    // Initialize 8 rays that miss (indices 8-15)
    for (int i = 8; i < 16; i++) {
        org_x[i] = 0.25f;
        org_y[i] = 0.25f;
        org_z[i] = 1.0f;
        dir_x[i] = 0.0f;
        dir_y[i] = 0.0f;
        dir_z[i] = 1.0f;                // Pointing up (away from triangle)
        tnear[i] = 0.0f;
        tfar[i] = 1e30f;
        time[i] = 0.0f;
        mask[i] = 0xFFFFFFFF;
        id[i] = i;
        flags[i] = 0;
        primID[i] = RTC_INVALID_GEOMETRY_ID;
        geomID[i] = RTC_INVALID_GEOMETRY_ID;
        instID[i] = RTC_INVALID_GEOMETRY_ID;
    }

    printf("Configured 16 rays:\n");
    printf("  Rays 0-7: Origin (varies, 0.25, 1.0), Direction (0, 0, -1) - SHOULD HIT\n");
    printf("  Rays 8-15: Origin (0.25, 0.25, 1.0), Direction (0, 0, 1) - SHOULD MISS\n\n");

    // Setup RTCRayHit16 structure
    RTCRayHit16 rayhit;
    rayhit.ray.org_x = org_x;
    rayhit.ray.org_y = org_y;
    rayhit.ray.org_z = org_z;
    rayhit.ray.dir_x = dir_x;
    rayhit.ray.dir_y = dir_y;
    rayhit.ray.dir_z = dir_z;
    rayhit.ray.tnear = tnear;
    rayhit.ray.tfar = tfar;
    rayhit.ray.time = time;
    rayhit.ray.mask = mask;
    rayhit.ray.id = id;
    rayhit.ray.flags = flags;
    rayhit.hit.Ng_x = Ng_x;
    rayhit.hit.Ng_y = Ng_y;
    rayhit.hit.Ng_z = Ng_z;
    rayhit.hit.u = u;
    rayhit.hit.v = v;
    rayhit.hit.primID = primID;
    rayhit.hit.geomID = geomID;
    rayhit.hit.instID[0] = instID;

    // Setup intersect arguments
    RTCIntersectArguments args;
    rtcInitIntersectArguments(&args);
    args.flags = RTC_RAY_QUERY_FLAG_INCOHERENT;
    args.feature_mask = RTC_FEATURE_FLAG_ALL;
    args.context = NULL;

    // Valid mask: all 16 rays active
    int valid[16];
    for (int i = 0; i < 16; i++) valid[i] = -1;

    printf("Calling rtcIntersect16...\n");

    // THE CRITICAL CALL - Does this crash?
    rtcIntersect16(valid, scene, &args, &rayhit);

    printf("rtcIntersect16 returned successfully!\n\n");

    // Check results
    printf("Results:\n");
    int hit_count = 0;
    int miss_count = 0;

    for (int i = 0; i < 16; i++) {
        if (geomID[i] != RTC_INVALID_GEOMETRY_ID) {
            printf("  Ray %2d: HIT - geomID=%u, primID=%u, tfar=%.3f\n",
                   i, geomID[i], primID[i], tfar[i]);
            hit_count++;
        } else {
            printf("  Ray %2d: MISS\n", i);
            miss_count++;
        }
    }

    printf("\nSummary: %d hits, %d misses\n", hit_count, miss_count);

    // Verify expected results
    bool success = true;
    if (hit_count != 8) {
        printf("ERROR: Expected 8 hits, got %d\n", hit_count);
        success = false;
    }
    if (miss_count != 8) {
        printf("ERROR: Expected 8 misses, got %d\n", miss_count);
        success = false;
    }

    // Cleanup
    rtcReleaseScene(scene);
    rtcReleaseDevice(device);
    printf("\nCleanup complete\n");

    if (success) {
        printf("\n=== SUCCESS: Native Embree handles 16-ray scenario correctly ===\n");
        return 0;
    } else {
        printf("\n=== FAILURE: Unexpected results ===\n");
        return 1;
    }
}
