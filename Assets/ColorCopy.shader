Shader "Unlit/CopyColorShader" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _Color("Multiplicative color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			sampler2D _MainTex;
			sampler2D_float _DepthTexX;
			sampler2D_float _DepthTexY;
			sampler2D_float _DepthTexZ;
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
			uniform float _depthWidth;
			uniform float _depthHeight;
            uniform int _Orientation;
			uniform float4x4 _mvpColor;
			uniform float4x4 _depthToWorld;
            

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = float2(v.texcoord.x, v.texcoord.y);
           
                /*if (_Orientation == 1) {
                    // Portrait
                    o.texcoord = float2(1.0 - o.texcoord.y, o.texcoord.x);
                }
                else if (_Orientation == 3) {
                    // Landscape left
                    o.texcoord = float2(1.0 - o.texcoord.x, 1.0 - o.texcoord.y);
                }*/
                o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				//do lookups etc here...
				float2 tc = i.texcoord;
				tc.y = 1.0 - tc.y;
				float dX = tex2D(_DepthTexX, tc);
				float dY = tex2D(_DepthTexY, tc);
				float dZ = tex2D(_DepthTexZ, tc);
				//also does sample screenspace texture return 0->1 or 0->255?
				if(length(float3(dX, dY, dZ)) > 0)
				{
					float wIndex = (tc.x * _depthWidth);
					float hIndex = (tc.y * _depthHeight);
					float4 cameraPoint = float4(wIndex + 0.5, hIndex + 0.5, 1.0, 0.0);
					if(cameraPoint.x >= 0 && cameraPoint.x < _depthWidth && cameraPoint.y >= 0 && cameraPoint.y < _depthHeight)
					{
						float4 newCameraPoint = float4(dX, dY, dZ, 1.0);//cameraPoint.x, cameraPoint.y, cameraPoint.z, 1.0);
						newCameraPoint = mul(_depthToWorld, newCameraPoint);
						newCameraPoint.w = 1.0;
						float4 projPos = mul(_mvpColor, newCameraPoint);
						projPos.xyz /= projPos.w;
						projPos.xyz = projPos.xyz * 0.5 + 0.5;
						projPos.y = 1.0 - projPos.y;
						if(projPos.x >= 0 && projPos.x < 1.0 && projPos.y >= 0 && projPos.y < 1.0)// && projPos.z >= 0 && projPos.z < 1.0)	//adding the z checks causes it to not work...
						{
							return tex2D(_MainTex, projPos.xy);//UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, projPos.xy); 
						}
						else
						{
							return fixed4(1,0,0,1);//
						}
					}
					//return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord) * _Color;
					return fixed4(0,1,0,1);//
				}
				
				return fixed4(0,0,1,1);//
            }
            ENDCG

        }
    }
    Fallback Off
}