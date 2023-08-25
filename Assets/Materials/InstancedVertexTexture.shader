Shader "Instanced/InstancedVertexTextureShader" {
    Properties
    {
        _GeomTexture ("GeomTexture", 2D) = "black" {}
        _ColorTexture ("ColorTexture", 2D) = "black" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
 
            sampler2D _GeomTexture;
            sampler2D _ColorTexture;
            
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
 
            v2f vert(appdata v, uint instanceID : SV_InstanceID) 
            {
                
                float x = instanceID % 320;
                float y = instanceID / 320;
                float4 data = tex2Dlod(_GeomTexture, float4(x,y, 0, 0));
                float4 color = tex2Dlod(_ColorTexture, float4(x,y, 0, 0));

                //float4 data = positionBuffer[instanceID];  
                //float4 color = colorBuffer[instanceID];
                //float3 localPosition = v.vertex.xyz * data.w;
                float3 worldPosition = data.xyz + v.vertex.xyz;//localPosition;
				//worldPosition *= 10;
                //float3 worldNormal = v.normal;
                               
                v2f o;
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