Shader "Unlit/CopyDepthShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest Always
        Cull Off
        ZWrite Off
   
        Pass
        {
            Name "Unlit"
           
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
       
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
            };
       
            sampler2D_float _MainTex;
			//Texture2D _MainTex;
            float4 _MainTex_ST;
            int _Orientation;
			
            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
           
                // Flip X
                o.uv = float2(v.uv.x, v.uv.y);
                o.uv = TRANSFORM_TEX(o.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                //return (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv)/65535.0);
				float val = tex2D(_MainTex, i.uv).r;//(float)(((float)_MainTex.Load(int3(i.uv.x*320, i.uv.y*288, 0)).r;//*4000.0)/255.0);
				return float4(val, val, val, val);
            }
            ENDHLSL
        }
    }
}