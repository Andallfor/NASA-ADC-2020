﻿Shader "Unlit Vertex Color"
{ // from https://gamedev.stackexchange.com/questions/168342/programmatically-generated-mesh-vertex-colors-not-showing-up-in-unity
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Tags { "LightMode" = "ForwardBase" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "AutoLight.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                LIGHTING_COORDS(0,1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Brightness;
            fixed _Gamma;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.color.w = 1.0;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= i.color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                float atten = LIGHT_ATTENUATION(i);
                return col;
            }
            ENDCG
        }
    }
}