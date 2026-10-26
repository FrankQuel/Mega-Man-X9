Shader "MegaManX9/DragonBossFlash"
{
 Properties
 {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _FlashColor ("Hit flash", Color) = (1,1,1,1)
  _FlashAmount ("Flash amount", Range(0,1)) = 0
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
  Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   Tags { "LightMode"="Universal2D" }
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   sampler2D _MainTex; fixed4 _Color, _FlashColor; float _FlashAmount;
   v2f vert(appdata input) { v2f output; output.vertex=UnityObjectToClipPos(input.vertex); output.uv=input.uv; output.color=input.color*_Color; return output; }
   fixed4 frag(v2f input):SV_Target { fixed4 color=tex2D(_MainTex,input.uv)*input.color; color.rgb=lerp(color.rgb,_FlashColor.rgb,saturate(_FlashAmount)); return color; }
   ENDCG
  }
 }
 Fallback "Sprites/Default"
}
