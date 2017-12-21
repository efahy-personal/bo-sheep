using UnityEngine;

public class TerrainChunk {
	const float colliderGenerationDistanceThreshold = 5;

	public event System.Action<TerrainChunk, bool> onVisisbilityChanged;
	public Vector2 coordinate;

	GameObject meshObject;
	Vector2 sampleCentre;

	// Used to calculate the distance between two points below
	Bounds bounds;

	MeshRenderer meshRenderer;
	MeshFilter meshFilter;
	MeshCollider meshCollider;

	LODInfo[] detailLevels;
	LODMesh[] lodMeshes;

	int colliderLODIndex;

	HeightMap heightMap;
	bool heightMapReceived;

	// Store the old LODIndex, so we know in UpdateTerrainChunk() if it hasn't
	// changed and we don't need to do anything
	int previousLODIndex = -1;

	bool hasSetCollider;
	float maxViewDistance;

	HeightMapSettings heightMapSettings;
	MeshSettings meshSettings;
	Transform viewer;

	public TerrainChunk(Vector2 coordinate, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
		this.coordinate = coordinate;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.heightMapSettings = heightMapSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		sampleCentre = coordinate * meshSettings.meshWorldSize / meshSettings.meshScale;
		Vector2 position = coordinate * meshSettings.meshWorldSize;
		bounds = new Bounds (position, Vector2.one * meshSettings.meshWorldSize);

		meshObject = new GameObject("Terrain Chunk");
		meshObject.name = "Terrain Chunk " + coordinate.ToString();
		meshObject.layer = 8; // Ground layer
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshRenderer.material = material;


		meshObject.transform.position = new Vector3(position.x, 0, position.y);
		meshObject.transform.parent = parent;

		// Default chunks to invisible to begin
		SetVisible(false);

		lodMeshes = new LODMesh[detailLevels.Length];

		for (int i = 0; i < detailLevels.Length; i++) {
			lodMeshes[i] = new LODMesh(detailLevels[i].lod);

			// Set up the callbacks to be called when we get an LOD update
			lodMeshes[i].updateCallback += UpdateTerrainChunk;
			// Only update the collision mesh if the update pertains to the LOD mesh
			// of the index we're using as collision mesh
			if (i == colliderLODIndex) {
				lodMeshes[i].updateCallback += UpdateCollisionMesh;
			}
		}

		maxViewDistance = detailLevels [detailLevels.Length - 1].visibleDistanceThreshold;
	}

	public void Load() {
		ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.verticesPerLineCount, meshSettings.verticesPerLineCount, heightMapSettings, sampleCentre), OnHeightMapGenerated);
	}

	void OnHeightMapGenerated(object heightMapObject) {
		this.heightMap = (HeightMap)heightMapObject;
		heightMapReceived = true;

		UpdateTerrainChunk ();
	}

	Vector2 viewerPosition {
		get {
			return new Vector2 (viewer.position.x, viewer.position.z);
		}
	}

	public void UpdateTerrainChunk() {
		if (heightMapReceived) {
			float viewerDistanceFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
			bool wasVisible = IsVisible ();

			// Should the chunk be visible?
			bool chunkVisible = viewerDistanceFromNearestEdge <= maxViewDistance;

			if (chunkVisible) {
				int lodIndex = 0;

				// Loop to find the index of the LOD we want to be displaying, based on the
				// current viewer distance
				for (int i = 0; i < detailLevels.Length - 1; i++) {
					if (viewerDistanceFromNearestEdge > detailLevels [i].visibleDistanceThreshold) {
						lodIndex = i + 1;
					} else {
						break;
					}
				}

				// If the required LOD has changed and we already have the new mesh display it,
				// otherwise if we haven't already requested it then request it
				if (lodIndex != previousLODIndex) {
					LODMesh lodMesh = lodMeshes [lodIndex];

					if (lodMesh.meshAvailable) {
						previousLODIndex = lodIndex;

						meshFilter.mesh = lodMesh.mesh;
					} else if (!lodMesh.meshRequested) {
						lodMesh.RequestMesh (heightMap, meshSettings);
					}
				}
			}

			if (wasVisible != chunkVisible) {
				SetVisible (chunkVisible);

				if (onVisisbilityChanged != null) {
					onVisisbilityChanged (this, chunkVisible);
				}
			}
		}
	}

	// Called more frequently than UpdateTerrainChunk so we have a frequently
	// updated player position and based on that, create collision meshes in
	// a timely manner
	public void UpdateCollisionMesh() {
		if (!hasSetCollider) {
			float sqrDistanceFromViewerToEdge = bounds.SqrDistance (viewerPosition);

			if (sqrDistanceFromViewerToEdge < detailLevels [colliderLODIndex].sqrVisibleDistanceThreshold) {
				if (!lodMeshes [colliderLODIndex].meshRequested) {
					lodMeshes [colliderLODIndex].RequestMesh (heightMap, meshSettings);
				}
			}

			// If the player is within the threshold distance to the adjacent chunk
			if (sqrDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
				if (lodMeshes [colliderLODIndex].meshAvailable) {
					meshCollider.sharedMesh = lodMeshes [colliderLODIndex].mesh;
					hasSetCollider = true;
				}
			}
		}
	}

	public void SetVisible(bool visible) {
		meshObject.SetActive (visible);
	}

	public bool IsVisible() {
		return meshObject.activeSelf;
	}

	class LODMesh {
		public Mesh mesh;
		public bool meshRequested;
		public bool meshAvailable;
		int lod;

		// Callback to be called when mesh is received
		public event System.Action updateCallback;

		public LODMesh(int lod) {
			this.lod = lod;
		}

		void OnMeshDataReceived(object meshDataObject) {
			mesh = ((MeshData)meshDataObject).CreateMesh ();
			meshAvailable = true;

			updateCallback ();
		}

		public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
			meshRequested = true;
			ThreadedDataRequester.RequestData (() => MeshGenerator.GenerateTerrainMesh (heightMap.values, meshSettings, lod), OnMeshDataReceived);
		}
	}
}