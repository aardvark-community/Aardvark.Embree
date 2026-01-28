// Diagnostic test for Embree 4 point queries with instance geometries
// Tests if rtcPointQuery works with instanced geometries
// Compile: cl /EHsc /I..\..\..\..\..\..\include\embree4 embree_point_query_instance_test.cpp /link embree4.lib

#include <embree4/rtcore.h>
#include <iostream>
#include <cmath>

struct ClosestPointResult
{
    float distSq = INFINITY;
    float px, py, pz;
    unsigned primID = RTC_INVALID_GEOMETRY_ID;
    unsigned geomID = RTC_INVALID_GEOMETRY_ID;
};

bool closestPointCallback(RTCPointQueryFunctionArguments* args)
{
    ClosestPointResult* result = (ClosestPointResult*)args->userPtr;

    std::cout << "  Callback invoked: geomID=" << args->geomID
              << " primID=" << args->primID
              << " instStackSize=" << args->context->instStackSize << std::endl;

    // Get geometry
    RTCScene scene = (RTCScene)args->context;
    RTCGeometry geom = rtcGetGeometry(scene, args->geomID);
    if (!geom) return false;

    // Get buffers
    int* indices = (int*)rtcGetGeometryBufferData(geom, RTC_BUFFER_TYPE_INDEX, 0);
    float* vertices = (float*)rtcGetGeometryBufferData(geom, RTC_BUFFER_TYPE_VERTEX, 0);
    if (!indices || !vertices) return false;

    // Get triangle vertices
    int i0 = indices[args->primID * 3 + 0];
    int i1 = indices[args->primID * 3 + 1];
    int i2 = indices[args->primID * 3 + 2];

    float p0x = vertices[i0 * 3 + 0];
    float p0y = vertices[i0 * 3 + 1];
    float p0z = vertices[i0 * 3 + 2];

    float p1x = vertices[i1 * 3 + 0];
    float p1y = vertices[i1 * 3 + 1];
    float p1z = vertices[i1 * 3 + 2];

    float p2x = vertices[i2 * 3 + 0];
    float p2y = vertices[i2 * 3 + 1];
    float p2z = vertices[i2 * 3 + 2];

    std::cout << "  Triangle vertices: (" << p0x << "," << p0y << "," << p0z << ") "
              << "(" << p1x << "," << p1y << "," << p1z << ") "
              << "(" << p2x << "," << p2y << "," << p2z << ")" << std::endl;

    // Simple distance calculation (center of triangle to query point)
    float cx = (p0x + p1x + p2x) / 3.0f;
    float cy = (p0y + p1y + p2y) / 3.0f;
    float cz = (p0z + p1z + p2z) / 3.0f;

    RTCPointQuery* query = (RTCPointQuery*)args->query;
    float dx = query->x - cx;
    float dy = query->y - cy;
    float dz = query->z - cz;
    float distSq = dx*dx + dy*dy + dz*dz;

    if (distSq < result->distSq)
    {
        result->distSq = distSq;
        result->px = cx;
        result->py = cy;
        result->pz = cz;
        result->primID = args->primID;
        result->geomID = args->geomID;

        // Update query radius
        query->radius = sqrtf(distSq);
        return true;
    }

    return false;
}

int main()
{
    std::cout << "=== Embree 4 Point Query Instance Test ===" << std::endl;

    // Create device
    RTCDevice device = rtcNewDevice(NULL);
    if (!device) {
        std::cerr << "ERROR: rtcNewDevice failed" << std::endl;
        return 1;
    }

    // Test 1: Direct geometry (baseline)
    std::cout << "\n[Test 1] Point query on DIRECT geometry:" << std::endl;
    {
        RTCScene scene = rtcNewScene(device);
        RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);

        // Triangle vertices
        float* vertices = (float*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_VERTEX, 0,
                                                          RTC_FORMAT_FLOAT3, 3*sizeof(float), 3);
        vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        unsigned* indices = (unsigned*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_INDEX, 0,
                                                                RTC_FORMAT_UINT3, 3*sizeof(unsigned), 1);
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        rtcCommitGeometry(geom);
        rtcAttachGeometry(scene, geom);
        rtcReleaseGeometry(geom);
        rtcCommitScene(scene);

        // Query point above triangle
        RTCPointQuery query;
        query.x = 0.25f;
        query.y = 0.25f;
        query.z = 1.0f;
        query.time = 0.0f;
        query.radius = INFINITY;

        RTCPointQueryContext context;
        rtcInitPointQueryContext(&context);

        ClosestPointResult result;
        bool found = rtcPointQuery(scene, &query, &context,
                                   (RTCPointQueryFunction)closestPointCallback, &result);

        if (found && result.distSq != INFINITY) {
            std::cout << "SUCCESS: Found closest point" << std::endl;
            std::cout << "  Distance: " << sqrtf(result.distSq) << std::endl;
            std::cout << "  Point: (" << result.px << ", " << result.py << ", " << result.pz << ")" << std::endl;
        } else {
            std::cout << "FAILED: No closest point found" << std::endl;
        }

        rtcReleaseScene(scene);
    }

    // Test 2: Instanced geometry
    std::cout << "\n[Test 2] Point query on INSTANCED geometry:" << std::endl;
    {
        // Create internal scene with geometry
        RTCScene internalScene = rtcNewScene(device);
        RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);

        // Triangle vertices
        float* vertices = (float*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_VERTEX, 0,
                                                          RTC_FORMAT_FLOAT3, 3*sizeof(float), 3);
        vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        unsigned* indices = (unsigned*)rtcSetNewGeometryBuffer(geom, RTC_BUFFER_TYPE_INDEX, 0,
                                                                RTC_FORMAT_UINT3, 3*sizeof(unsigned), 1);
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        rtcCommitGeometry(geom);
        rtcAttachGeometry(internalScene, geom);
        rtcReleaseGeometry(geom);
        rtcCommitScene(internalScene);

        // Create instance in main scene
        RTCScene scene = rtcNewScene(device);
        RTCGeometry instance = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_INSTANCE);
        rtcSetGeometryInstancedScene(instance, internalScene);

        // Set identity transform
        float transform[12] = {
            1.0f, 0.0f, 0.0f, 0.0f,  // row 0
            0.0f, 1.0f, 0.0f, 0.0f,  // row 1
            0.0f, 0.0f, 1.0f, 0.0f   // row 2
        };
        rtcSetGeometryTransform(instance, 0, RTC_FORMAT_FLOAT3X4_ROW_MAJOR, transform);
        rtcSetGeometryTimeStepCount(instance, 1);

        rtcCommitGeometry(instance);
        rtcAttachGeometry(scene, instance);
        rtcReleaseGeometry(instance);
        rtcCommitScene(scene);

        // Query point above triangle
        RTCPointQuery query;
        query.x = 0.25f;
        query.y = 0.25f;
        query.z = 1.0f;
        query.time = 0.0f;
        query.radius = INFINITY;

        RTCPointQueryContext context;
        rtcInitPointQueryContext(&context);

        ClosestPointResult result;
        bool found = rtcPointQuery(scene, &query, &context,
                                   (RTCPointQueryFunction)closestPointCallback, &result);

        if (found && result.distSq != INFINITY) {
            std::cout << "SUCCESS: Found closest point on instance" << std::endl;
            std::cout << "  Distance: " << sqrtf(result.distSq) << std::endl;
            std::cout << "  Point: (" << result.px << ", " << result.py << ", " << result.pz << ")" << std::endl;
        } else {
            std::cout << "FAILED: No closest point found on instance" << std::endl;
            std::cout << "  This indicates rtcPointQuery does NOT traverse instances" << std::endl;
        }

        rtcReleaseScene(internalScene);
        rtcReleaseScene(scene);
    }

    rtcReleaseDevice(device);

    std::cout << "\n=== Test Complete ===" << std::endl;
    return 0;
}
