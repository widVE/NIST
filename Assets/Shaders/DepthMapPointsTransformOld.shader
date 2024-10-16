
Shader "Custom/DepthMapPointsTransformOld" {
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
		Cull Back
		
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

		//sampler2D _ColorImage;
		//sampler2D _DepthImage;

		int useNormals = 1;
		int _SubSample = 1;

		int colorType = 0;	//0 = color, 1 = depth, 2 = conf, 3 = flow, 4 = deriv
		
		/*float4x4 inverse(float4x4 m) {
			float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
			float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
			float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
			float n41 = 0, n42 = 0, n43 = 0, n44 = m[3][3];

			float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
			float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
			float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
			float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

			float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
			float idet = 1.0f / det;

			float4x4 ret;

			ret[0][0] = t11 * idet;
			ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
			ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
			ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

			ret[1][0] = t12 * idet;
			ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
			ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
			ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

			ret[2][0] = t13 * idet;
			ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
			ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
			ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

			ret[3][0] = t14 * idet;
			ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
			ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
			ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

			return ret;
		}*/

		void vert (inout appdata v, out Input o) 
        {
			//if(v.id % 1000 != 0)
			//{
			//	return;
			//}

			UNITY_INITIALIZE_OUTPUT(Input,o);
			UNITY_SETUP_INSTANCE_ID(o);
				
			//if(v.vertex.x > _MinBounds.x && v.vertex.y > _MinBounds.y && v.vertex.z > _MinBounds.z && 
			//	v.vertex.x < _MaxBounds.x && v.vertex.y < _MaxBounds.y && v.vertex.z < _MaxBounds.z)
			//{
				uint resX = (uint)_ResolutionX;
				float idW = (v.id * _SubSample) % resX;
				float idH = (v.id * _SubSample) / resX;

				float4 tc = float4((((float)idW+0.5)/_ResolutionX), (((float)idH+0.5)/_ResolutionY), 0, 0);
				float4 tc2 = tc;//float4(1.0-((float)idW+0.5)/_ResolutionX, 1.0-(((float)idH+0.5)/_ResolutionY), 0, 1);

#if USE_CPU_DEPTH
				tc2.y = tc2.y + 0.5;
				if(tc2.y >= 1.0)
				{
					tc2.y = tc2.y - 1.0;
				}
#endif


				float4 d = v.vertex;
				//float4 d = tex2Dlod(_DepthImage, tc2);
				//uint4 d = _DepthImage.Load(tc2);
				//if(d.x > 0.0)
				{
					d.w = 1.0;
					//d.xyz *= 65536;

					float dx = d.x;
					float dy = d.y;
					float dz = d.z;
					
					//float dx = (d.x - 32768.0) / 1000.0;
					//float dy = (d.y - 32768.0) / 1000.0;
					//float dz = (d.z - 32768.0) / 1000.0;

					float4 vert = float4(dx, dy, dz, 1.0);

					vert = mul(_ModelTransform, vert);
					
					//vert.z = -vert.z;
					//float t = vert.y;
					//vert.y = vert.z;
					//vert.z = t;
					
					v.vertex = vert;
					//v.pointSize = 5.0;

					if(useNormals == 1)
					{
						v.normal = float3(0,1,0);
						v.tangent = float4(1,0,0,0);
					}
					
					o.normal = v.normal;//mul(inverse(_ModelTransform), v.normal);
					//v.color = tex2Dlod(_ColorImage, tc);
					o.color = v.color;

					UNITY_TRANSFER_FOG(o,UnityObjectToClipPos(v.vertex));
				}

		}		
		
		void surf (Input IN, inout SurfaceOutputStandard o)//SurfaceOutputStandardSpecular o) 
		{
			o.Normal = float3(0,1,0);
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