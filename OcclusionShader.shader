// OcclusionShader.shader
// ─────────────────────────────────────────────────────────────────
// Custom URP Unlit shader for the AR video plane.
//
// How it works:
//   • The base texture (_MainTex) is the video player's RenderTexture.
//   • The mask texture (_OcclusionMask) is the binary segmentation output
//     from MobileSAM via SentisInferenceRunner (R8 RenderTexture).
//   • In the fragment shader, if the mask value for this pixel exceeds
//     _MaskThreshold, the pixel is DISCARDED via clip().
//   • A discarded pixel reveals the underlying AR camera feed (sky pass),
//     creating the illusion that the real person is in front of the video.
//
// Assign to the Material on the AR video plane prefab.
// ─────────────────────────────────────────────────────────────────

Shader "Custom/ARVideoOcclusion"
{
    Properties
    {
        _MainTex      ("Video Texture",     2D)    = "white" {}
        _OcclusionMask("Occlusion Mask",    2D)    = "black" {}
        _MaskThreshold("Mask Threshold",   Range(0,1)) = 0.05   // lower = more of hand captured
        _EdgeSoftness ("Edge Softness",    Range(0,0.2)) = 0.08 // higher = fills gaps in stencil

        // For the demo toggle: 0 = occlusion OFF, 1 = ON
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        // ── Render Queue ───────────────────────────────────────────────────
        // Geometry+1 so this renders AFTER the AR camera background pass.
        // We use clip() (fragment discard) NOT alpha blending — discarding a
        // fragment lets the already-rendered AR camera feed show through,
        // which is the correct way to achieve AR occlusion.
        Tags
        {
            "RenderType"      = "Opaque"
            "Queue"           = "Geometry+1"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ARVideoOcclusion"
            Tags { "LightMode" = "UniversalForward" }

            // No blending — we use clip() to fully discard person pixels.
            // This punches a clean hole through to the AR camera background.
            Blend Off
            ZWrite Off
            Cull Off  // Double-sided so the plane is visible from any angle.

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Textures & Samplers ────────────────────────────────────────
            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_OcclusionMask); SAMPLER(sampler_OcclusionMask);

            // ── Uniform Constants ──────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _MaskThreshold;
                float  _EdgeSoftness;
                float  _OcclusionStrength;
            CBUFFER_END

            // ── Vertex Input / Output ──────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;  // screen-space position for mask
            };

            // ── Vertex Shader ──────────────────────────────────────────────
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // Apply video texture tiling/offset.
                // Flip BOTH U and V to correct Unity Plane mesh UV orientation:
                //   - Flip V (Y): fixes vertical inversion on Android
                //   - Flip U (X): fixes horizontal mirror (text appears backwards otherwise)
                float2 rawUV = float2(1.0 - input.uv.x, 1.0 - input.uv.y);
                output.uv = TRANSFORM_TEX(rawUV, _MainTex);

                // Screen-space position — used in frag to compute mask UV that
                // matches the camera frame orientation (not video plane UV space).
                output.screenPos = ComputeScreenPos(output.positionHCS);

                return output;
            }

            // ── Fragment Shader ────────────────────────────────────────────
            half4 frag(Varyings input) : SV_Target
            {
                // Sample the video frame.
                half4 videoColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Compute screen-space UV for sampling the ARCore human stencil.
                // On Android, ARCore's humanStencilTexture is already in screen space
                // with origin top-left — no Y flip needed.
                float2 maskUV = input.screenPos.xy / input.screenPos.w;

                // Sample the binary segmentation mask (R channel only).
                float maskValue = SAMPLE_TEXTURE2D(_OcclusionMask,
                                                    sampler_OcclusionMask,
                                                    maskUV).r;

                // ── Soft edge computation ────────────────────────────────────
                // smoothstep gives a smooth transition at mask boundaries,
                // avoiding hard pixel-level aliasing at person silhouettes.
                float edgeFactor = smoothstep(
                    _MaskThreshold - _EdgeSoftness,
                    _MaskThreshold + _EdgeSoftness,
                    maskValue);

                // ── Apply occlusion via clip() ───────────────────────────────
                // clip() DISCARDS the fragment entirely — unlike alpha blending,
                // a discarded fragment reveals the AR camera background (already
                // rendered in the Background pass), NOT Unity's skybox/clear color.
                // This is the ONLY correct way to achieve AR pass-through occlusion.
                //
                // clip(x): if x < 0, the pixel is discarded.
                // So: when (edgeFactor * _OcclusionStrength - 0.5) < 0  → keep video
                //          when it's >= 0 → discard pixel (show real camera feed)
                float occlude = edgeFactor * _OcclusionStrength;
                clip(0.5 - occlude); // discard if occlude > 0.5 (person detected)

                return half4(videoColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
