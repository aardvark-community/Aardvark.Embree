using Aardvark.Base;
using System;

namespace Aardvark.Embree
{
    public class EmbreeGeometry : IDisposable
    {
        public IntPtr Handle { get; private set; }

        protected EmbreeGeometry(Device device, RTCGeometryType type)
        {
            Handle = EmbreeAPI.rtcNewGeometry(device.Handle, type);
        }

        protected EmbreeGeometry(Device device, RTCGeometryType type, RTCBuildQuality quality)
        {
            Handle = EmbreeAPI.rtcNewGeometry(device.Handle, type);
            EmbreeAPI.rtcSetGeometryBuildQuality(Handle, quality);
        }

        public virtual void Dispose()
        {
            EmbreeAPI.rtcReleaseGeometry(Handle);
            Handle = IntPtr.Zero;
        }

        public void Commit()
        {
            EmbreeAPI.rtcCommitGeometry(Handle);
        }
    }

    public class TriangleGeometry : EmbreeGeometry
    {
        private readonly EmbreeBuffer m_vertices;
        private readonly EmbreeBuffer m_indices;

        public TriangleGeometry(Device device, V3f[] vertices, int[] triangleIndices, RTCBuildQuality quality)
            : base(device, RTCGeometryType.Triangle, quality)
        {
            m_vertices = new EmbreeBuffer(device, vertices);
            m_indices = new EmbreeBuffer(device, triangleIndices);

            // triangle index buffer needs to be UINT3
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, 0, sizeof(int) * 3, (ulong)(triangleIndices.Length / 3));
            // triangle vertex needs to be FLOAT3
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, 0, sizeof(float) * 3, (ulong)vertices.Length);
            
            Commit();

            device.CheckError("Create TriangleGeometry");
        }

        /// <summary>
        /// Create TriangleGeometry from buffers. Indices expected to be int32 and vertices to be V3f.
        /// </summary>
        public TriangleGeometry(Device device, EmbreeBuffer vertexBuffer, int vertexOffset, int vertexCount, EmbreeBuffer indexBuffer, int indexOffset, int triangleCount, RTCBuildQuality quality)
            : base(device, RTCGeometryType.Triangle, quality)
        {
            EmbreeAPI.rtcRetainBuffer(vertexBuffer.Handle);
            EmbreeAPI.rtcRetainBuffer(indexBuffer.Handle);
            m_vertices = vertexBuffer;
            m_indices = indexBuffer;
            
            // triangle index buffer needs to be UINT3
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, (ulong)indexOffset * sizeof(int), sizeof(int) * 3, (ulong)triangleCount);
            // triangle vertex needs to be FLOAT3
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, (ulong)vertexOffset, sizeof(float) * 3, (ulong)vertexCount);

            Commit();

            device.CheckError("Create TriangleGeometry");
        }

        public override void Dispose()
        {
            m_vertices.Dispose();
            m_indices.Dispose();
            base.Dispose();
        }
    }
}
