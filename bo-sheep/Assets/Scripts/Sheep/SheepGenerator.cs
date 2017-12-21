using System.Collections.Generic;
using UnityEngine;

public static class SheepGenerator
{
	public static List<GameObject> sheepList = new List<GameObject>();

	// Use this for initialization
	public static void GenerateSheep (GameObject sheepPrefab, GameObject sheepContainer) {
		System.Random pseudoRandomNumberGenerator = new System.Random (SheepGeneratorSettings.seed);

		int sheepCount = pseudoRandomNumberGenerator.Next (SheepGeneratorSettings.sheepCountMin, SheepGeneratorSettings.sheepCountMin);

		for (int i = 0; i < sheepCount; i++)
		{
			GameObject newSheep = MonoBehaviour.Instantiate(sheepPrefab);

			Vector2 newSheepPosition = new Vector2(pseudoRandomNumberGenerator.Next(-SheepGeneratorSettings.sheepDistanceOnAxisMax, SheepGeneratorSettings.sheepDistanceOnAxisMax),
					pseudoRandomNumberGenerator.Next(-SheepGeneratorSettings.sheepDistanceOnAxisMax, SheepGeneratorSettings.sheepDistanceOnAxisMax));

			RaycastHit hit;

			Physics.Raycast(new Vector3(newSheepPosition.x, 100.0f, newSheepPosition.y), Vector3.down, out hit);

			float sheepPlacementHeight = 100.0f - hit.distance + 0.3f;

			newSheep.transform.position = new Vector3(newSheepPosition.x, sheepPlacementHeight, newSheepPosition.y);

			newSheep.transform.parent = sheepContainer.transform;

			sheepList.Add(newSheep);
		}
	}
}

public static class SheepGeneratorSettings
{
	public static int seed = 0;
	public static int sheepCountMin = 200;
	public static int sheepCountMax = 300;
	public static int sheepDistanceOnAxisMax = 50;
}