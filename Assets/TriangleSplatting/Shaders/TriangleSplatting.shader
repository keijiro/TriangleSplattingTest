Shader "TriangleSplatting/TriangleSplatting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Structured Buffers
            StructuredBuffer<float3> _PositionBuffer;
            StructuredBuffer<float4> _ColorBuffer;
            StructuredBuffer<float3> _NormalBuffer;
            StructuredBuffer<uint> _IndexBuffer;
            StructuredBuffer<uint> _MetadataBuffer;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                uint index = _IndexBuffer[v.vertexID];
                float3 pos = _PositionBuffer[index];
                o.vertex = UnityObjectToClipPos(float4(pos, 1.0));
                o.color = _ColorBuffer[index];
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}