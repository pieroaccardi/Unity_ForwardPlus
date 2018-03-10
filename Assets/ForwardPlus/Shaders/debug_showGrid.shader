Shader "ForwardPlus/debug_showGrid"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_GridColor("GridColor", Color) = (0, 0, 0, 1)
		_Show("Show", Range(0, 1)) = 0.8
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D heatmap;

			//REVERTARE
			Texture2D<uint2> lightsGrid;

			float4 _GridColor;
			float _Show;

			fixed4 frag (v2f_img i) : SV_Target
			{
				float2 uv = i.uv.xy;
				uv.y = 1 - uv.y;
				uv *= _ScreenParams.xy / 16.0;

				float2 xy = abs(frac(uv) * 2 - 1);
				float VAL = 8;
				float reticolo = 1.0 - step(pow(pow(xy.x, VAL) + pow(xy.y, VAL), 1.0 / VAL), 0.93);
				reticolo *= 0.25;

				fixed4 col = tex2D(_MainTex, i.uv);

				uint2 grid = lightsGrid[(uint2)uv];

				float heat_uv_x = grid.y * 0.015625;

				float4 heat = tex2D(heatmap, float2(heat_uv_x, 0.5f));

				fixed4 ris = heat * (1.0 - reticolo) + _GridColor * reticolo;

				ris = col * (1 - _Show) + ris * _Show;

				ris.a = 1;

				return ris;
			}
			ENDCG
		}
	}
}
