Shader "Custom/2D/Sprite-Outline"
{
    // 픽셀아트 스프라이트용 외곽선 셰이더. 알파가 있는 픽셀(스프라이트 내부)은 원래 색 그대로
    // 그리고, 알파가 없는(스프라이트 바깥) 픽셀 중 이웃(상하좌우+대각선) 텍셀에 알파가 있는
    // 픽셀이 하나라도 있으면 그 자리를 _OutlineColor로 채운다 - Point 필터 픽셀아트 기준으로
    // 딱 떨어지는 N텍셀 두께의 선명한 외곽선을 만든다. URP 2D Renderer가 Sprite-Unlit-Default와
    // 동일하게 "LightMode"="Universal2D" 태그의 패스를 그리므로 그 셰이더 구조를 그대로 따랐다.
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 1

        // Legacy properties - materials using this shader가 SpriteRenderer의 기본 프로퍼티들과
        // 호환되도록. Sprite-Unlit-Default.shader와 동일한 목적.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Color;
            half4 _RendererColor;
            half4 _OutlineColor;
            float _OutlineWidth;

            Varyings OutlineVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            half4 OutlineFragment(Varyings i) : SV_Target
            {
                half4 center = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // 스프라이트 내부(알파 있음)는 그대로 - 외곽선은 바깥쪽에만 그린다.
                if (center.a > 0.5h)
                    return center;

                float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;

                float neighborAlpha = 0;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( texel.x, 0)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-texel.x, 0)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0,  texel.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -texel.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( texel.x,  texel.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-texel.x,  texel.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( texel.x, -texel.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-texel.x, -texel.y)).a;

                if (neighborAlpha > 0)
                    return _OutlineColor;

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
