Shader "Hidden/WorldFog"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset] _SkyCubemap("-", Cube) = "" {}
	}
    SubShader
    {
		CGINCLUDE

		sampler2D _CameraDepthTexture;
		sampler2D _MainTex;
		samplerCUBE _SkyCubemap;
		half4 _SkyCubemap_HDR;

		uniform float fogStartDistance;
		uniform float fogTotalDistance;
		uniform float fogExponentFactor;

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint vertexID : SV_VertexID;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 direction : POSITION1;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f output;

				float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				output.direction = mul(unity_CameraToWorld, float4(viewVector, 0));
				output.vertex = UnityObjectToClipPos(v.vertex);
				output.uv = v.uv;
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				half4 skyData = texCUBE(_SkyCubemap, normalize(input.direction));
				half3 skyColor = DecodeHDR(skyData, _SkyCubemap_HDR);
				float4 sceneColor = tex2D(_MainTex, input.uv);
				float zSample = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv)) * length(input.direction);
				float fogFactor = pow(clamp((zSample - fogStartDistance) / fogTotalDistance, 0, 1), fogExponentFactor);
				return sceneColor * (1 - fogFactor) + float4(skyColor.r, skyColor.g, skyColor.b, 1) * fogFactor;
			}
			ENDCG
		}
    }
}
