Shader "SimulCat/URP/Display from Phase CRT"
{
    Properties
    {
        _BaseMap ("CRT Texture", 2D) = "grey" {}

        _ShowReal("Show Real", float) = 0
        _ShowImaginary("Show Imaginary", float) = 0
        _ShowSquare("Show Square", float) = 0

        _ScaleAmplitude("Scale Amplitude", Range(1, 120)) = 50
        _ScaleEnergy("Scale Energy", Range(1, 120)) = 50
        _Brightness("Display Brightness", Range(0,2)) = 1

        _ColorNeg("Colour Base", color) = (0, 0.3, 1, 0)
        _Color("Colour Wave", color) = (1, 1, 0, 0)
        _ColorVel("Colour Velocity", color) = (0, 0.3, 1, 0)
        _ColorFlow("Colour Flow", color) = (1, 0.3, 0, 0)
        _Frequency("Frequency", float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline"
                "RenderType"="Transparent"
                "Queue" = "Transparent"}
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                        // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _BaseMap;

            float _ScaleAmplitude;
            float _ScaleEnergy;
            float _Brightness;

            float _ShowReal;
            float _ShowImaginary;
            float _ShowSquare;
            
            float4 _Color;
            float4 _ColorNeg;
            float4 _ColorVel;
            float4 _ColorFlow;
            float _Frequency;

            static const float Tau = 6.28318531f;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.
            float4 frag(Varyings IN) : SV_Target
            {
                bool displaySquare = round(_ShowSquare) > 0;
                bool displayReal = round(_ShowReal) > 0;
                bool displayIm = round(_ShowImaginary) > 0;     
                float4 col = _ColorNeg;
                if (!(displayReal || displayIm))
                {
                    return col;
                }

                // Defining the color variable and returning it.
                float4 sample = tex2D(_BaseMap, IN.uv);
                
                float2 phasor = float2(1,0);
                float amplitude = sample.z;
                float ampSq = sample.w;
                float value = 0;

                if (displayIm && displayReal)
                {
                    if (displaySquare)
                        value = sample.w * _ScaleEnergy * _ScaleEnergy;
                    else
                        value = sample.z * _ScaleAmplitude;
                    value *= _Brightness;
                    col = lerp(_ColorNeg, _ColorFlow, value);
                    col.a = displaySquare ? value+0.33 : clamp(value, .25,1);
                    return col;
                }
                                // To show wave movement, rotate phase vector, no need to recalculate pattern, this allows CRT to calculate once, then leave static;

                float tphi = (1 - frac(_Frequency * _Time.y)) * Tau;
                float sinPhi = sin(tphi);
                float cosPhi = cos(tphi);
                phasor.x = sample.x * cosPhi - sample.y * sinPhi;
                phasor.y = sample.x * sinPhi + sample.y * cosPhi;
                
                value = displayReal ? phasor.x : phasor.y;
                if (displaySquare)
                {
                    value *= _ScaleEnergy;
                    value *= value;
                }
                else
                    value *= _ScaleAmplitude;
                value *= _Brightness;
                col = lerp(_ColorNeg, displayReal ? _Color : _ColorVel, clamp(value,0.0,3.0));
                col.a = (displaySquare) ? value +0.3 : clamp(value + 1, 0.3, 1);

                return col;
            }
            ENDHLSL
        }
    }
}
