using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public class TriangleSplattingRenderer : MonoBehaviour
{
    public string offFilePath;
    public Material renderMaterial;
    public ComputeShader sortComputeShader;

    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _triangleDataBuffer;
    private ComputeBuffer _sortedTriangleIndicesBuffer;
    private ComputeBuffer _indirectArgsBuffer;

    private int _triangleCount;

    private struct TriangleData
    {
        public uint v0, v1, v2;
        public float depth;
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(offFilePath))
        {
            Debug.LogError("OFF file path is not set.");
            return;
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, offFilePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"File not found: {fullPath}");
            return;
        }

        OffData offData = OffImporter.Import(fullPath);
        _triangleCount = offData.triangleCount;

        if (_triangleCount == 0)
        {
            Debug.LogError("No triangles imported.");
            return;
        }

        // Initialize buffers
        _vertexBuffer = new ComputeBuffer(offData.vertices.Length, sizeof(float) * 3);
        _vertexBuffer.SetData(offData.vertices);

        _colorBuffer = new ComputeBuffer(offData.colors.Length, sizeof(float) * 4);
        _colorBuffer.SetData(offData.colors);

        TriangleData[] triangleDataArray = new TriangleData[_triangleCount];
        for (int i = 0; i < _triangleCount; i++)
        {
            triangleDataArray[i].v0 = (uint)offData.triangleIndices[i * 3];
            triangleDataArray[i].v1 = (uint)offData.triangleIndices[i * 3 + 1];
            triangleDataArray[i].v2 = (uint)offData.triangleIndices[i * 3 + 2];
            triangleDataArray[i].depth = 0.0f; // Will be calculated in compute shader
        }
        _triangleDataBuffer = new ComputeBuffer(_triangleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(TriangleData)));
        _triangleDataBuffer.SetData(triangleDataArray);

        _sortedTriangleIndicesBuffer = new ComputeBuffer(_triangleCount, sizeof(uint));
        uint[] initialIndices = new uint[_triangleCount];
        for (uint i = 0; i < _triangleCount; i++)
        {
            initialIndices[i] = i;
        }
        _sortedTriangleIndicesBuffer.SetData(initialIndices);

        // Indirect arguments for Graphics.DrawProceduralIndirect
        // 0: vertex count per instance
        // 1: instance count
        // 2: start vertex location
        // 3: start instance location
        _indirectArgsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[4] { 3, (uint)_triangleCount, 0, 0 };
        _indirectArgsBuffer.SetData(args);
    }

    void Update()
    {
        if (_triangleCount == 0 || renderMaterial == null || sortComputeShader == null)
        {
            return;
        }

        // Phase 1: Calculate Depths
        int calculateDepthsKernel = sortComputeShader.FindKernel("CalculateDepths");
        sortComputeShader.SetBuffer(calculateDepthsKernel, "Vertices", _vertexBuffer);
        sortComputeShader.SetBuffer(calculateDepthsKernel, "TriangleDataBuffer", _triangleDataBuffer);
        sortComputeShader.SetMatrix("ViewMatrix", Camera.main.worldToCameraMatrix);
        sortComputeShader.SetVector("CameraWorldPos", Camera.main.transform.position);
        sortComputeShader.Dispatch(calculateDepthsKernel, (_triangleCount + 63) / 64, 1, 1);

        // Phase 2: Sort Triangles (Placeholder for actual sorting algorithm)
        // For now, we'll just assume _sortedTriangleIndicesBuffer is correctly sorted.
        // In a real implementation, this would involve multiple compute shader dispatches
        // for a full GPU sorting algorithm like Bitonic Sort or Radix Sort.
        // For prototyping, we might implement a simple CPU sort here for testing,
        // or rely on a pre-sorted buffer if the data is small.

        // Set buffers for rendering
        renderMaterial.SetBuffer("Vertices", _vertexBuffer);
        renderMaterial.SetBuffer("Colors", _colorBuffer);
        renderMaterial.SetBuffer("SortedTriangleIndices", _sortedTriangleIndicesBuffer);
        renderMaterial.SetBuffer("TriangleDataBuffer", _triangleDataBuffer);
        renderMaterial.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);

        // Draw
        Graphics.DrawProceduralIndirect(renderMaterial, new Bounds(Vector3.zero, Vector3.one * 1000f), MeshTopology.Triangles, _indirectArgsBuffer);
    }

    void OnDestroy()
    {
        _vertexBuffer?.Release();
        _colorBuffer?.Release();
        _triangleDataBuffer?.Release();
        _sortedTriangleIndicesBuffer?.Release();
        _indirectArgsBuffer?.Release();
    }
}
