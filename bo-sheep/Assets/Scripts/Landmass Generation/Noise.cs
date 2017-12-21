using UnityEngine;

public static class Noise {

	public enum NormalizeMode { Local, Global }

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre) {
		float[,] noiseMap = new float[mapWidth, mapHeight];

		// Using this random number generator, each octave will be generated from a different
		// location
		System.Random pseudoRandomNumberGenerator = new System.Random (settings.seed);
		Vector2[] octaveOffsets = new Vector2[settings.octaveCount];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < settings.octaveCount; i++) {
			// Testing has apparently shown that using offsets larger than the ones used here
			// mean the PerlinNoise function start returning lots of repeating values
			float offsetX = pseudoRandomNumberGenerator.Next (-100000, 100000) + settings.userOffset.x + sampleCentre.x;
			float offsetY = pseudoRandomNumberGenerator.Next (-100000, 100000) - settings.userOffset.y - sampleCentre.y;

			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.persistance;
		}

		// Inside the innermost loop below, we translate our PerlinNoise values from a 0 to 1
		// range to a -1 to +1 range.  But the map we return needs to have values in the
		// range 0 to 1.  To enable the re-translation back to that range we'll keep track of
		// the min and max noise height values we add to the map as we loop
		//
		// Note: I tried removing the translation to a -1 to +1 range and ended up with similar
		// noise pattern but quite white (higher values of noise) and I guess this is because
		// of the multiple octaves adding to the noise value and pushing it up to and slightly over 1
		// so even then the normalisation back to the range 0 to 1 is necessary
		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		// We'll use these midpoint values when getting our sampleX and Y below, so that when we
		// change the scale value in the Unity editor, the map zooms at its centre and not towards
		// the point in the top right
		float midPointX = mapWidth / 2f;
		float midPointY = mapHeight / 2f;

		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				// For each point on the map, loop octaveCount times to apply each level of
				// reducing influence on the noiseHeight value for that point
				for (int i = 0; i < settings.octaveCount; i++) {
					// So here we're calculating where in the Perlin noise function space to get
					// the noise value for the current noise map point
					// See comment above re the midPointX and midPointY values
					float sampleX = (x - midPointX + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y - midPointY + octaveOffsets[i].y) / settings.scale * frequency;

					// By default the values from PerlinNoise() are in the range 0 to 1, but for
					// more interesting noise translate the value to be in the range -1 to 1.  Note
					// that we normalise the height values below so we won't end up with negative
					// heights
					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;

					noiseHeight += perlinValue * amplitude;

					// Persistence is in the range 0 to 1, so amplitude reduces each successive octave
					amplitude *= settings.persistance;

					// Lacunarity is > 1 so frequence increases with each successive octave
					frequency *= settings.lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				}

				if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}

				noiseMap [x, y] = noiseHeight;

				if (settings.normalizeMode == NormalizeMode.Global) {
					float normalizedHeight = (noiseMap [x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap [x, y] = Mathf.Clamp (normalizedHeight, 0, int.MaxValue);
				}
			}
		}

		// Use the min and max noise height values recorded from the above loops to translated
		// all the noise heights in the map back into the range 0 to 1, using the InverseLerp
		//function
		if (settings.normalizeMode == NormalizeMode.Local) {
			for (int y = 0; y < mapHeight; y++) {
				for (int x = 0; x < mapWidth; x++) {
				
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				}
			}
		}

		return noiseMap;
	}
}

[System.Serializable]
public class NoiseSettings {
	
	public Noise.NormalizeMode normalizeMode;
	public float scale = 50;
	public int octaveCount = 6;
	// Range attribute makes this a slider in the Unity editor
	[Range(0,1)]
	public float persistance = 0.6f;
	public float lacunarity = 2.0f;
	public int seed;
	public Vector2 userOffset;

	public void Validate() {
		// We're taking a scale at least partly because the Perlin noise function retains the same
		// Y when X is a whole integer value.  With the scale being used to divide x and y above,
		// we shouldn't get any(many?) whole integers
		scale = Mathf.Max (scale, 0.01f);
		octaveCount = Mathf.Max (octaveCount, 1);
		lacunarity = Mathf.Max (lacunarity, 1);
		persistance = Mathf.Clamp01 (persistance);
	}

}
