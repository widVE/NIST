Shader "Instanced/InstancedSurfaceShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _DepthTex ("Depth", 2D) = "black" {}
        _LocalPCTex ("Local PC", 2D) = "black" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Front
		//ZTest Always

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard //addshadow fullforwardshadows 
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;
        sampler2D _DepthTex;
        sampler2D _LocalPCTex;

        struct Input {
            float2 uv_MainTex;
            float4 color;
        };

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<float4> positionBuffer;
#endif

        float width;
        float height;

        float4x4 _Matrix;
        float4x4 _localMatrix;

        void rotate2D(inout float2 v, float r)
        {
            float s, c;
            sincos(r, s, c);
            v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void setup()
        {

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4 data = positionBuffer[unity_InstanceID];

          //  float rotation = data.w * data.w * _Time.y * 0.5f;
          //  rotate2D(data.xz, rotation);

            //get the depth
            int x = unity_InstanceID % width;
            int y = unity_InstanceID / width;

            float2 uv = float2((x+0.5)/width, (y+0.5)/height);

            float4 tc2 = float4(uv.x,uv.y,0,1.0);

            float4 d = tex2Dlod(_DepthTex, tc2);
            d *= 255.0;

            float4 d2 = tex2Dlod(_LocalPCTex, tc2);
            d2 *= 255.0;

            uint4 pXYZ = uint4(0,0,0,0);
            
            pXYZ.x = uint(d.z) | ((uint(d.x) & 0x0000000F) << 8);
            pXYZ.y = uint(d.y) | ((uint(d.x) & 0x000000F0) << 4);

            pXYZ.z = (uint(d2.z) & 0x000000FF) | ((uint(d2.y) & 0x000000FF) << 8);
            pXYZ.w = uint(d2.x) | ((uint(d2.w) & 0x0000000F) << 8);

            float dx = (pXYZ.x - 2048.0) / 1000.0;
            float dy = (pXYZ.y - 2048.0) / 1000.0;
            float dz = (pXYZ.z) / 1000.0;
            float cubeSize = pXYZ.w / 1000.0;

            float4 vert = float4(dx, dy, dz, 1.0);

            vert = mul(_Matrix, vert);
            
            data.xyz = vert.xzy;
            data.z = -data.z;
            data.w = cubeSize;
            data.w*=1.2;
            
            float s = data.w;
            float4x4 scale = float4x4(s,0,0,0,
                                     0,s,0,0,
                                     0,0,s,0,
                                     0,0,0,1);

            _localMatrix = mul(_localMatrix, scale);
            
            unity_ObjectToWorld._11_21_31_41 = float4(_localMatrix[0][0], _localMatrix[0][1], _localMatrix[0][2], 0);
            unity_ObjectToWorld._12_22_32_42 = float4(_localMatrix[1][0], _localMatrix[1][1], _localMatrix[1][2], 0);
            unity_ObjectToWorld._13_23_33_43 = float4(_localMatrix[2][0], _localMatrix[2][1], _localMatrix[2][2], 0);
            unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);
            unity_WorldToObject = unity_ObjectToWorld;
            //unity_WorldToObject._14_24_34 *= -1;
          //  unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
           
#endif
           
        }

        void vert (inout appdata_full v) {
             //v.vertex.xyz += float3(0, 10,0);
            //discard;
         }

        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o) {
           
            uint instanceID = 0;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            instanceID = unity_InstanceID;
#endif

            int x = instanceID % width;
            int y = instanceID / width;

            float2 uv = float2((x+0.5)/width, (y+0.5)/height);

            float4 c = float4(tex2D(_MainTex, uv).yzw, 1.0);

            if (dot(c.xyz,c.xyz) == 0)
                discard;

            o.Albedo = c;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }


        ENDCG
    }
    FallBack "Diffuse"
}