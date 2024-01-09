Shader "InsideVisible-3D-UNDER"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Cull Off  // Front | Back | Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile __ COLOR_CORRECTION

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				v.texcoord.x = 1 - v.texcoord.x;
				v.texcoord.y = 1 - v.texcoord.y;

				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				i.texcoord.y = i.texcoord.y / 2;
				float4 col = tex2D(_MainTex, i.texcoord);
#if !UNITY_COLORSPACE_GAMMA && COLOR_CORRECTION
				col.rgb = GammaToLinearSpace(col.rgb); // Remove gamma correction
#endif
				return col;
			}
			ENDCG
		}
	}
}