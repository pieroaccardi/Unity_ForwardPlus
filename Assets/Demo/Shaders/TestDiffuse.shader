Shader "ForwardPlus/TestDiffuse"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "Assets/ForwardPlus/Shaders/Common.cginc"

			Texture2D<uint2> g_lightsGrid;
			Buffer<uint> g_lightsIndexBuffer;
			StructuredBuffer<Light> g_lights;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 clipPosition : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				float3 normal : TEXCOORD3;
			};
						
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
				o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0));
				o.clipPosition = o.vertex;
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPosition = i.worldPosition;

				float3 N = normalize(i.normal);

				float2 texcoord = i.clipPosition.xy / i.clipPosition.w * 0.5 + 0.5;
				uint2 grid = g_lightsGrid[(uint2)(texcoord * _ScreenParams.xy / 16.0)];

				uint n = min(64, grid.y);

				float vv = grid.x;

				float3 lit = float3(0, 0, 0);

				for (int i = 0; i < n; ++i)
				{
					uint index = g_lightsIndexBuffer[grid.x + i];
					Light l = g_lights[index];
					float3 dist = l.PositionWorldSpace - worldPosition;
					float3 E = l.Color * (1.0 / (dot(dist, dist) * 100 / l.range / l.range) - 0.01);

					lit += max(0, E);
				}

				return float4(lit, 1);
			}
			ENDCG
		}
	}

		Fallback "Diffuse"
}
