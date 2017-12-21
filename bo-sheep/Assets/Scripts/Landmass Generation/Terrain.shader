Shader "Custom/Terrain" {
	Properties {
		// ECF episode 17 getting set to start texturing the mountains
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		// This is not actually C#, its CG which is derived from some shader
		// language... or something
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4; // Who knew that epsilon is usually used to represent a very small value

		int layerCount;
		float3 baseColours[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseColourStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		float minHeight;
		float maxHeight;

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		float3 triPlanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			// For the time being, overwrite what we've done in the loop above
			// Project the texture straight down along the y axis.  Tests have shown
			// us that this looks fine when the ground is flat but shit on the side
			// of mountains
			//o.Albedo = tex2D(testTexture, IN.worldPos.xz / testScale);

			// Instead of projecting down along y, we'll use a common texturing technique
			// called triplanar mapping.  This projects along the normals of the mesh
			float3 scaledWorldPos = worldPos / scale;

			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		// Called for every visible pixel, every frame (no really!)
		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			// Make it so that blendAxes.x, .y and .z add up to 1
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < layerCount; i++) {
				// epsilon is used to avoid divide by zero issues in the inverseLerp function
				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);

				float3 baseColour = baseColours[i] * baseColourStrength[i];
				float3 textureColour = triPlanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColourStrength[i]);

				o.Albedo = o.Albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
