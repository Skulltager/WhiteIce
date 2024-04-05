// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "WorldChunk"
{
	Properties
	{
		textureArray("Texture Array", 2DArray) = "white" {}
		baseLightStrength("Base Light Strength", Range(0.0, 1.0)) = 0.5
		uvScale("UV Scale", Range(0.0, 500.0)) = 1
	}
	SubShader
	{ 
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGINCLUDE
		UNITY_DECLARE_TEX2DARRAY(textureArray);

		StructuredBuffer<float> heightMap;
		StructuredBuffer<float> biomeMap;

		uniform float baseLightStrength;
		uniform StructuredBuffer<int> biomeTextureIndices;
		uniform uint heightMapWidth;
		uniform uint biomeMapStride;
		uniform uint biomes;
		uniform float uvScale;
		ENDCG

		Pass
		{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma require 2darray
			#include "UnityCG.cginc" 


			uniform float4 _LightColor0;
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 localVertex : POSITION1;
				float3 worldVertex : POSITION2;
				float3 normal : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			v2f vert(appdata v)
			{
				v2f output;
				output.worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
				output.localVertex = v.vertex.xyz;
				output.vertex = UnityObjectToClipPos(v.vertex);
				output.normal = v.normal;
				UNITY_TRANSFER_FOG(output, output.vertex);
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				uint leftIndex = (uint) input.localVertex.x;
				uint bottomIndex = (uint) input.localVertex.z;
				float leftFactor = input.worldVertex.x / uvScale;
				float bottomFactor = input.worldVertex.z / uvScale;

				uint i;
				float4 color = float4(0, 0, 0, 0);
				for (i = 0; i < biomes; i++)
				{
					uint heightMapIndex = leftIndex + bottomIndex * heightMapWidth;
					float biomeFactor = biomeMap[heightMapIndex + i * biomeMapStride];
					uint textureIndex = biomeTextureIndices[i];

					float3 textureArrayCoords = float3(leftFactor, bottomFactor, (float)textureIndex);
					color += UNITY_SAMPLE_TEX2DARRAY(textureArray, textureArrayCoords) * biomeFactor;
				}

				float worldLightFactor = max(0, dot(input.normal, _WorldSpaceLightPos0));
				float4 lightColor = _LightColor0 * worldLightFactor;
				color = color * baseLightStrength + color * lightColor * (1.0 - baseLightStrength);
				color.a = 1;
				UNITY_APPLY_FOG(input.fogCoord, color); 
				UNITY_OPAQUE_ALPHA(color.a);
				return color;
			}
			ENDCG
		}
		Pass 
		{
			Tags{ "LightMode" = "ForwardAdd"}
			Blend One One

			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag 
			#pragma require 2darray
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#include "AutoLight.cginc" 
			#include "UnityCG.cginc"

			uniform float4 _LightColor0;
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 localVertex : POSITION1;
				float3 worldVertex : POSITION2;
				float3 normal : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				LIGHTING_COORDS(2, 3)
			};

			v2f vert(appdata v)
			{
				v2f output;
				output.worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
				output.localVertex = v.vertex.xyz;
				output.vertex = UnityObjectToClipPos(v.vertex);
				output.normal = v.normal;
				TRANSFER_VERTEX_TO_FRAGMENT(output);
				UNITY_TRANSFER_FOG(output, output.vertex);
				return output;
			}

			float4 frag(v2f input) : SV_Target
			{ 
				uint leftIndex = (uint) input.localVertex.x;
				uint bottomIndex = (uint) input.localVertex.z;
				float leftFactor = input.worldVertex.x / uvScale;
				float bottomFactor = input.worldVertex.z / uvScale;

				uint i; 

				float4 color = float4(0, 0, 0, 0);
				for (i = 0; i < biomes; i++)
				{
					uint heightMapIndex = leftIndex + bottomIndex * heightMapWidth;
					float biomeFactor = biomeMap[heightMapIndex + i * biomeMapStride];
					uint textureIndex = biomeTextureIndices[i];

					float3 textureArrayCoords = float3(leftFactor, bottomFactor, (float)textureIndex);
					color += UNITY_SAMPLE_TEX2DARRAY(textureArray, textureArrayCoords) * biomeFactor;
				}
				float attenuation = (_WorldSpaceLightPos0.w > 0) * LIGHT_ATTENUATION(input) + (_WorldSpaceLightPos0.w == 0);
				float3 differenceToLightSource = _WorldSpaceLightPos0.xyz - input.worldVertex;
				float squaredDistance = dot(differenceToLightSource, differenceToLightSource);

				float lightFactor = (_WorldSpaceLightPos0.w > 0) * max(0, dot(input.normal, normalize(differenceToLightSource))) + (_WorldSpaceLightPos0.w == 0) * max(0, dot(input.normal, _WorldSpaceLightPos0));
				float4 lightColor = _LightColor0 * attenuation * lightFactor;

				color = color * lightColor * (1.0 - baseLightStrength);
				color.a = 1;

				UNITY_APPLY_FOG(input.fogCoord, color);
				return color;
			}

			ENDCG
		}
	}
	FallBack "Diffuse"
}