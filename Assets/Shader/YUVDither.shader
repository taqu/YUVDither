Shader "Sprites/YUVDither"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
    }
    CGINCLUDE
    #include "UnityCG.cginc"

    static const half4 ConstantFloats = half4(0.0, 0.5, 0.75, 1.0);

    sampler2D _MainTex;
    uniform half4 _MainTex_TexelSize;

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        half4 uv : TEXCOORD0;
    };

    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);

#if UNITY_UV_STARTS_AT_TOP
        half2 uv = half2(v.uv.x, v.uv.y);
#else
        half2 uv = half2(v.uv.x, 1.0-v.uv.y);
        //half2 uv = half2(v.uv.x, v.uv.y);
#endif

        o.uv.xy = uv;
        o.uv.zw = uv * 0.5;
        return o;
    }

    half3 yuv2rgb(half y, half u, half v)
    {
        half3 rgb;
        rgb.r = y + 1.402*v;
        rgb.g = y - 0.344*u - 0.714*v;
        rgb.b = y + 1.772*u;
        return rgb;
    }

    half4 frag (v2f i) : SV_Target
    {
        half4 uv0 = i.uv.zyzy + ConstantFloats.xxyx;

        half2 ya = tex2D(_MainTex, i.uv.xy).gr;

        half u = tex2D(_MainTex, uv0.xy).b - 0.5;
        half v = tex2D(_MainTex, uv0.zw).b - 0.5;

        half4 c;
        c.rgb = yuv2rgb(ya.r, u, v);
        c.a = ya.g;
        return c;
    }
    ENDCG

    SubShader
    {
        Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
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
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            ENDCG
        }
    }
}
