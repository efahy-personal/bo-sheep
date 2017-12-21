using UnityEngine;

public class MapPreview : MonoBehaviour {
	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, Mesh, FalloffMap };

	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureData;

	public Material terrainMaterial;

	// Actual values for this needs to be a factor of 240 (i.e. 241 - 1).  We're
	// gonna allow the values 2, 4, 6, 8, 10, 12 and to get them we allow this
	// to be set in the range 0 to 6 and multiple by 2 below
	[Range(0,MeshSettings.maxSupportedLodCount - 1)]
	public int editorPreviewLevelOfDetail;

	public bool autoUpdate;

	public void DrawMapInEditor() {
		textureData.ApplyToMaterial (terrainMaterial);
		textureData.UpdateMeshHeights (terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap (meshSettings.verticesPerLineCount, meshSettings.verticesPerLineCount, heightMapSettings, Vector2.zero);

		if (drawMode == DrawMode.NoiseMap) {
			DrawTerrain (TextureGenerator.TextureFromHeightMap (heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			DrawMesh (MeshGenerator.GenerateTerrainMesh (heightMap.values, meshSettings, editorPreviewLevelOfDetail));
		} else if (drawMode == DrawMode.FalloffMap) {
			DrawTerrain (TextureGenerator.TextureFromHeightMap (new HeightMap (FalloffGenerator.GenerateFalloffMap (meshSettings.verticesPerLineCount), 0, 1)));
		}
	}

	public void DrawTerrain(Texture2D terrainTexture) {
		// In order for the texture to be visible in the Unity editor before we run the game
		// we use the textureRenderer's sharedMaterial rather than its material, which would
		// only be instantiated at runtime
		textureRenderer.sharedMaterial.mainTexture = terrainTexture;
		textureRenderer.transform.localScale = new Vector3 (terrainTexture.width, 1, terrainTexture.height) / 10f;

		textureRenderer.gameObject.SetActive (true);
		meshFilter.gameObject.SetActive (false);
	}

	public void DrawMesh (MeshData meshData) {
		// sharedMesh so we can generate outside of game mode
		meshFilter.sharedMesh = meshData.CreateMesh ();

		textureRenderer.gameObject.SetActive (false);
		meshFilter.gameObject.SetActive (true);
	}

	/// <summary>
	/// Method that triggers a redraw of the map in the editor.  This is used as a callback
	/// in the NoiseData and TerrainData scripts that they end up calling if values are
	/// changed.
	/// </summary>
	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor ();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial (terrainMaterial);
	}

	// Called automatically when one of the script's property values is changed in
	// the editor
	void OnValidate() {

		// Set our OnValuesUpdated method as a callback for the terrainData and noiseData
		// scripts so that if values in assets based on them are changed, our callback is
		// called.  We'll update the editor preview mesh/map when so called.
		if (meshSettings != null) {
			// If we're already subscribed, unsubscript so that on the next line we don't
			// end up subscribing multiple times.  Note if we haven't subscribed at all yet,
			// this line does nothing
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}

		if (heightMapSettings != null) {
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}

		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}

}
