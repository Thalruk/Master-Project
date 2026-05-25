Shader "Custom/VoxelTextureShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture Array", 2DArray) = "" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _AOIntensity ("AO Intensity", Range(0,1)) = 1.0
        [Toggle(COMPUTE_AO_ON)] _EnableAO ("Enable Vertex AO", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma multi_compile_instancing
        #pragma multi_compile __ COMPUTE_AO_ON
        #pragma target 3.5

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
            float2 uv_MainTex;
            float texIndex;
            float4 vertexColor : COLOR;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_MainTex = v.texcoord.xy;
            o.texIndex = v.texcoord.z;
            o.vertexColor = v.color; 
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _AOIntensity;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 uv3 = float3(IN.uv_MainTex, IN.texIndex);
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv3) * _Color;
            
            float3 ambientLighting = float3(1, 1, 1);

            #ifdef COMPUTE_AO_ON
                ambientLighting = lerp(float3(1, 1, 1), IN.vertexColor.rgb, _AOIntensity);
            #endif
            
            o.Albedo = c.rgb * ambientLighting;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}