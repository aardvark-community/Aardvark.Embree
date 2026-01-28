using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests.Diagnostics;

/// <summary>
/// Diagnostic test to investigate Occluded4 behavior.
/// Tests what values Embree rtcOccluded4 actually returns.
/// </summary>
public class Occluded4DiagnosticTest
{
    [Fact(DisplayName = "Diagnostic: Inspect Occluded4 raw tfar values")]
    public void DiagnosticOccluded4_InspectTfarValues()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays: 2 should hit triangle, 2 should miss
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Should hit - center
            new V3f(5, 5, 2),          // Should miss - far away
            new V3f(-0.3f, -0.3f, 2),  // Should hit - inside
            new V3f(10, 0, 2)          // Should miss - far right
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        // First, test with single-ray Occluded to verify expected behavior
        bool single0 = scene.Occluded(origins[0], directions[0]);
        bool single1 = scene.Occluded(origins[1], directions[1]);
        bool single2 = scene.Occluded(origins[2], directions[2]);
        bool single3 = scene.Occluded(origins[3], directions[3]);

        Console.WriteLine("=== Single-ray Occluded results ===");
        Console.WriteLine($"Ray 0 (should hit):  {single0}");
        Console.WriteLine($"Ray 1 (should miss): {single1}");
        Console.WriteLine($"Ray 2 (should hit):  {single2}");
        Console.WriteLine($"Ray 3 (should miss): {single3}");

        // Now test Occluded4
        var occluded = scene.Occluded4(origins, directions);

        Console.WriteLine("\n=== Occluded4 results ===");
        Console.WriteLine($"Ray 0 (should hit):  {occluded[0]}");
        Console.WriteLine($"Ray 1 (should miss): {occluded[1]}");
        Console.WriteLine($"Ray 2 (should hit):  {occluded[2]}");
        Console.WriteLine($"Ray 3 (should miss): {occluded[3]}");

        // Assert single-ray results are correct
        Assert.True(single0, "Single ray 0 should be occluded");
        Assert.False(single1, "Single ray 1 should not be occluded");
        Assert.True(single2, "Single ray 2 should be occluded");
        Assert.False(single3, "Single ray 3 should not be occluded");

        // Assert Occluded4 matches single-ray behavior
        Assert.Equal(single0, occluded[0]);
        Assert.Equal(single1, occluded[1]);
        Assert.Equal(single2, occluded[2]);
        Assert.Equal(single3, occluded[3]);
    }
}
