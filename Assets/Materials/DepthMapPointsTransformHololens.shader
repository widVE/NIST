
Shader "Custom/DepthMapPointsTransformHololens" {
	Properties {
		_MinBounds ("Min Bounds", Vector) = (-20, -20, -20, 1)
		_MaxBounds ("Max Bounds", Vector) = (20, 20, 20, 1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_AmbientOcclusion ("Ambient Occlusion", Range(0,1)) = 0.0
		_ColorImage("Color Image", 2D) = "white" {}
		_DepthImage("Depth Image", 2D) = "black" {}
		_ConfidenceImage("Confidence Image", 2D) = "white" {}
	}
    SubShader
    {
		Tags 
		{ 
			"RenderType"="Opaque"
		}
		
		LOD 200
		ZWrite On
		ZTest LEqual
		
		CGPROGRAM
		
		#include "UnityCG.cginc"
		
		#pragma surface surf Standard fullforwardshadows vertex:vert
		//#pragma multi_compile_fog
		#pragma target 5.0
		//#pragma exclude_renderers nomrt
		//#pragma multi_compile_lightpass
		//#pragma multi_compile ___ UNITY_HDR_ON
			
		struct appdata {
            float4 vertex : POSITION;
			float3 normal : NORMAL;
			//float pointSize : PSIZE;
			float4 color : COLOR;
			float4 tangent : TANGENT;
			float4 texcoord0 : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint id : SV_VertexID;
         };
		
		 struct Input {
            float4 color;
			float3 normal;
			UNITY_FOG_COORDS(0)
         };
		
		float4 _MinBounds;
		float4 _MaxBounds;
		
		float _Glossiness;
		float _Metallic;
		float _AmbientOcclusion;
		
		float _ResolutionX;
		float _ResolutionY;
		
		float4x4 _CameraIntrinsics;
		float4x4 _ModelTransform;
		float4x4 _ViewProj;

		sampler2D_float _ColorImage;
		sampler2D_float _DepthImage;
		sampler2D _ConfidenceImage;

		int useNormals = 0;
		
		void vert (inout appdata v, out Input o) 
        {
			UNITY_INITIALIZE_OUTPUT(Input,o);
				
			//if(v.vertex.x > _MinBounds.x && v.vertex.y > _MinBounds.y && v.vertex.z > _MinBounds.z && 
			//	v.vertex.x < _MaxBounds.x && v.vertex.y < _MaxBounds.y && v.vertex.z < _MaxBounds.z)
			//{
				uint resX = (uint)_ResolutionX;
				
				int idW = v.id % resX;
				int idH = v.id / resX;

				float4 tc = float4(((float)idW+0.5)/_ResolutionX, 1.0-(((float)idH+0.5)/_ResolutionY), 0, 1);
				
				float4 c = tex2Dlod(_ConfidenceImage, tc);

				//hololens needs this check...
				//if(c.x > 0)
				{
					float4 d = tex2Dlod(_DepthImage, tc);
					if(d.x > 0.0)
					{
						v.color = tex2Dlod(_ColorImage, tc);
						if(length(v.color.xyz) > 0.0)
						{
							//d.x = d.x * 65536.0;
							//d.x = d.x / 5000.0;
							d.x = d.x / 255.0;
							d.x = d.x * 4000.0;
							
							float4 vert = mul(_CameraIntrinsics, float4((float)idW+0.5, (float)idH+0.5, 1.0, 0.0));
							vert *= d.x;
							vert.w = 1.0;

							vert = mul(_ModelTransform, vert);
							vert.xyzw /= vert.w;
							//float t = vert.y;
							//vert.y = vert.z;
							//vert.z = t;
							
							v.vertex = vert;
							//v.pointSize = 1.0;

							if(useNormals == 1)
							{
								v.normal = float3(0,1,0);//tex2Dlod(DepthNormalsTexture, float4(idW, idH, 0, 0)).xyz;
								v.tangent = float4(1,0,0,0);//tex2Dlod(TangentTexture, float4(idW, idH, 0, 0));
							}
							
							o.normal = v.normal;
							
							o.color = v.color;


							UNITY_TRANSFER_FOG(o,UnityObjectToClipPos(v.vertex));
						}
					}
				}
			//}
			//else
			//{
			//	v.vertex = float4(0,0,0,1);
			//}
		}		
		
		void surf (Input IN, inout SurfaceOutputStandard o)//SurfaceOutputStandardSpecular o) 
		{
			o.Normal = IN.normal;
			o.Albedo = IN.color;
			//o.Specular = IN.color;
			o.Smoothness = _Glossiness;
			o.Occlusion = _AmbientOcclusion;
			o.Metallic = _Metallic;
			o.Alpha = 1; 
			//o.Emission = half4(1.0, 1.0, 1.0, 1.0);//IN.color;//
		}
		
		ENDCG
    }
	
	Fallback "Standard"
}