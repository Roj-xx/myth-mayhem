Shader "SvKnightDev/Skybox/Panoramic 360 XYZ Rotation Zoom Stretch Crossfade"
{
    Properties
    {
        [Header(Panorama Textures)]

        [NoScaleOffset]
        _MainTex ("Current Panorama (HDR)", 2D) = "grey" {}

        [NoScaleOffset]
        _NextTex ("Next Panorama (HDR)", 2D) = "grey" {}

        _Blend ("Crossfade", Range(0, 1)) = 0


        [Header(Color Adjustment)]

        [HDR]
        _Tint ("Tint", Color) = (1, 1, 1, 1)

        [Gamma]
        _Exposure ("Exposure", Range(0, 8)) = 1


        [Header(Panorama Rotation)]

        _RotationX ("Rotation X / Pitch", Range(-180, 180)) = 0
        _RotationY ("Rotation Y / Yaw", Range(-180, 180)) = 0
        _RotationZ ("Rotation Z / Roll", Range(-180, 180)) = 0


        [Header(Panorama Zoom)]

        _Zoom ("Zoom", Range(0.25, 4.0)) = 1.0


        [Header(Panorama Stretch)]

        _StretchX ("Stretch X", Range(0.25, 4.0)) = 1.0
        _StretchY ("Stretch Y", Range(0.25, 4.0)) = 1.0
        _StretchZ ("Stretch Z", Range(0.25, 4.0)) = 1.0


        [HideInInspector]
        _MainTex_HDR ("Current HDR Decode Data", Vector) = (1, 1, 0, 0)

        [HideInInspector]
        _NextTex_HDR ("Next HDR Decode Data", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NextTex;

            half4 _MainTex_HDR;
            half4 _NextTex_HDR;

            half4 _Tint;
            half _Exposure;
            half _Blend;

            float _RotationX;
            float _RotationY;
            float _RotationZ;

            float _Zoom;

            float _StretchX;
            float _StretchY;
            float _StretchZ;

            struct AppData
            {
                float4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexToFragment
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 RotateAroundX(
                float3 direction,
                float degrees
            )
            {
                float angle = radians(degrees);

                float sine;
                float cosine;

                sincos(angle, sine, cosine);

                return float3
                (
                    direction.x,

                    cosine * direction.y -
                    sine * direction.z,

                    sine * direction.y +
                    cosine * direction.z
                );
            }

            float3 RotateAroundY(
                float3 direction,
                float degrees
            )
            {
                float angle = radians(degrees);

                float sine;
                float cosine;

                sincos(angle, sine, cosine);

                return float3
                (
                    cosine * direction.x -
                    sine * direction.z,

                    direction.y,

                    sine * direction.x +
                    cosine * direction.z
                );
            }

            float3 RotateAroundZ(
                float3 direction,
                float degrees
            )
            {
                float angle = radians(degrees);

                float sine;
                float cosine;

                sincos(angle, sine, cosine);

                return float3
                (
                    cosine * direction.x -
                    sine * direction.y,

                    sine * direction.x +
                    cosine * direction.y,

                    direction.z
                );
            }

            float3 RotatePanorama(float3 direction)
            {
                /*
                 * The inverse rotation is applied to the texture
                 * sampling direction.
                 *
                 * Reverse rotation order:
                 * Z, Y, then X.
                 */

                direction = RotateAroundZ(
                    direction,
                    -_RotationZ
                );

                direction = RotateAroundY(
                    direction,
                    -_RotationY
                );

                direction = RotateAroundX(
                    direction,
                    -_RotationX
                );

                return direction;
            }

            float3 ApplyZoomAndStretch(float3 direction)
            {
                float zoom = max(
                    _Zoom,
                    0.001
                );

                float3 stretch = max
                (
                    float3
                    (
                        _StretchX,
                        _StretchY,
                        _StretchZ
                    ),

                    float3
                    (
                        0.001,
                        0.001,
                        0.001
                    )
                );

                /*
                 * Values greater than 1 zoom in.
                 * Values below 1 zoom out.
                 */

                direction.x /= zoom;
                direction.y /= zoom;

                /*
                 * Apply individual XYZ stretching.
                 */

                direction /= stretch;

                return normalize(direction);
            }

            float2 DirectionToEquirectangularUV(
                float3 direction
            )
            {
                direction = normalize(direction);

                direction.y = clamp(
                    direction.y,
                    -1.0,
                    1.0
                );

                float longitude = atan2(
                    direction.z,
                    direction.x
                );

                float latitude = acos(
                    direction.y
                );

                float2 sphericalCoordinates;

                sphericalCoordinates.x =
                    longitude * (0.5 / UNITY_PI);

                sphericalCoordinates.y =
                    latitude * (1.0 / UNITY_PI);

                float2 uv =
                    float2(0.5, 1.0) -
                    sphericalCoordinates;

                /*
                 * Repeat horizontally because the panorama
                 * represents 360 degrees.
                 */

                uv.x = frac(uv.x);

                /*
                 * Prevent vertical sampling outside the panorama.
                 */

                uv.y = saturate(uv.y);

                return uv;
            }

            VertexToFragment Vert(AppData input)
            {
                VertexToFragment output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_INITIALIZE_OUTPUT(
                    VertexToFragment,
                    output
                );

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(
                    output
                );

                output.position =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.direction =
                    RotatePanorama(
                        input.vertex.xyz
                    );

                return output;
            }

            half4 Frag(VertexToFragment input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(
                    input
                );

                float3 sampleDirection =
                    ApplyZoomAndStretch(
                        input.direction
                    );

                float2 uv =
                    DirectionToEquirectangularUV(
                        sampleDirection
                    );

                half4 currentEncodedColor =
                    tex2D(
                        _MainTex,
                        uv
                    );

                half4 nextEncodedColor =
                    tex2D(
                        _NextTex,
                        uv
                    );

                half3 currentColor =
                    DecodeHDR(
                        currentEncodedColor,
                        _MainTex_HDR
                    );

                half3 nextColor =
                    DecodeHDR(
                        nextEncodedColor,
                        _NextTex_HDR
                    );

                half blendAmount =
                    saturate(_Blend);

                half3 finalColor =
                    lerp(
                        currentColor,
                        nextColor,
                        blendAmount
                    );

                finalColor *= _Tint.rgb;
                finalColor *= _Exposure;

                return half4(
                    finalColor,
                    1.0
                );
            }

            ENDCG
        }
    }

    Fallback Off
}