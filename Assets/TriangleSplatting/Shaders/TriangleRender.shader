Shader "TriangleSplatting/TriangleRender"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // Render both sides for transparency sorting

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Define a struct for triangle data (must match C# and Compute Shader)
            struct TriangleData
            {
                uint v0, v1, v2;
                float depth;
            };

            // Input buffers from C#
            StructuredBuffer<float3> Vertices;
            StructuredBuffer<float4> Colors;
            StructuredBuffer<uint> SortedTriangleIndices; // Indices into TriangleDataBuffer
            StructuredBuffer<TriangleData> TriangleDataBuffer; // Not directly used for rendering, but for reference

            float4x4 _ObjectToWorld;

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // Calculate which triangle and which vertex within that triangle we are rendering
                uint triangleIndexInSortedList = v.instanceID;
                uint vertexInTriangle = v.vertexID;

                // Get the actual original triangle index from the sorted list
                uint originalTriangleIndex = SortedTriangleIndices[triangleIndexInSortedList];

                // Let's assume TriangleDataBuffer is also passed to the shader for now.
                // This will require adding TriangleDataBuffer to renderMaterial.SetBuffer in C#.
                TriangleData currentTriangle = TriangleDataBuffer[originalTriangleIndex];

                float3 worldPos;
                float4 vertexColor;

                if (vertexInTriangle == 0)
                {
                    worldPos = Vertices[currentTriangle.v0];
                    vertexColor = Colors[currentTriangle.v0];
                }
                else if (vertexInTriangle == 1)
                {
                    worldPos = Vertices[currentTriangle.v1];
                    vertexColor = Colors[currentTriangle.v1];
                }
                else
                {
                    worldPos = Vertices[currentTriangle.v2];
                    vertexColor = Colors[currentTriangle.v2];
                }

                o.pos = UnityObjectToClipPos(mul(_ObjectToWorld, float4(worldPos, 1.0)));
                o.color = vertexColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Standard"
}
