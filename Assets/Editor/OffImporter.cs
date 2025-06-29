using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.Linq;

[ScriptedImporter(1, "off")]
public class OffImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext context)
    {
        try
        {
            EditorUtility.DisplayProgressBar("OFF Importer", "Reading file...", 0.0f);

            var text = System.IO.File.ReadAllText(context.assetPath);
            var lines = text.Split('\n');

            var vertices = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var allFaces = new System.Collections.Generic.List<FaceInfo>();

            var lineIndex = 0;

            // Read OFF header
            if (lines[lineIndex].Trim() != "COFF")
            {
                Debug.LogError("Invalid OFF file header.");
                return;
            }
            lineIndex++;

            // Read counts (vertices, faces, edges)
            var counts = lines[lineIndex].Trim().Split(' ');
            var vertexCount = int.Parse(counts[0]);
            var faceCount = int.Parse(counts[1]);
            lineIndex++;

            EditorUtility.DisplayProgressBar("OFF Importer", "Reading vertices...", 0.1f);

            // Read vertices and colors
            for (var i = 0; i < vertexCount; i++)
            {
                if (i % 1000 == 0)
                    EditorUtility.DisplayProgressBar("OFF Importer", $"Reading vertices... ({i}/{vertexCount})", 0.1f + 0.3f * i / vertexCount);

                var components = lines[lineIndex].Trim().Split(' ');
                vertices.Add(new Vector3(
                    float.Parse(components[0]),
                    float.Parse(components[1]),
                    float.Parse(components[2])
                ));
                if (components.Length >= 6) // RGB color
                {
                    colors.Add(new Color(
                        int.Parse(components[3]) / 255f,
                        int.Parse(components[4]) / 255f,
                        int.Parse(components[5]) / 255f
                    ));
                }
                else
                {
                    colors.Add(Color.white);
                }
                lineIndex++;
            }

            EditorUtility.DisplayProgressBar("OFF Importer", "Reading faces...", 0.4f);

            // Read faces and store them with centroids
            for (var i = 0; i < faceCount; i++)
            {
                if (i % 1000 == 0)
                    EditorUtility.DisplayProgressBar("OFF Importer", $"Reading faces... ({i}/{faceCount})", 0.4f + 0.3f * i / faceCount);

                var components = lines[lineIndex].Trim().Split(' ');
                var numVerticesInFace = int.Parse(components[0]);
                if (numVerticesInFace == 3) // Only support triangles for now
                {
                    var v0 = int.Parse(components[1]);
                    var v1 = int.Parse(components[2]);
                    var v2 = int.Parse(components[3]);

                    Color faceColor = Color.white;
                    if (components.Length >= 7) // Check for RGBa color (3 vertices + 4 color components)
                    {
                        float r = int.Parse(components[4]) / 255f;
                        float g = int.Parse(components[5]) / 255f;
                        float b = int.Parse(components[6]) / 255f;
                        float a = components.Length >= 8 ? int.Parse(components[7]) / 255f : 1f;
                        faceColor = new Color(r, g, b, a);
                    }

                    var centroid = (vertices[v0] + vertices[v1] + vertices[v2]) / 3.0f;
                    allFaces.Add(new FaceInfo { v0 = v0, v1 = v1, v2 = v2, centroid = centroid, color = faceColor });
                }
                lineIndex++;
            }

            EditorUtility.DisplayProgressBar("OFF Importer", "Building hierarchy...", 0.7f);

            // Calculate overall bounds of the model
            Bounds modelBounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var v in vertices)
            {
                modelBounds.Encapsulate(v);
            }

            // Build Octree
            OctreeNode rootNode = new OctreeNode(modelBounds);
            foreach (var face in allFaces)
            {
                rootNode.AddFace(face);
            }

            // Max faces per node is an approximation for MAX_VERTICES_PER_CHUNK / 3
            // We use 65535 / 3 = 21845 as a rough estimate
            const int MAX_FACES_PER_NODE = 20000;
            const int MAX_OCTREE_DEPTH = 10; // Limit depth to prevent excessive subdivision

            rootNode.Build(MAX_OCTREE_DEPTH, MAX_FACES_PER_NODE);

            const int MAX_VERTICES_PER_CHUNK = 65535; // Max vertices for 16-bit index

            var rootGameObject = new GameObject(System.IO.Path.GetFileNameWithoutExtension(context.assetPath));
            context.AddObjectToAsset("main asset", rootGameObject);
            context.SetMainObject(rootGameObject);

            // Create and add the material asset once
            var material = new Material(Shader.Find("Unlit/VertexColor"));
            context.AddObjectToAsset("UnlitVertexColorMaterial", material);

            var meshIndex = 0;
            var leafNodes = rootNode.GetLeafNodes().ToList();
            var leafNodeCount = leafNodes.Count;

            EditorUtility.DisplayProgressBar("OFF Importer", "Creating meshes...", 0.8f);

            // Iterate through leaf nodes of the Octree and create meshes
            for (var i = 0; i < leafNodeCount; i++)
            {
                var leafNode = leafNodes[i];
                EditorUtility.DisplayProgressBar("OFF Importer", $"Creating meshes... ({i}/{leafNodeCount})", 0.8f + 0.2f * i / leafNodeCount);

                var currentVertices = new System.Collections.Generic.List<Vector3>();
                var currentColors = new System.Collections.Generic.List<Color>();
                var currentTriangles = new System.Collections.Generic.List<int>();
                var currentVertexMap = new System.Collections.Generic.Dictionary<VertexData, int>();

                foreach (var face in leafNode.faces)
                {
                    // Check if adding this triangle exceeds MAX_VERTICES_PER_CHUNK
                    if (currentVertexMap.Count + 3 > MAX_VERTICES_PER_CHUNK)
                    {
                        CreateMeshChunk(context, rootGameObject, currentVertices, currentColors, currentTriangles, material, meshIndex++);
                        currentVertices.Clear();
                        currentColors.Clear();
                        currentTriangles.Clear();
                        currentVertexMap.Clear();
                    }

                    // Add vertices and colors, remapping indices
                    var newV0 = AddVertex(currentVertices, currentColors, currentVertexMap, vertices[face.v0], face.color);
                    var newV1 = AddVertex(currentVertices, currentColors, currentVertexMap, vertices[face.v1], face.color);
                    var newV2 = AddVertex(currentVertices, currentColors, currentVertexMap, vertices[face.v2], face.color);

                    currentTriangles.Add(newV0);
                    currentTriangles.Add(newV1);
                    currentTriangles.Add(newV2);
                }

                // Create the last mesh chunk for the current leaf node if there's any remaining data
                if (currentTriangles.Count > 0)
                {
                    CreateMeshChunk(context, rootGameObject, currentVertices, currentColors, currentTriangles, material, meshIndex);
                }
                meshIndex++;
            }

            Debug.Log($"Successfully imported {context.assetPath} with {meshIndex} mesh chunks.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private class OctreeNode
    {
        public Bounds bounds;
        public System.Collections.Generic.List<FaceInfo> faces;
        public OctreeNode[] children;
        private int currentDepth;

        public OctreeNode(Bounds bounds, int depth = 0)
        {
            this.bounds = bounds;
            this.faces = new System.Collections.Generic.List<FaceInfo>();
            this.currentDepth = depth;
        }

        public void AddFace(FaceInfo face)
        {
            faces.Add(face);
        }

        public void Build(int maxDepth, int maxFacesPerNode)
        {
            if (currentDepth >= maxDepth || faces.Count <= maxFacesPerNode)
            {
                return; // Stop subdivision
            }

            children = new OctreeNode[8];
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = bounds.center;
            Vector3 halfSize = bounds.extents;

            // Create 8 children nodes
            children[0] = new OctreeNode(new Bounds(center + new Vector3(-halfSize.x / 2, -halfSize.y / 2, -halfSize.z / 2), halfSize), currentDepth + 1); // --- (minX, minY, minZ)
            children[1] = new OctreeNode(new Bounds(center + new Vector3( halfSize.x / 2, -halfSize.y / 2, -halfSize.z / 2), halfSize), currentDepth + 1); // +-- (maxX, minY, minZ)
            children[2] = new OctreeNode(new Bounds(center + new Vector3(-halfSize.x / 2,  halfSize.y / 2, -halfSize.z / 2), halfSize), currentDepth + 1); // -+- (minX, maxY, minZ)
            children[3] = new OctreeNode(new Bounds(center + new Vector3( halfSize.x / 2,  halfSize.y / 2, -halfSize.z / 2), halfSize), currentDepth + 1); // ++- (maxX, maxY, minZ)
            children[4] = new OctreeNode(new Bounds(center + new Vector3(-halfSize.x / 2, -halfSize.y / 2,  halfSize.z / 2), halfSize), currentDepth + 1); // --+ (minX, minY, maxZ)
            children[5] = new OctreeNode(new Bounds(center + new Vector3( halfSize.x / 2, -halfSize.y / 2,  halfSize.z / 2), halfSize), currentDepth + 1); // +-+ (maxX, minY, maxZ)
            children[6] = new OctreeNode(new Bounds(center + new Vector3(-halfSize.x / 2,  halfSize.y / 2,  halfSize.z / 2), halfSize), currentDepth + 1); // -++ (minX, maxY, maxZ)
            children[7] = new OctreeNode(new Bounds(center + new Vector3( halfSize.x / 2,  halfSize.y / 2,  halfSize.z / 2), halfSize), currentDepth + 1); // +++ (maxX, maxY, maxZ)

            // Distribute faces to children
            foreach (var face in faces)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (children[i].bounds.Contains(face.centroid))
                    {
                        children[i].AddFace(face);
                        break; // Assuming each face belongs to only one child node
                    }
                }
            }
            faces.Clear(); // Clear faces from parent node once distributed

            // Recursively build children
            foreach (var child in children)
            {
                child.Build(maxDepth, maxFacesPerNode);
            }
        }

        public System.Collections.Generic.IEnumerable<OctreeNode> GetLeafNodes()
        {
            if (children == null)
            {
                yield return this;
            }
            else
            {
                foreach (var child in children)
                {
                    foreach (var leaf in child.GetLeafNodes())
                    {
                        yield return leaf;
                    }
                }
            }
        }
    }

    private void CreateMeshChunk(AssetImportContext context, GameObject parent, System.Collections.Generic.List<Vector3> chunkVertices, System.Collections.Generic.List<Color> chunkColors, System.Collections.Generic.List<int> chunkTriangles, Material material, int index)
    {
        var mesh = new Mesh();
        mesh.SetVertices(chunkVertices);
        mesh.SetColors(chunkColors);
        mesh.SetTriangles(chunkTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var meshName = $"MeshChunk_{index}";
        context.AddObjectToAsset(meshName, mesh);

        var go = new GameObject(meshName);
        go.transform.SetParent(parent.transform);
        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        context.AddObjectToAsset(go.name, go);
    }

    private struct FaceInfo
    {
        public int v0, v1, v2;
        public Vector3 centroid;
        public Color color;
    }

    private struct VertexData
    {
        public Vector3 position;
        public Color color;

        public override bool Equals(object obj)
        {
            if (!(obj is VertexData other))
            {
                return false;
            }
            return position.Equals(other.position) && color.Equals(other.color);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(position, color);
        }
    }

    private int AddVertex(System.Collections.Generic.List<Vector3> currentVertices, System.Collections.Generic.List<Color> currentColors, System.Collections.Generic.Dictionary<VertexData, int> vertexMap, Vector3 vertex, Color color)
    {
        var data = new VertexData { position = vertex, color = color };
        if (vertexMap.TryGetValue(data, out int index))
        {
            return index;
        }

        currentVertices.Add(vertex);
        currentColors.Add(color);
        index = currentVertices.Count - 1;
        vertexMap.Add(data, index);
        return index;
    }
}
