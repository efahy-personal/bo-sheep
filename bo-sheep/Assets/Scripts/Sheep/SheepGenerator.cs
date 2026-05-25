using System.Collections.Generic;
using UnityEngine;

public static class SheepGenerator
{
	public static List<GameObject> sheepList = new List<GameObject>();

	// Use this for initialization
	public static void GenerateSheep (GameObject sheepPrefab, GameObject sheepContainer) {
		System.Random pseudoRandomNumberGenerator = new System.Random (SheepGeneratorSettings.seed);

		int sheepCount = pseudoRandomNumberGenerator.Next (SheepGeneratorSettings.sheepCountMin, SheepGeneratorSettings.sheepCountMax);

		for (int i = 0; i < sheepCount; i++)
		{
			Vector2 newSheepPosition = new Vector2(
				pseudoRandomNumberGenerator.Next(-SheepGeneratorSettings.sheepDistanceOnAxisMax, SheepGeneratorSettings.sheepDistanceOnAxisMax),
				pseudoRandomNumberGenerator.Next(-SheepGeneratorSettings.sheepDistanceOnAxisMax, SheepGeneratorSettings.sheepDistanceOnAxisMax));

			// Raycast from well above any possible terrain peak straight down, hitting
			// only the Ground layer (layer 8). We do this BEFORE Instantiate so the
			// sheep's own collider can't interfere with the cast.
			RaycastHit hit;
			int groundLayerMask = 1 << 8;
			bool didHit = Physics.Raycast(
				new Vector3(newSheepPosition.x, 500.0f, newSheepPosition.y),
				Vector3.down, out hit, 1000.0f, groundLayerMask);

			// Place the sheep at the terrain surface. +0.5f accounts for the sheep
			// model pivot being roughly at its centre (tweak if they float/clip).
			float sheepY = didHit ? hit.point.y + 0.5f : 2.5f;
			if (!didHit) {
				Debug.LogWarning($"[SheepGenerator] Raycast missed for sheep at ({newSheepPosition.x}, {newSheepPosition.y}) — spawning at fallback height.");
			}

			GameObject newSheep = MonoBehaviour.Instantiate(sheepPrefab);
			newSheep.transform.position = new Vector3(newSheepPosition.x, sheepY, newSheepPosition.y);
			newSheep.transform.parent = sheepContainer.transform;

			newSheep.AddComponent<SheepWobble>();

			sheepList.Add(newSheep);
		}
	}
}

public static class SheepGeneratorSettings
{
	public static int seed = 0;
	public static int sheepCountMin = 50;
	public static int sheepCountMax = 100;
	public static int sheepDistanceOnAxisMax = 50;
}