Shader "UnitySimulationX/FloorGrid"
{
    Properties
    {
        _Spacing ("Cell Spacing", Float) = 1
        _MajorEvery ("Major Line Every", Float) = 5
        _Extent ("Half Extent", Float) = 20
        _LineWidth ("Line Width", Range(0.5, 4)) = 1.35
        _MinorColor ("Minor Color", Color) = (0.28, 0.28, 0.28, 0.45)
        _MajorColor ("Major Color", Color) = (0.42, 0.42, 0.42, 0.8)
        _AxisXColor ("Axis X Color", Color) = (0.86, 0.27, 0.27, 1)
        _AxisYColor ("Axis Y Color", Color) = (0.29, 0.78, 0.35, 1)
        _FadeStart ("Fade Start (0=center, 1=edge)", Range(0, 1)) = 0.35
        _FadeEnabled ("Distance Fade", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FloorGrid"
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Spacing;
                float _MajorEvery;
                float _Extent;
                float _LineWidth;
                half4 _MinorColor;
                half4 _MajorColor;
                half4 _AxisXColor;
                half4 _AxisYColor;
                float _FadeStart;
                float _FadeEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float LineStrength(float gridPos, float gridCoord)
            {
                return 1.0 - smoothstep(0.0, _LineWidth * fwidth(gridCoord), gridPos);
            }

            float IsMajor(float worldCoord)
            {
                float cell = round(worldCoord / _Spacing);
                return step(abs(fmod(cell, _MajorEvery)), 0.01);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 worldPos = input.positionWS;

                if (abs(worldPos.x) > _Extent || abs(worldPos.z) > _Extent)
                    discard;

                float2 gridCoord = worldPos.xz / _Spacing;
                float2 gridPos = abs(frac(gridCoord - 0.5) - 0.5);

                float xStrength = LineStrength(gridPos.x, gridCoord.x);
                float zStrength = LineStrength(gridPos.y, gridCoord.y);
                float lineMask = saturate(max(xStrength, zStrength));

                if (lineMask <= 0.001)
                    discard;

                float axisX = 1.0 - smoothstep(0.0, _LineWidth * fwidth(worldPos.z), abs(worldPos.z));
                float axisY = 1.0 - smoothstep(0.0, _LineWidth * fwidth(worldPos.x), abs(worldPos.x));

                float majorMask = saturate(max(IsMajor(worldPos.x) * xStrength, IsMajor(worldPos.z) * zStrength));

                half4 color = _MinorColor;
                color = lerp(color, _MajorColor, majorMask);

                if (axisX > 0.001)
                    color = lerp(color, _AxisXColor, axisX);

                if (axisY > 0.001)
                    color = lerp(color, _AxisYColor, axisY);

                float distFade = 1.0;
                if (_FadeEnabled > 0.5)
                {
                    float radial = length(worldPos.xz) / max(_Extent, 0.001);
                    distFade = 1.0 - smoothstep(_FadeStart, 1.0, radial);
                }

                color.a *= lineMask * distFade;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
