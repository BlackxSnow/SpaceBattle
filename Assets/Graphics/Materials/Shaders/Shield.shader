Shader "Custom/Shield" {
	Properties
	{
		_Threshold("Distance Threshold", Float) = 2
		_Color("Main Color", Color) = (1,.5,.5,1)
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		ZWrite off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull off
		LOD 100

		Pass
		{
			HLSLPROGRAM

#pragma vertex UnlitPassVertex
#pragma fragment UnlitPassFragment

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float _Threshold;
			half4 _Color;
			
			int PointCount;
			float4 Points[128];
			float Strengths[128];

			struct VertIn {
				float4 pos : POSITION;
			};
			struct VertOut {
				float4 wPos : WPOSITION;
				float4 cPos : SV_POSITION;
			};

			VertOut UnlitPassVertex(VertIn input) 
			{
				VertOut output;
				float4 worldPos = mul(unity_ObjectToWorld, float4(input.pos.xyz, 1.0));
				output.cPos = mul(unity_MatrixVP, worldPos);
				output.wPos = worldPos;
				return output;
			}

			float4 UnlitPassFragment(VertOut input) : SV_TARGET
			{
				float4 col = float4(0,0,0,0);
				float intensity = 1;
				for (int i = 0; i < PointCount; i++) 
				{
					float distance = length(Points[i] - input.wPos);
					float multiplier = max(Strengths[i] + _Threshold - distance, 0);
					intensity += 50 / pow(distance, 4);
				}
				return float4(_Color.rgb * intensity, _Color.a);
			}

			ENDHLSL
		}
	}
}