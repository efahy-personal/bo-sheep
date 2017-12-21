using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
	public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);

		// To have crisp edges in our texture
		texture.filterMode = FilterMode.Point;

		// To prevent the texture from wrapping bits of the opposite edge
		texture.wrapMode = TextureWrapMode.Clamp;

		texture.SetPixels (colourMap);
		texture.Apply ();

		return texture;
	}

	public static Texture2D TextureFromHeightMap(HeightMap heightMap) {
		int width = heightMap.values.GetLength (0);
		int height = heightMap.values.GetLength (1);

		// Rather than loop over every pixel in the texture and set them according
		// to the corresponding noise value inside the loop, its quicker to pre-build
		// an array of the colours of all pixels in the texture and apply them to the
		// texture using its setPixels method
		Color[] colourMap = new Color[width * height];

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values [x, y]));
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}
}
