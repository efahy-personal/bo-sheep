using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {
	// Public variables
	public GameObject sheepPrefab;
	public GameObject sheepContainer;

	// Private variables
	bool sheepGenerated = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		// Only generate sheep once, and only generate them after some time has elapsed since
		// the game started to allow the terrain to have generated.  This is because we trace
		// a ray into the ground to see what height the ground is so the sheep drops from just
		// above it
		if (!sheepGenerated && Time.time > GlobalVariables.TIME_FOR_FIRST_TERRAIN_GENERATION_IN_SECONDS) {
			SheepGenerator.GenerateSheep(sheepPrefab, sheepContainer);

			sheepGenerated = true;
		}
	}
}
