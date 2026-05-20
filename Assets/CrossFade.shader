Shader "Skybox/CrossFade Cubemaps"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0, 8)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _Blend ("Blend", Range(0, 1)) = 0
        _CubemapA ("Cubemap A", Cube) = "" {}
        _CubemapB ("Cubemap B", Cube) = "" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _CubemapA;
            samplerCUBE _CubemapB;

            half4 _Tint;
            half _Exposure;
            half _Blend;
            float _Rotation;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float3 RotateAroundYInDegrees(float3 dir, float degrees)
            {
                float angle = radians(degrees);
                float s;
                float c;
                sincos(angle, s, c);
                return float3(c * dir.x + s * dir.z, dir.y, -s * dir.x + c * dir.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                fixed4 colA = texCUBE(_CubemapA, dir);
                fixed4 colB = texCUBE(_CubemapB, dir);
                fixed4 col = lerp(colA, colB, saturate(_Blend));
                col.rgb *= _Tint.rgb * _Exposure;
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}