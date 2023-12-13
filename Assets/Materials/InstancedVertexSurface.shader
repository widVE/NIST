Shader "Instanced/InstancedVertexShader" {
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
 
            StructuredBuffer<float4> positionBuffer;
            StructuredBuffer<float4> colorBuffer;
            
			struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
			
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				//UNITY_VERTEX_OUTPUT_STEREO
            };
 
            v2f vert(appdata v, uint instanceID : SV_InstanceID) {
               
                float4 data = positionBuffer[instanceID];  
                float4 color = colorBuffer[instanceID];
                //float3 localPosition = v.vertex.xyz * data.w;
                float3 worldPosition = data.xyz + v.vertex.xyz * 0.0015;//localPosition;
				//worldPosition *= 10;
                //float3 worldNormal = v.normal;
                               
                v2f o;
				o.vertex = float4(0,0,0,0);
				o.color = float4(0,0,0,0);
				UNITY_SETUP_INSTANCE_ID(o);
			    UNITY_INITIALIZE_OUTPUT(v2f, o);
			    //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.color = color;
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