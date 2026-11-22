Shader "Pulling/SkyLightGradient"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TopAlpha ("Top Alpha", Range(0, 1)) = 0.32
        _BottomAlpha ("Bottom Alpha", Range(0, 1)) = 0.18
        _HeightMin ("Height Min", Float) = 0
        _HeightMax ("Height Max", Float) = 1
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                #ifndef DEBUG_DISPLAY
                float3 positionWS : TEXCOORD2;
                #endif
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #ifndef UNITY_INSTANCING_ENABLED
                #define unity_SpriteColor half4(1, 1, 1, 1)
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _TopAlpha;
                half _BottomAlpha;
                float _HeightMin;
                float _HeightMax;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                #ifndef DEBUG_DISPLAY
                o.positionWS = TransformObjectToWorld(input.positionOS);
                #endif
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float span = max(_HeightMax - _HeightMin, 0.0001);
                float height = saturate((input.positionWS.y - _HeightMin) / span);
                half alpha = lerp(_BottomAlpha, _TopAlpha, (half)height);

                half4 result;
                result.rgb = half3(1.0, 1.0, 1.0);
                result.a = sprite.a * alpha * input.color.a;
                return result;
            }
            ENDHLSL
        }
    }
}
