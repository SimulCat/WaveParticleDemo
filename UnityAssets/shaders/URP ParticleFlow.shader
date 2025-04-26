Shader "Universal Render Pipeline/Quantum/Particle Scattering"
{
    /* This shader is a URP version of the Particle Scattering shader. It is used to render the quantum scattering particles in the scene. 
    */
    Properties
    {
        _BaseMap ("Particle Texture", 2D) = "white" {}
        _Color("Particle Colour", color) = (1, 1, 1, 1)
        _Visibility("Visibility",Range(0.0,1.0)) = 1.0
        _MomentumMap("Momentum Map", 2D ) = "black" {}
        _MapMaxP("Map max momentum", float ) = 1

        _SlitCount("Num Sources",float) = 2
        _SlitPitch("Slit Pitch",float) = 0.3
        _SlitWidth("Slit Width", float) = 0.05
        _BeamWidth("Beam Width", float) = 1
        _GratingOffset("Grating X Offset", float) = 0

        _ParticleP("Particle Momentum", float) = 1
        _MaxVelocity("MaxVelocity", float) = 5
        _SpeedRange("Speed Range fraction",Range(0.0,0.5)) = 0
        _PulseWidth("Pulse Width",float) = 0
        _PulseWidthMax("Max Pulse Width",float) = 1.5
        // Particle Decal Array
        _ArraySpacing("Array Spacing", Vector) = (0.1,0.1,0.1,0)
        // x,y,z count of array w= total.
        _ArrayDimension("Array Dimension", Vector) = (128,80,1,10240)
        _MarkerScale ("Marker Scale", Range(0.01,10)) = 1
        _Scale("Scale Demo",Float) = 1
        _MaxScale("Scale Max",Float) = 5
        // Play Control
        _BaseTime("Base Time Offset", Float)= 0
        _PauseTime("Freeze time",Float) = 0
        _Play("Play Animation", Float) = 1

    }

    SubShader
    {
        Tags {  "Queue"="Transparent" 
                "RenderType"="Transparent" 
                "RenderPipeline"="UniversalPipeline"}

        Blend One One
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                uint id             : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _Color;
            float _Visibility;

            sampler2D _MomentumMap;
          
            float _SlitCount;
            float _SlitPitch;
            float _SlitWidth;
            float _BeamWidth;
            float _GratingOffset;
            float _ParticleP;

            float _MapMaxP;
            float _MaxVelocity;
            float _PulseWidth;
            float _PulseWidthMax;
            float _SpeedRange;
            float _MapSum;

            float4 _ArraySpacing;
            float4 _ArrayDimension;
            float _MarkerScale;
            float _Scale;
            float _MaxScale;

            float _BaseTime;
            float _PauseTime;
            float _Play;

            static const float Tau = 6.28318531f;

            static const float TwoDivPi = 0.636619772367f;
            // 4/Pi
            static const float InvPi =  0.318309886183f;

            CBUFFER_END
            #define M(U) tex2Dlod(_MomentumMap, float4(U))

            float3 sampleMomentum(float incidentP,float rnd01)
            {
                float fracMax = incidentP/_MapMaxP;
                float4 mapMax = M(float4(fracMax,0.5,0,0));
                float lookUp = mapMax.y*abs(rnd01);
                float4 sample = M(float4(lookUp,0.5,0,0));
                float py = sample.z*sign(rnd01);
                int isValid = py < incidentP;
                float sinTheta = clamp(py/incidentP,-1.0,1.0);
                float cosTheta = cos(asin(sinTheta));
                return float3(cosTheta,sinTheta,isValid);
            }
            /*

            Description:
	            pcg hash function for when all you need is basic integer randomization, not time/spatially structured noise as in snoise.
	            from article by Nathan Reed
	            https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/
                and source paper
                https://jcgt.org/published/0009/03/02/
            */
            uint pcg_hash(uint input)
            {
                uint state = input * 747796405u + 2891336453u;
                uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
                return (word >> 22u) ^ word;
            }

            // Hash float from zero to max
            float RandomRange(float rangeMax, uint next)
            {
                float div;
                uint hsh;
                hsh = pcg_hash(next) & 0x7FFFFF;
                div = 0x7FFFFF;
                return rangeMax * ((float) hsh / div);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                ZERO_INITIALIZE(Varyings,OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                // Calculate the object space offset of the triangle centre from the vertex position
                float3 centerOffset;
                switch(IN.id%3) // Corner is vertex ID % 3
                {
                    case 2:
                        centerOffset = float3(0,0.57735027,0); 
                        break;
                    case 1:
                        centerOffset = float3(-.5,-0.288675135,0);
                        break;
                    default:
                        centerOffset = float3(0.5,-0.288675135,0);
                        break;
                }
                float3 vertexOffset = centerOffset*_ArraySpacing.xyz;
                float3 triCentreInMesh = IN.positionOS.xyz - vertexOffset;

                float2 localGridCentre = ((_ArrayDimension.xy - float2(1,1)) * _ArraySpacing.xy);
                float maxDiagonalDistance = length(localGridCentre);
                localGridCentre *= 0.5; // Align centre

                float markerScale =  _MarkerScale/_Scale;
                // Initialize velocity to average velocity
                float particleVelocity = _MaxVelocity*(_ParticleP/_MapMaxP);

                // Apply scale to the aperture positions and dimensions
                int slitCount = max(round(_SlitCount),1);
                float slitPitchScaled = _SlitPitch/_Scale;
                float slitWidthScaled = _SlitWidth/_Scale;
                float gratingWidthScaled = (slitCount-1)*slitPitchScaled + slitWidthScaled;
                float beamWidth = max (_BeamWidth/_Scale,(gratingWidthScaled + slitWidthScaled));
                // Set slitCenter to left-most position
                float leftSlitCenter = -(slitCount - 1)*slitPitchScaled*0.5;


                // Hash the triangle ID to randomize simulation based on vertex ID
                uint idHash = pcg_hash(IN.id/3);
                float hsh01 = (float)(idHash & 0x7FFFFF);
                float div = 0x7FFFFF;
                hsh01 = (hsh01/div);
                float hshPlusMinus = (hsh01*2.0)-1.0;
                // Also hash for particle speed and start position
                float startHash = RandomRange(1.0,idHash ^ 0xAC3FFF)-0.5;
                float speedHash = RandomRange(2.0,idHash >> 3)-1.0;

                // 'Randomly' assign the particle to an aperture
                int nSlit = (idHash >> 8) % slitCount;
                float slitCenter = leftSlitCenter + (nSlit * slitPitchScaled);
                float leftEdge = leftSlitCenter - slitWidthScaled*0.5;

                float startPosY =  (_GratingOffset > 0.00001) ? (beamWidth * startHash) : slitCenter + (startHash * slitWidthScaled);
                float normPos = frac((startPosY-leftEdge)/slitPitchScaled)*slitPitchScaled;
                // check if particle y pos is valid;
                bool validPosY = (startPosY >= leftEdge) && (startPosY <= (-leftEdge)) && (normPos <= slitWidthScaled);

                // Now particle scattering and position
                int hasPulse = (int)(_PulseWidth > 0);
                int continuous = (int)hasPulse == 0;
                float pulseDuration = hasPulse * _PulseWidth;
                float pulseMax = hasPulse * _PulseWidthMax;
                float voffset = 1 + (_SpeedRange * InvPi * asin(speedHash));
                float vScale = particleVelocity/_Scale;
                float cyclePeriod = (maxDiagonalDistance/vScale) + pulseMax;
                
                // Divide time by period to get fraction of the cycle.
                float cycles = ((_Play * _Time.y + (1-_Play)*_PauseTime)-_BaseTime)/cyclePeriod;
                float cycleTime = frac(cycles + continuous*hsh01)*cyclePeriod - pulseMax;
                float timeOffset =  pulseDuration * InvPi * asin(hshPlusMinus);
                float trackDistance = (cycleTime + timeOffset)*vScale*voffset;
                float gratingDistance = _GratingOffset/_Scale;
                float postGratingDist = max(0.0,trackDistance-gratingDistance);
                float preGratingDist = min(gratingDistance,trackDistance);

                // Calculate the particle position based on the time and velocity
                float2 startPos = float2(preGratingDist-(localGridCentre.x),startPosY);
                float momentumHash = RandomRange(2, idHash);
                float3 sample = sampleMomentum(_ParticleP*voffset,momentumHash-1.0);
                float2 particlePos = startPos + sample.xy*postGratingDist;
                validPosY = validPosY || (trackDistance <= gratingDistance);
                int  posIsInside = (int)(validPosY)*floor(sample.z)*int((abs(particlePos.x) < localGridCentre.x) && (abs(particlePos.y) <= localGridCentre.y));
               
                // Check inside bounding box
                particlePos = posIsInside*particlePos + (1-posIsInside)*triCentreInMesh.xy;
                // Now got a new position for the triangle in the model
                float3 triCentreInModel = float3 (particlePos,0);


                triCentreInModel = posIsInside * triCentreInModel + (1-posIsInside)*triCentreInMesh; 
                vertexOffset *= markerScale;                    // Scale the quad corner offset to world, now we billboard

                IN.positionOS.xyz=triCentreInModel+vertexOffset;

                // billboard the triangle
                float4 camModelCentre = float4(triCentreInModel,1.0);
                float4 camVertexOffset = float4(vertexOffset,0.0);

                // Billboarding in GLSL was Three steps in one line
                //      1) Inner step is to use UNITY_MATRIX_MV to get the camera-oriented coordinate of the centre of the billboard.
                //         Here, the xy coords of the billboarded vertex are always aligned to the camera XY so...
                //      2) Just add the scaled xy model offset to lock the vertex orientation to the camera view.
                //      3) Transform the result by the Projection matrix (UNITY_MATRIX_P) and we now have the billboarded vertex in clip space.

                OUT.positionHCS = mul(UNITY_MATRIX_P,(mul(UNITY_MATRIX_MV, camModelCentre) + camVertexOffset));
                
                //Standard code
                //OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = float4(_Color.rgb,-.5 + posIsInside * 1.5);

                OUT.uv = TRANSFORM_TEX(IN.uv,_BaseMap);
                // Returning the output.
                return OUT;
            }


            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap, IN.uv);
                col.rgb *= IN.color.rgb;
                col.a = IN.color.a;
                if(col.a < 0)
                {
					clip(-1);
					col = 0;
				}
                col *= _Visibility;
                return col;
            }
            ENDHLSL
        }
    }   
}
 
