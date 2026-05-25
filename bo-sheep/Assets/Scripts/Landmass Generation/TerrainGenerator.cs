using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public Material meshMaterial;

	Vector2 viewerPosition;
	Vector2 viewerPositionAtLastChunkUpdate;

	float meshWorldSize;
	int chunksVisibleInViewDistance;

	// Dictionary of terrain chunks that'll allow us to keep track of the
	// chunks we've drawn so we don't end up drawing any of them twice.
	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
		textureSettings.ApplyToMaterial (meshMaterial);
		textureSettings.UpdateMeshHeights (meshMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		float maxViewDistance = detailLevels [detailLevels.Length - 1].visibleDistanceThreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDistance = Mathf.RoundToInt (maxViewDistance / meshWorldSize);

		// Always update visible chunks at the start since in the Update()
		// method we don't do so until the viewer has moved a bit
		UpdateVisibleChunks ();
	}

	Vector2 viewerPositionAtLastColliderUpdate;
	const float viewerMoveThresholdForColliderUpdate = 1.5f;
	const float sqrViewerMoveThresholdForColliderUpdate = viewerMoveThresholdForColliderUpdate * viewerMoveThresholdForColliderUpdate;

	void Update() {
		if (viewer == null) {
			GameObject player = GameObject.FindWithTag("Player");
			if (player != null) viewer = player.transform;
			else return;
		}

		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		// If the player has moved significantly since last collider update
		if ((viewerPositionAtLastColliderUpdate - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForColliderUpdate) {
			viewerPositionAtLastColliderUpdate = viewerPosition;
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}

		if ((viewerPositionAtLastChunkUpdate - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionAtLastChunkUpdate = viewerPosition;
			UpdateVisibleChunks ();

			int currentChunkCoordX = Mathf.FloorToInt (viewerPosition.x / meshWorldSize);
			int currentChunkCoordY = Mathf.FloorToInt (viewerPosition.y / meshWorldSize);
			Debug.Log ($"[Terrain] Moved! Position: {viewerPosition} (Chunk: {currentChunkCoordX}, {currentChunkCoordY})");
		}
	}

	void UpdateVisibleChunks() {
		HashSet<Vector2> updatedChunkCoordinates = new HashSet<Vector2> ();

		// Loop backwards over the list because we might end up removing some
		// chunks as we go through them and looping forwards like normal would
		// mean we might end up with out of bounds issues
		for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
			updatedChunkCoordinates.Add (visibleTerrainChunks [i].coordinate);
			visibleTerrainChunks [i].UpdateTerrainChunk();
		}

		// Get the X and Y coordinates where the player/viewer currently is
		int currentChunkCoordX = Mathf.FloorToInt (viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.FloorToInt (viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				// Only update if we haven't already updated in the earlier loop
				if (!updatedChunkCoordinates.Contains (viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						TerrainChunk newChunk = new TerrainChunk (viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, meshMaterial);
						terrainChunkDictionary.Add (viewedChunkCoord, newChunk);
						newChunk.onVisisbilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load ();
					}
				}
			}
		}
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleTerrainChunks.Add (chunk);
		} else {
			visibleTerrainChunks.Remove (chunk);
		}
	}

	public bool IsTerrainReadyAt(Vector3 position) {
		float worldSize = meshWorldSize;
		if (worldSize == 0 && meshSettings != null) {
			worldSize = meshSettings.meshWorldSize;
		}
		if (worldSize == 0) return false;

		int chunkX = Mathf.FloorToInt (position.x / worldSize);
		int chunkY = Mathf.FloorToInt (position.z / worldSize);
		Vector2 chunkCoord = new Vector2 (chunkX, chunkY);

		if (terrainChunkDictionary.TryGetValue (chunkCoord, out TerrainChunk chunk)) {
			return chunk.HasSetCollider;
		}
		return false;
	}
}

[System.Serializable]
public struct LODInfo {
	[Range(0, MeshSettings.maxSupportedLodCount - 1)]
	public int lod;
	public float visibleDistanceThreshold;

	public float sqrVisibleDistanceThreshold {
		get {
			return visibleDistanceThreshold * visibleDistanceThreshold;
		}
	}
}
