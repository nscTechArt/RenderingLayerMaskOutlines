Shader "Hidden/Outline"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {} 
        
        _OutlineColor ("Outline Color", Color) = (0.4784313, 0.972549, 0.317647, 1)
        _OutlineWidth ("Outline Width", Float) = 2
        
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D _MainTex; SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;
            Texture2D _MaskRT; SamplerState sampler_MaskRT;

            float4 _OutlineColor;
            float _OutlineWidth;
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv :TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uvRoberts[5] :TEXCOORD1;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                float2 uv = IN.uv;
                OUT.uvRoberts[0] = uv + float2(-1, -1) * _MainTex_TexelSize * _OutlineWidth;
                OUT.uvRoberts[1] = uv + float2(1, -1) * _MainTex_TexelSize * _OutlineWidth;
                OUT.uvRoberts[2] = uv + float2(-1, 1) * _MainTex_TexelSize * _OutlineWidth;
                OUT.uvRoberts[3] = uv + float2(1, 1) * _MainTex_TexelSize * _OutlineWidth;
                OUT.uvRoberts[4] = uv;
                
                return OUT;
            }

            float Luminance(float3 color)
            {
                return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
            }

            float Roberts(Varyings varyings)
            {
                const float gx[4] ={-1, 0, 0, 1};
                const float gy[4] = {0, -1, 1, 0};
                float edgeX = 0, edgeY = 0;

                for (int i = 0; i < 4; i++)
                {
                    float4 color = _MaskRT.Sample(sampler_MaskRT, varyings.uvRoberts[i]);
                    float lum = Luminance(color);
                    edgeX += lum * gx[i];
                    edgeY += lum * gy[i];
                }
                return abs(edgeX) + abs(edgeY);
            }

            float4 frag(Varyings varyings) : SV_Target
            {
                float4 cameraColorRT = _MainTex.Sample(sampler_MainTex, varyings.uvRoberts[4]);
                
                float outline = Roberts(varyings);
                
                return outline * _OutlineColor + cameraColorRT;
            }
            ENDHLSL
        }
    }
}