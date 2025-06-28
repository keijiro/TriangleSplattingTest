
using UnityEngine;
using System.IO;
using System.Globalization;

public static class OffImporter
{
    public static Mesh Import(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        var lines = File.ReadAllLines(filePath);
        var lineIndex = 0;

        // Header
        var header = lines[lineIndex++].Trim().ToUpper();
        var hasColor = (header == "COFF");

        // Skip comments and empty lines.
        while (lineIndex < lines.Length && (lines[lineIndex].StartsWith('#') || string.IsNullOrWhiteSpace(lines[lineIndex])))
            lineIndex++;

        // Counts
        var counts = lines[lineIndex++].Split(' ');
        var vertexCount = int.Parse(counts[0]);
        var faceCount = int.Parse(counts[1]);

        // Read vertices from file
        var fileVertices = new Vector3[vertexCount];
        var fileColors = new Color[vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            // Skip comments and empty lines.
            while (lineIndex < lines.Length && (lines[lineIndex].StartsWith('#') || string.IsNullOrWhiteSpace(lines[lineIndex])))
                lineIndex++;

            // Check if we've run out of lines.
            if (lineIndex >= lines.Length)
            {
                Debug.LogError("Unexpected end of file while reading vertices.");
                return null;
            }

            var line = lines[lineIndex++];
            if (string.IsNullOrWhiteSpace(line)) { i--; continue; }

            var parts = line.Split(' ');
            fileVertices[i] = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                -float.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        // Create new mesh data by duplicating vertices for each face
        var meshVertices = new Vector3[faceCount * 3];
        var meshTriangles = new int[faceCount * 3];
        var meshColors = new Color[faceCount * 3];

        for (var i = 0; i < faceCount; i++)
        {
            // Skip comments and empty lines.
            while (lineIndex < lines.Length && (lines[lineIndex].StartsWith('#') || string.IsNullOrWhiteSpace(lines[lineIndex])))
                lineIndex++;

            // Check if we've run out of lines.
            if (lineIndex >= lines.Length)
            {
                Debug.LogError("Unexpected end of file while reading faces.");
                return null;
            }

            var line = lines[lineIndex++];
            if (string.IsNullOrWhiteSpace(line)) { i--; continue; }

            var parts = line.Split(' ');
            if (int.Parse(parts[0]) != 3) continue;

            var v1_idx = int.Parse(parts[1]);
            var v2_idx = int.Parse(parts[2]);
            var v3_idx = int.Parse(parts[3]);

            var triIndex = i * 3;

            meshVertices[triIndex]     = fileVertices[v1_idx];
            meshVertices[triIndex + 1] = fileVertices[v2_idx];
            meshVertices[triIndex + 2] = fileVertices[v3_idx];

            meshTriangles[triIndex]     = triIndex;
            meshTriangles[triIndex + 1] = triIndex + 1;
            meshTriangles[triIndex + 2] = triIndex + 2;

            if (hasColor || parts.Length > 7)
            {
                var color = new Color(
                    int.Parse(parts[4]) / 255.0f,
                    int.Parse(parts[5]) / 255.0f,
                    int.Parse(parts[6]) / 255.0f
                );
                meshColors[triIndex]     = color;
                meshColors[triIndex + 1] = color;
                meshColors[triIndex + 2] = color;
            }
        }

        // Create Mesh
        var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(filePath) };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.colors = meshColors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
