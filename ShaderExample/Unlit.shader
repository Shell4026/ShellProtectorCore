Shader "Protector/unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MipTex("MipReference", 2D) = "white" { }
        _EncryptTex("Encrypted", 2D) = "white" { }

        _Key0("key0", float) = 0
        _Key1("key1", float) = 0
        _Key2("key2", float) = 0
        _Key3("key3", float) = 0
        _Key4("key4", float) = 0
        _Key5("key5", float) = 0
        _Key6("key6", float) = 0
        _Key7("key7", float) = 0
        _Key8("key8", float) = 0
        _Key9("key9", float) = 0
        _Key10("key10", float) = 0
        _Key11("key11", float) = 0
        _Key12("key12", float) = 0
        _Key13("key13", float) = 0
        _Key14("key14", float) = 0
        _Key15("key15", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float4 local_pos : TEXCOORD1;
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            UNITY_DECLARE_TEX2D(_MipTex);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_EncryptTex);

            float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.local_pos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
			
            #include "Decrypt.cginc"
            float4 frag(v2f i) : SV_Target
            {
                float2 mainUV = i.uv;
                float4 mip_texture = _MipTex.Sample(sampler_MipTex, mainUV);

                int mip = round(mip_texture.r * 255 / 10);
                int m[13] = { 0, 0, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                float2 uv_unit = _MainTex_TexelSize.xy;
                float2 uv_bilinear = mainUV - 0.5 * uv_unit;

                float4 c00 = DecryptTextureXXTEA(uv_bilinear + float2(uv_unit.x * 0, uv_unit.y * 0), m[mip]);
                float4 c10 = DecryptTextureXXTEA(uv_bilinear + float2(uv_unit.x * 1, uv_unit.y * 0), m[mip]);
                float4 c01 = DecryptTextureXXTEA(uv_bilinear + float2(uv_unit.x * 0, uv_unit.y * 1), m[mip]);
                float4 c11 = DecryptTextureXXTEA(uv_bilinear + float2(uv_unit.x * 1, uv_unit.y * 1), m[mip]);

                float2 f = frac(uv_bilinear * _MainTex_TexelSize.zw);

                float4 c0 = lerp(c00, c10, f.x);
                float4 c1 = lerp(c01, c11, f.x);

                float4 bilinear = lerp(c0, c1, f.y);

                float4 mainTexture = c00;
                return mainTexture;
            }
            ENDCG
        }
    }
}
