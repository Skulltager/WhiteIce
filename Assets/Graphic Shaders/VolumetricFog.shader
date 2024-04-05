Shader "Hidden/VolumetricFog"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		CGINCLUDE

		sampler2D _CameraDepthTexture;
		sampler2D _MainTex;

		Texture3D<float4> noiseCube;
		sampler3D worleyNoise;
		float3 boundsMin;
		float3 boundsMax;

		float sampleStepSize;

		float4 sampleScaleX;
		float4 sampleScaleY;
		float4 sampleScaleZ;
		float4 sampleMultiplier;
		float4 sampleThreshold;
		float4 sampleEdgeVerticalCutoff;
		float4 sampleEdgeHorizontalCutoff;
		float4 sampleWeights;

		float maxFogDensity;

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
				float4 vertex : SV_POSITION0;
				float3 direction : POSITION1;
				float2 uv : TEXCOORD0;
			};

			float RayCastBounds(float3 rayOrigin, float3 rayDirection, float cameraDepth)
			{
				float3 t0 = (boundsMin - rayOrigin) / rayDirection;
				float3 t1 = (boundsMax - rayOrigin) / rayDirection;

				float3 tMin = min(t0, t1);
				float3 tMax = max(t0, t1);

				float entryDistance = max(max(tMin.x, tMin.y), tMin.z);
				float exitDistance = min(min(tMax.x, tMax.y), tMax.z);

				float distanceToBox = max(0, entryDistance);
				float distanceInsideBox = max(0, exitDistance - distanceToBox);
				distanceInsideBox = min(cameraDepth - distanceToBox, distanceInsideBox);
				if (distanceInsideBox <= 0 || distanceToBox  > cameraDepth)
					return 0;


				float startSampleDistance = fmod(distanceInsideBox, sampleStepSize * 2) / 2;
				int totalStepCount = 2 + floor(distanceInsideBox / sampleStepSize / 2) * 2;
				float3 boxStartPosition = rayOrigin + rayDirection * distanceToBox;
				float fogAmount = 0;
				float distanceSpend = 0;
				int i = 0;
				
				while(fogAmount < maxFogDensity * 10 && i < totalStepCount && i < 150)
				{
					float currentStepSize = (i == 0 || i == totalStepCount - 1) ? startSampleDistance : sampleStepSize;
					float multiplier = currentStepSize / sampleStepSize;

					float3 sampleWorldPosition = boxStartPosition + (distanceSpend + currentStepSize / 2) * rayDirection;
					float3 distanceToEdge = min(sampleWorldPosition - boundsMin, boundsMax - sampleWorldPosition);

					float colorSampleValue = 0;
					for (int j = 0; j < 4; j++)
					{
						multiplier *= min(1, distanceToEdge.x / sampleEdgeHorizontalCutoff[j]);
						multiplier *= min(1, distanceToEdge.y / sampleEdgeVerticalCutoff[j]);
						multiplier *= min(1, distanceToEdge.z / sampleEdgeHorizontalCutoff[j]);
						float3 samplePosition = sampleWorldPosition / float3(sampleScaleX[j], sampleScaleY[j], sampleScaleZ[j]);
						float color = tex3D(worleyNoise, samplePosition)[j];
						colorSampleValue += max(0, (color - sampleThreshold[j])) / (1 - sampleThreshold[j]) * sampleMultiplier[j] * multiplier;
					}
					fogAmount += max(0, (colorSampleValue - sampleThreshold[3]));
					distanceSpend += currentStepSize;
					i++;
				}

				return clamp(1 - exp(-fogAmount / maxFogDensity), 0, 1);
			}

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
				float zSample = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv)) * length(input.direction);
				float fogAmount = RayCastBounds(_WorldSpaceCameraPos, normalize(input.direction), zSample);
				
				return lerp(tex2D(_MainTex, input.uv), float4(1,1,1,1), fogAmount) ;

				//boxCast.y = min(zSample - boxCast.x, boxCast.y);
				//return float4(1, 1, 1, 1);
				
				//float4 color = (zSample > boxCast.x && boxCast.y > 0) * float4(0, 0, 0, 1);
				//color += (zSample <= boxCast.x || boxCast.y == 0) * tex2D(_MainTex, input.uv);
				//return color;
			}
			ENDCG
		}
	}
}
