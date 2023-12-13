
Shader "Custom/DepthMapPointsTransform" {
	Properties {
		_MinBounds ("Min Bounds", Vector) = (-20, -20, -20, 1)
		_MaxBounds ("Max Bounds", Vector) = (20, 20, 20, 1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_AmbientOcclusion ("Ambient Occlusion", Range(0,1)) = 0.0
		_ColorImage("Color Image", 2D) = "white" {}
		_DepthImage("Depth Image", 2D) = "black" {}
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
		
		//#define USE_CPU_DEPTH 1

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
		float _Scale;
		
		float4x4 _CameraIntrinsics;
		float4x4 _ModelTransform;
		float4x4 _ViewProj;

		sampler2D _ColorImage;
		sampler2D _DepthImage;

		int useNormals = 1;
		int _SubSample = 1;

		int colorType = 0;	//0 = color, 1 = depth, 2 = conf, 3 = flow, 4 = deriv
		
		void vert (inout appdata v, out Input o) 
        {
			//if(v.id % 1000 != 0)
			//{
			//	return;
			//}

			UNITY_INITIALIZE_OUTPUT(Input,o);
				
			//if(v.vertex.x > _MinBounds.x && v.vertex.y > _MinBounds.y && v.vertex.z > _MinBounds.z && 
			//	v.vertex.x < _MaxBounds.x && v.vertex.y < _MaxBounds.y && v.vertex.z < _MaxBounds.z)
			//{
				uint resX = (uint)_ResolutionX;
				int idW = (v.id * _SubSample) % resX;
				int idH = (v.id * _SubSample) / resX;

				float4 tc = float4((((float)idW+0.5)/_ResolutionX), (((float)idH+0.5)/_ResolutionY), 0, 1);
				float4 tc2 = tc;//float4(1.0-((float)idW+0.5)/_ResolutionX, 1.0-(((float)idH+0.5)/_ResolutionY), 0, 1);

#if USE_CPU_DEPTH
				tc2.y = tc2.y + 0.5;
				if(tc2.y >= 1.0)
				{
					tc2.y = tc2.y - 1.0;
				}
#endif


				float4 d = tex2Dlod(_DepthImage, tc2);
				//uint4 d = _DepthImage.Load(tc2);
				//if(d.x > 0.0)
				{
					d.w = 1.0;
					//d.xyz *= 65536;

					float dx = d.x;//(d.x - 32768.0) / 1000.0;
					float dy = d.y;//(d.y - 32768.0) / 1000.0;
					float dz = d.z;//(d.z - 32768.0) / 1000.0;

					float4 vert = float4(dx, dy, dz, 1.0);
					//d.x = d.x * 65536.0;
					//d.x = d.x / 5000.0;
					//

					vert = mul(_ModelTransform, vert);
					
					//vert.z = -vert.z;
					//float t = vert.y;
					//vert.y = vert.z;
					//vert.z = t;
					
					v.vertex = vert;
					//v.pointSize = 1.0;

					if(useNormals == 1)
					{
						v.normal = float3(0,1,0);
						v.tangent = float4(1,0,0,0);
					}
					
					o.normal = v.normal;
					v.color = tex2Dlod(_ColorImage, tc);
					o.color = v.color;


					UNITY_TRANSFER_FOG(o,UnityObjectToClipPos(v.vertex));
				}

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