using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {
	// Public variables
	public GameObject sheepPrefab;
	public GameObject sheepContainer;

	// Private variables
	bool sheepGenerated = false;
	TerrainGenerator terrainGenerator;

	// Use this for initialization
	void Start () {
		terrainGenerator = FindAnyObjectByType<TerrainGenerator>();
	}
	
	// Update is called once per frame
	void Update () {
		// Only generate sheep once, and only generate them after the terrain has generated
		// its colliders at the spawn center. This ensures that the raycasts used to place
		// sheep on the ground hit the terrain collider properly instead of falling through.
		if (!sheepGenerated) {
			if (terrainGenerator == null) {
				terrainGenerator = FindAnyObjectByType<TerrainGenerator>();
			}

			if (terrainGenerator != null && terrainGenerator.IsTerrainReadyAt(Vector3.zero)) {
				SheepGenerator.GenerateSheep(sheepPrefab, sheepContainer);
				sheepGenerated = true;
			}
		}
	}
}
