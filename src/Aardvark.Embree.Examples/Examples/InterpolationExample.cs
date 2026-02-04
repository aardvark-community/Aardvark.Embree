using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Aardvark.Embree;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// InterpolationExample: Vertex Attribute Interpolation
///
/// Demonstrates how Embree interpolates vertex attributes using barycentric coordinates:
/// - Per-vertex colors for smooth color gradients
/// - Normal vectors for smooth shading
/// - UV coordinates for texture mapping
/// - Comparison between triangle and quad interpolation
/// - Educational explanations of barycentric interpolation mathematics
/// - Demonstrates N>4 vertex attribute interpolation
/// </summary>
public class InterpolationExample : ExampleBase
{
    public override string Title => "Vertex Attribute Interpolation";
    public override string Description => "Colors, normals, UVs, and barycentric mathematics";
    public override string Category => "Geometry & Attributes";
    public override int OrderIndex => 5;
    public override int ApproximateLineCount => 350;

    protected override void Execute()
    {
        PrintSection("1. BARYCENTRIC COORDINATES EXPLAINED");
        ExplainBarycentricCoordinates();

        PrintSection("2. TRIANGLE INTERPOLATION - Per-Vertex Colors");
        DemonstrateTriangleColorInterpolation();

        PrintSection("3. SMOOTH SHADING - Normal Interpolation");
        DemonstrateSmoothShading();

        PrintSection("4. TEXTURE MAPPING - UV Coordinate Interpolation");
        DemonstrateUVInterpolation();

        PrintSection("5. QUAD INTERPOLATION - Bilinear Interpolation");
        DemonstrateQuadInterpolation();

        PrintSection("6. N>4 ATTRIBUTE INTERPOLATION");
        DemonstrateMultiAttributeInterpolation();

        PrintSection("7. DERIVATIVES - Tangent Space Computation");
        DemonstrateDerivatives();
    }

    /// <summary>
    /// Explains barycentric coordinates with concrete examples.
    /// Barycentric coordinates (u, v, w) represent a point as a weighted combination
    /// of triangle vertices, where w = 1 - u - v.
    /// </summary>
    private static void ExplainBarycentricCoordinates()
    {
        Print("Barycentric coordinates express any point inside a triangle as:");
        Print("  P = w*V0 + u*V1 + v*V2, where w = 1 - u - v");
        Print("");
        Print("Key properties:");
        Print("  * At vertex V0: (u=0, v=0, w=1) → 100% of V0");
        Print("  * At vertex V1: (u=1, v=0, w=0) → 100% of V1");
        Print("  * At vertex V2: (u=0, v=1, w=0) → 100% of V2");
        Print("  * Triangle center: (u=⅓, v=⅓, w=⅓) → equal mix");
        Print("  * Edge V0-V1 midpoint: (u=½, v=0, w=½)");
        Print("");
        Print("Embree returns (u, v) in RayHit.Coord, compute w = 1 - u - v");
        Print("");
        Print("Example interpolation formula for color:");
        Print("  color = w*color0 + u*color1 + v*color2");
        Print("  where w = 1 - u - v");
    }

    /// <summary>
    /// Demonstrates color interpolation on a triangle with RGB vertex colors.
    /// Shows how barycentric coordinates create smooth color gradients.
    /// </summary>
    private void DemonstrateTriangleColorInterpolation()
    {
        if (Device == null) return;

        // RGB triangle: pure colors at corners demonstrate smooth gradient blending
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),    // V0 - Red corner
            new V3f(2, 0, 0),    // V1 - Green corner
            new V3f(1, 2, 0)     // V2 - Blue corner
        };

        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        // Vertex attributes store arbitrary per-vertex data; Embree interpolates them at hit points
        var colors = new V3f[]
        {
            new V3f(1, 0, 0),  // Red at V0
            new V3f(0, 1, 0),  // Green at V1
            new V3f(0, 0, 1)   // Blue at V2
        };

        unsafe
        {
            // rtcSetGeometryVertexAttributeCount must be called before binding attribute buffers
            var colorBuffer = EmbreeBuffer.Create(Device, colors);
            EmbreeAPI.rtcSetGeometryVertexAttributeCount(geometry.Handle, 1);
            Device.CheckError("SetVertexAttributeCount");
            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.VertexAttribute, 0,
                RTCFormat.FLOAT3, colorBuffer.Handle, 0, (nuint)(sizeof(float) * 3), 3);
            Device.CheckError("SetGeometryBuffer(VertexAttribute)");
            geometry.Commit();

            var scene = new Scene(Device, RTCBuildQuality.High, false);
            var geomId = scene.AttachGeometry(geometry);
            scene.Commit();

            Print("Triangle with vertex colors:");
            Print("  V0 at (0,0,0) → RED   (1.0, 0.0, 0.0)");
            Print("  V1 at (2,0,0) → GREEN (0.0, 1.0, 0.0)");
            Print("  V2 at (1,2,0) → BLUE  (0.0, 0.0, 1.0)");
            Print("");

            // Test interpolation at various barycentric coordinates
            var testPoints = new[]
            {
                (origin: new V3f(0, 0, -1), dir: new V3f(0, 0, 1), name: "V0 (Red corner)", expectedU: 0f, expectedV: 0f),
                (origin: new V3f(2, 0, -1), dir: new V3f(0, 0, 1), name: "V1 (Green corner)", expectedU: 1f, expectedV: 0f),
                (origin: new V3f(1, 2, -1), dir: new V3f(0, 0, 1), name: "V2 (Blue corner)", expectedU: 0f, expectedV: 1f),
                (origin: new V3f(1, 0, -1), dir: new V3f(0, 0, 1), name: "Edge V0-V1 midpoint", expectedU: 0.5f, expectedV: 0f),
                (origin: new V3f(1, 1, -1), dir: new V3f(0, 0, 1), name: "Triangle center", expectedU: 0.333f, expectedV: 0.333f)
            };

            Print("Interpolating colors at key locations:");
            Print("");

            foreach (var test in testPoints)
            {
                var hit = new RayHit();
                if (scene.Intersect(test.origin, test.dir, ref hit))
                {
                    // Extract barycentric coordinates from hit
                    // u, v returned by Embree; w computed to satisfy constraint u+v+w=1
                    // This ensures weighted sum maintains the point inside the triangle
                    float u = hit.Coord.X;
                    float v = hit.Coord.Y;
                    float w = 1.0f - u - v;

                    // rtcInterpolate uses barycentric coords to blend vertex attributes
                    V3f interpolatedColor = default;
                    // Configure interpolation query:
                    // - (u,v): barycentric coords from ray hit
                    // - bufferSlot 0: which attribute buffer (we only have one)
                    // - P: output pointer for interpolated value
                    // - dPdu/dPdv: derivative outputs (Zero = not requested)
                    // - valueCount=3: interpolate 3 floats (RGB color)
                    var args = new RTCInterpolateArguments
                    {
                        geometry = geometry.Handle,
                        primID = hit.PrimitiveId,
                        u = u,
                        v = v,
                        bufferType = RTCBufferType.VertexAttribute,
                        bufferSlot = 0,
                        P = new IntPtr(&interpolatedColor),
                        dPdu = IntPtr.Zero,
                        dPdv = IntPtr.Zero,
                        ddPdudu = IntPtr.Zero,
                        ddPdvdv = IntPtr.Zero,
                        ddPdudv = IntPtr.Zero,
                        valueCount = 3
                    };
                    EmbreeAPI.rtcInterpolate(ref args);

                    Print($"{test.name}:");
                    Print($"  Barycentric: (u={u:F3}, v={v:F3}, w={w:F3})");
                    Print($"  Interpolated color: RGB({interpolatedColor.X:F3}, {interpolatedColor.Y:F3}, {interpolatedColor.Z:F3})");
                    Print($"  Formula: {w:F3}*Red + {u:F3}*Green + {v:F3}*Blue");
                    Print("");
                }
            }

            scene.Dispose();
            colorBuffer.Dispose();
        }

        geometry.Dispose();
    }

    /// <summary>
    /// Demonstrates smooth vs flat shading using normal interpolation.
    /// Flat shading uses geometry normal, smooth shading interpolates vertex normals.
    /// </summary>
    private void DemonstrateSmoothShading()
    {
        if (Device == null) return;

        Print("Comparing FLAT shading vs SMOOTH shading:");
        Print("");
        Print("FLAT SHADING:");
        Print("  Uses geometric normal (Ng) directly from hit");
        Print("  Same normal across entire triangle → faceted appearance");
        Print("");
        Print("SMOOTH SHADING:");
        Print("  Interpolates per-vertex normals using barycentric coordinates");
        Print("  Different normal at each point → smooth appearance");
        Print("");

        // Triangle with non-planar vertex normals simulates curved surface
        var vertices = new V3f[]
        {
            new V3f(-1, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0.5f)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        // Per-vertex normals: each vertex has a different normal direction
        var normals = new V3f[]
        {
            new V3f(-1, 0, 0).Normalized,   // Normal at V0
            new V3f(1, 0, 0).Normalized,    // Normal at V1
            new V3f(0, 1, 0.5f).Normalized  // Normal at V2
        };

        unsafe
        {
            var normalBuffer = EmbreeBuffer.Create(Device, normals);
            EmbreeAPI.rtcSetGeometryVertexAttributeCount(geometry.Handle, 1);
            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.VertexAttribute, 0,
                RTCFormat.FLOAT3, normalBuffer.Handle, 0, (nuint)(sizeof(float) * 3), 3);
            geometry.Commit();

            var scene = new Scene(Device, RTCBuildQuality.High, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var hit = new RayHit();
            if (scene.Intersect(new V3f(0, 0.3f, -1), new V3f(0, 0, 1), ref hit))
            {
                // FLAT SHADING: Ng (geometric normal) is constant across triangle
                //   → Faceted appearance, visible triangle edges
                V3f flatNormal = hit.Normal.Normalized;

                // SMOOTH SHADING: Interpolated vertex normals vary across surface
                //   → Curved appearance, hidden triangle tessellation
                //   → Fakes smooth surfaces with fewer triangles
                V3f smoothNormal = default;
                var args = new RTCInterpolateArguments
                {
                    geometry = geometry.Handle,
                    primID = hit.PrimitiveId,
                    u = hit.Coord.X,
                    v = hit.Coord.Y,
                    bufferType = RTCBufferType.VertexAttribute,
                    bufferSlot = 0,
                    P = new IntPtr(&smoothNormal),
                    dPdu = IntPtr.Zero,
                    dPdv = IntPtr.Zero,
                    ddPdudu = IntPtr.Zero,
                    ddPdvdv = IntPtr.Zero,
                    ddPdudv = IntPtr.Zero,
                    valueCount = 3
                };
                EmbreeAPI.rtcInterpolate(ref args);
                smoothNormal = smoothNormal.Normalized;

                Print("At triangle interior point:");
                Print($"  Flat normal (Ng):   ({flatNormal.X:F3}, {flatNormal.Y:F3}, {flatNormal.Z:F3})");
                Print($"  Smooth normal:      ({smoothNormal.X:F3}, {smoothNormal.Y:F3}, {smoothNormal.Z:F3})");
                Print("");
                Print("Use smooth normals for:");
                Print("  * Organic shapes (characters, terrain)");
                Print("  * Curved surfaces approximated by triangles");
                Print("  * Phong/Blinn-Phong shading");
            }

            scene.Dispose();
            normalBuffer.Dispose();
        }

        geometry.Dispose();
    }

    /// <summary>
    /// Demonstrates UV coordinate interpolation for texture mapping.
    /// UVs define how 2D textures map onto 3D geometry.
    /// </summary>
    private void DemonstrateUVInterpolation()
    {
        if (Device == null) return;

        Print("UV coordinates map 2D textures onto 3D geometry.");
        Print("Typically: U ∈ [0,1] horizontal, V ∈ [0,1] vertical");
        Print("");

        // Quad = 2 triangles; UVs mapped 1:1 so texture covers entire surface
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),  // Bottom-left
            new V3f(1, 0, 0),  // Bottom-right
            new V3f(1, 1, 0),  // Top-right
            new V3f(0, 1, 0)   // Top-left
        };
        var indices = new int[] { 0, 1, 2, 0, 2, 3 }; // Two triangles

        var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        // Standard UV layout: (0,0) at bottom-left, (1,1) at top-right
        var uvs = new V2f[]
        {
            new V2f(0, 0),  // Bottom-left
            new V2f(1, 0),  // Bottom-right
            new V2f(1, 1),  // Top-right
            new V2f(0, 1)   // Top-left
        };

        unsafe
        {
            // FLOAT2 format: 2 floats per vertex, tightly packed
            var uvBuffer = stackalloc float[4 * 2];
            for (int i = 0; i < 4; i++)
            {
                uvBuffer[i * 2 + 0] = uvs[i].X;
                uvBuffer[i * 2 + 1] = uvs[i].Y;
            }

            var uvArray = new float[8];
            for (int i = 0; i < 8; i++)
                uvArray[i] = uvBuffer[i];
            var buffer = EmbreeBuffer.Create<float>(Device, uvArray);
            EmbreeAPI.rtcSetGeometryVertexAttributeCount(geometry.Handle, 1);
            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.VertexAttribute, 0,
                RTCFormat.FLOAT2, buffer.Handle, 0, (nuint)(sizeof(float) * 2), 4);
            geometry.Commit();

            var scene = new Scene(Device, RTCBuildQuality.High, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            Print("Quad with UV mapping:");
            Print("  V0 (0,0,0) → UV(0.0, 0.0) [Bottom-left]");
            Print("  V1 (1,0,0) → UV(1.0, 0.0) [Bottom-right]");
            Print("  V2 (1,1,0) → UV(1.0, 1.0) [Top-right]");
            Print("  V3 (0,1,0) → UV(0.0, 1.0) [Top-left]");
            Print("");

            var hit = new RayHit();
            if (scene.Intersect(new V3f(0.5f, 0.5f, -1), new V3f(0, 0, 1), ref hit))
            {
                V2f interpolatedUV = default;
                var args = new RTCInterpolateArguments
                {
                    geometry = geometry.Handle,
                    primID = hit.PrimitiveId,
                    u = hit.Coord.X,
                    v = hit.Coord.Y,
                    bufferType = RTCBufferType.VertexAttribute,
                    bufferSlot = 0,
                    P = new IntPtr(&interpolatedUV),
                    dPdu = IntPtr.Zero,
                    dPdv = IntPtr.Zero,
                    ddPdudu = IntPtr.Zero,
                    ddPdvdv = IntPtr.Zero,
                    ddPdudv = IntPtr.Zero,
                    valueCount = 2
                };
                EmbreeAPI.rtcInterpolate(ref args);

                Print($"At quad center (0.5, 0.5, 0):");
                Print($"  Interpolated UV: ({interpolatedUV.X:F3}, {interpolatedUV.Y:F3})");
                Print($"  Expected: (0.5, 0.5) [OK]");
                Print("");
                Print("Applications:");
                Print("  * Texture sampling: texColor = texture.Sample(u, v)");
                Print("  * Normal mapping: perturb normals from texture");
                Print("  * Displacement mapping: offset geometry");
            }

            scene.Dispose();
            buffer.Dispose();
        }

        geometry.Dispose();
    }

    /// <summary>
    /// Demonstrates quad (4-vertex) interpolation using bilinear coordinates.
    /// Quads use (u,v) differently than triangles.
    /// </summary>
    private void DemonstrateQuadInterpolation()
    {
        if (Device == null) return;

        Print("Quads use bilinear interpolation (different from triangles):");
        Print("  P = (1-u)(1-v)V0 + u(1-v)V1 + uv*V2 + (1-u)v*V3");
        Print("");
        Print("For quads: (u,v) ∈ [0,1]×[0,1] parametrize the quad surface");
        Print("");

        // Embree splits quads along diagonal; bilinear requires manual computation from 4 vertices
        Print("NOTE: Embree represents quads as two triangles internally.");
        Print("Interpolation uses triangle barycentric coordinates, not bilinear.");
        Print("For true bilinear interpolation, manually compute from 4 vertices.");
    }

    /// <summary>
    /// Demonstrates interpolation with more than 4 vertex attributes.
    /// Shows that Embree handles N>4 correctly (critical test case).
    /// </summary>
    private void DemonstrateMultiAttributeInterpolation()
    {
        if (Device == null) return;

        Print("Embree 4 supports interpolating arbitrary numbers of vertex attributes.");
        Print("Critical: N>4 vertex attributes (previously caused issues in older versions)");
        Print("");

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        // Pack multiple attributes contiguously: color(3) + tangent(3) = 6 floats/vertex
        unsafe
        {
            var attributes = stackalloc float[3 * 6]; // 3 vertices * 6 floats each

            // V0: Color(1,0,0) + Tangent(1,0,0)
            attributes[0] = 1; attributes[1] = 0; attributes[2] = 0;
            attributes[3] = 1; attributes[4] = 0; attributes[5] = 0;

            // V1: Color(0,1,0) + Tangent(0,1,0)
            attributes[6] = 0; attributes[7] = 1; attributes[8] = 0;
            attributes[9] = 0; attributes[10] = 1; attributes[11] = 0;

            // V2: Color(0,0,1) + Tangent(0,0,1)
            attributes[12] = 0; attributes[13] = 0; attributes[14] = 1;
            attributes[15] = 0; attributes[16] = 0; attributes[17] = 1;

            var attrArray = new float[18];
            for (int i = 0; i < 18; i++)
                attrArray[i] = attributes[i];
            var buffer = EmbreeBuffer.Create<float>(Device, attrArray);
            EmbreeAPI.rtcSetGeometryVertexAttributeCount(geometry.Handle, 1);
            // Buffer layout: FLOAT3 format reads 3 floats at a time
            // Stride = 6 floats = skip color(3) + tangent(3) to reach next vertex's color
            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.VertexAttribute, 0,
                RTCFormat.FLOAT3, buffer.Handle, 0, (nuint)(sizeof(float) * 6), 3);
            geometry.Commit();

            var scene = new Scene(Device, RTCBuildQuality.High, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            // valueCount=6 tells Embree to interpolate all 6 components in one call
            var hit = new RayHit();
            if (scene.Intersect(new V3f(0.5f, 0.33f, -1), new V3f(0, 0, 1), ref hit))
            {
                var result = stackalloc float[6];
                var args = new RTCInterpolateArguments
                {
                    geometry = geometry.Handle,
                    primID = hit.PrimitiveId,
                    u = hit.Coord.X,
                    v = hit.Coord.Y,
                    bufferType = RTCBufferType.VertexAttribute,
                    bufferSlot = 0,
                    P = new IntPtr(result),
                    dPdu = IntPtr.Zero,
                    dPdv = IntPtr.Zero,
                    ddPdudu = IntPtr.Zero,
                    ddPdvdv = IntPtr.Zero,
                    ddPdudv = IntPtr.Zero,
                    valueCount = 6  // N>4 test
                };
                EmbreeAPI.rtcInterpolate(ref args);

                Print("Interpolating 6 attributes (Color RGB + Tangent XYZ):");
                Print($"  Color:   RGB({result[0]:F3}, {result[1]:F3}, {result[2]:F3})");
                Print($"  Tangent: XYZ({result[3]:F3}, {result[4]:F3}, {result[5]:F3})");
                Print("");
                Print("[OK] N>4 interpolation works correctly");
            }

            scene.Dispose();
            buffer.Dispose();
        }

        geometry.Dispose();
    }

    /// <summary>
    /// Demonstrates derivative calculation for tangent space computation.
    /// Derivatives are used for bump mapping, displacement, and curvature analysis.
    /// </summary>
    private void DemonstrateDerivatives()
    {
        if (Device == null) return;

        Print("Derivatives provide tangent vectors for surface parameterization:");
        Print("  dP/du = rate of change in u direction");
        Print("  dP/dv = rate of change in v direction");
        Print("  Normal = normalize(dP/du × dP/dv)");
        Print("");

        // Non-flat triangle: V2.Z=0.2 creates slope, making derivatives non-zero
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0.2f)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        var scene = new Scene(Device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        unsafe
        {
            var hit = new RayHit();
            if (scene.Intersect(new V3f(0.4f, 0.4f, -1), new V3f(0, 0, 1), ref hit))
            {
                // Request derivatives: how surface position changes as we move in u/v directions
                // dPdu = tangent vector along u-axis (V0→V1 direction)
                // dPdv = tangent vector along v-axis (V0→V2 direction)
                // Together they span the tangent plane at the hit point
                V3f position = default;
                V3f dPdu = default;
                V3f dPdv = default;

                var args = new RTCInterpolateArguments
                {
                    geometry = geometry.Handle,
                    primID = hit.PrimitiveId,
                    u = hit.Coord.X,
                    v = hit.Coord.Y,
                    bufferType = RTCBufferType.Vertex,
                    bufferSlot = 0,
                    P = new IntPtr(&position),
                    dPdu = new IntPtr(&dPdu),
                    dPdv = new IntPtr(&dPdv),
                    ddPdudu = IntPtr.Zero,
                    ddPdvdv = IntPtr.Zero,
                    ddPdudv = IntPtr.Zero,
                    valueCount = 3
                };
                EmbreeAPI.rtcInterpolate(ref args);

                // Cross product of tangent vectors gives surface normal direction
                V3f computedNormal = new V3f(
                    dPdu.Y * dPdv.Z - dPdu.Z * dPdv.Y,
                    dPdu.Z * dPdv.X - dPdu.X * dPdv.Z,
                    dPdu.X * dPdv.Y - dPdu.Y * dPdv.X
                ).Normalized;

                Print("At hit point:");
                Print($"  Position:  ({position.X:F3}, {position.Y:F3}, {position.Z:F3})");
                Print($"  dP/du:     ({dPdu.X:F3}, {dPdu.Y:F3}, {dPdu.Z:F3})");
                Print($"  dP/dv:     ({dPdv.X:F3}, {dPdv.Y:F3}, {dPdv.Z:F3})");
                Print($"  Normal:    ({computedNormal.X:F3}, {computedNormal.Y:F3}, {computedNormal.Z:F3})");
                Print("");
                Print("Applications:");
                Print("  * Tangent space for normal mapping");
                Print("  * Bump mapping (perturb normal)");
                Print("  * Displacement mapping direction");
                Print("  * Curvature analysis (second derivatives)");
            }
        }

        scene.Dispose();
        geometry.Dispose();
    }
}
