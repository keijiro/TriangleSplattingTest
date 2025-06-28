
using UnityEngine;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OffMeshViewer : MonoBehaviour
{
    public string offFilePath = "References/Pretrained/garden.off";

    void Start()
    {
        var fullPath = System.IO.Path.Combine(Application.dataPath, "..", offFilePath);

        var mesh = OffImporter.Import(fullPath);
        if (mesh == null) return;

        GetComponent<MeshFilter>().mesh = mesh;

        var material = new Material(Shader.Find("Unlit/VertexColor"));
        GetComponent<MeshRenderer>().material = material;
    }
}
