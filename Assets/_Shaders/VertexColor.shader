Shader "Custom/VertexColorShader"
{
    Properties
    {
        // Add properties if you need additional control
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // Vertex color from the mesh
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR; // Pass the vertex color to the fragment shader
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color; // Pass vertex color to fragment
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color; // Output the vertex color as the final pixel color
            }
            ENDCG
        }
    }
}
