// UI/Default에 눈알 회전을 더한 셰이더.
// 정면을 보는 눈알 그림을 구로 보고, 시선만큼 돌린 구 표면을 다시 그림에서 찾아 그린다.
// 실루엣(알파)은 원래 자리에서 읽으므로 눈알 외곽선과 Mask 모양은 그대로다.
Shader "Hyunwoo/UI/Eyeball Gaze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _EyeSphere ("Eye Sphere (center UV xy, radius UV zw)", Vector) = (0.5, 0.5, 0.5, 0.5)
        _Gaze ("Gaze (yaw, pitch radians)", Vector) = (0, 0, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask          : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;
            float4 _EyeSphere;
            float4 _Gaze;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if (!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            // 화면에 보이는 구 표면 한 점이 돌기 전에 그림의 어디에 있었는지 찾는다.
            float2 EyeballUV(float2 uv)
            {
                float2 p = (uv - _EyeSphere.xy) / _EyeSphere.zw;
                float r2 = dot(p, p);
                float3 n = float3(p, sqrt(saturate(1.0 - r2)));

                float cy = cos(_Gaze.x), sy = sin(_Gaze.x);
                float cp = cos(_Gaze.y), sp = sin(_Gaze.y);
                // 좌우 회전(양수면 오른쪽을 본다)을 먼저 되돌린다.
                float3 a = float3(n.x * cy - n.z * sy, n.y, n.x * sy + n.z * cy);
                // 위아래 회전(양수면 위를 본다)을 되돌린다.
                float3 s = float3(a.x, a.y * cp - a.z * sp, a.y * sp + a.z * cp);

                // 뒤쪽 반구에서 넘어온 부분은 테두리 색을 쓰고, 테두리의 반투명 가장자리는 피한다.
                float len = max(length(s.xy), 1e-4);
                float2 q = s.z < 0.0 ? s.xy / len : s.xy;
                q *= min(1.0, 0.97 / len);
                float2 turned = _EyeSphere.xy + q * _EyeSphere.zw;
                return r2 < 1.0 ? turned : uv;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                half4 texel = tex2D(_MainTex, EyeballUV(IN.texcoord));
                texel.a = tex2D(_MainTex, IN.texcoord).a;
                half4 color = IN.color * (texel + _TextureSampleAdd);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
}
