using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {
	public GameObject sheepPrefab;
	public GameObject sheepContainer;

	public Text scoreText;
	public Text timeRemainingText;

	// Private variables
	GlobalVariables globalVariables;

	bool sheepGenerated = false;

	float gameStartTime;

	void Start ()
	{
		globalVariables = new GlobalVariables();

		StartGame();
	}
	
	// Update is called once per frame
	void Update ()
	{
		// Only generate sheep once, and only generate them after some time has elapsed since
		// the game started to allow the terrain to have generated.  This is because we trace
		// a ray into the ground to see what height the ground is so the sheep drops from just
		// above it
		if (!sheepGenerated && GetGameTimeElapsed() > GlobalVariables.TIME_FOR_FIRST_TERRAIN_GENERATION_IN_SECONDS) {
			SheepGenerator.GenerateSheep(sheepPrefab, sheepContainer);

			sheepGenerated = true;
		}

		globalVariables.timeRemaining = GlobalVariables.GAME_TIME_IN_SECONDS - GetGameTimeElapsed();

		if (globalVariables.timeRemaining < 0) {
			globalVariables.timeRemaining = 0.0f;
		}

		SetTimeRemainingText();

		if (globalVariables.timeRemaining == 0.0f) {
			GameOver();
		}
	}

	private void StartGame()
	{
		globalVariables.timeRemaining = GlobalVariables.GAME_TIME_IN_SECONDS;
		globalVariables.score = 0;
		gameStartTime = Time.time;

		SetScoreText();
		SetTimeRemainingText();
	}

	private void GameOver()
	{
		SceneManager.LoadScene(GlobalVariables.SCENE_INDEX_MAIN_MENU);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public float GetGameTimeElapsed()
	{
		return Time.time - gameStartTime;
	}

	public void SheepCaught(GameObject sheep)
	{
		Destroy(sheep);

		globalVariables.score++;
		SetScoreText();
	}

	private void SetScoreText()
	{
		scoreText.text = "Sheep collected: " + globalVariables.score.ToString();
	}

	private void SetTimeRemainingText()
	{
		timeRemainingText.text = "Time left: " + globalVariables.timeRemaining.ToString();
	}
}
