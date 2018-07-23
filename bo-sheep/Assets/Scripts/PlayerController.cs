using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {

	public float walkSpeed = 2f;
	public float runSpeed = 6f;
	public float gravity = -12;
	public float jumpHeight = 1.0f;
	[Range(0,1)]
	public float airControlPercent;
	public Text scoreText;
	public Text timeRemainingText;

	// Variables for smoothing and damping turn rate towards desired direction
	public float turnSmoothTime = 0.2f;
	float turnSmoothVelocity;

	// Variables for smoothing speed changes
	public float speedSmoothTime = 0.1f;
	float speedSmoothVelocity;
	float currentSpeed;
	float velocityY;

	Animator animator;
	Transform cameraTransform;

	CharacterController controller;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator> ();
		cameraTransform = Camera.main.transform;
		controller = GetComponent<CharacterController> ();
		SetScoreText();
		SetTimeRemainingText();
	}
	
	// Update is called once per frame
	void Update () {
		// 1. Calculate remaining time and update on screen
		GlobalVariables.timeRemaining = GlobalVariables.GAME_TIME_IN_SECONDS - Time.time;
		if (GlobalVariables.timeRemaining < 0) {
			GlobalVariables.timeRemaining = 0.0f;
		}

		SetTimeRemainingText();

		if (GlobalVariables.timeRemaining == 0.0f) {
			GameOver();
		}

		// 2. User Input Section
		Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));

		Vector2 inputDirection = input.normalized;

		// We're running if the player's holding left shift key
		bool isRunning = Input.GetKey (KeyCode.LeftShift);

		Move (inputDirection, isRunning);

		if (Input.GetKeyDown (KeyCode.Space)) {
			Jump();
		}

		// 3. Animation Section

		// Calculate the animation speed; again zero if we're not moving
		float animationSpeedPercent = (isRunning ? currentSpeed / runSpeed : currentSpeed / walkSpeed * 0.5f) * inputDirection.magnitude;

		// Last two parameters mean the animation speed is damped
		animator.SetFloat ("SpeedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
	}

	void Move (Vector2 inputDirection, bool isRunning) {
		// If the player is providing input, do stuff
		if (inputDirection != Vector2.zero) {
			// Set the rotation angle based on user input.  Basically this is
			// the rotation in degrees on the Y axis
			float targetRotation = Mathf.Atan2 (inputDirection.x, inputDirection.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

			transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle (transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime));
		}

		// Multiplying by inputDirection.magnitude so that speed is zero if we're
		// not moving
		float targetSpeed = (isRunning ? runSpeed : walkSpeed) * inputDirection.magnitude;

		currentSpeed = Mathf.SmoothDamp (currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

		// Calculate veritcal speed resulting from gravity
		velocityY += Time.deltaTime * gravity;

		// Move the player
		Vector3 velocity = transform.forward * currentSpeed + Vector3.up * velocityY;

		controller.Move (velocity * Time.deltaTime);

		// Update the current speed to what the character controller is reporting,
		// because it knows about collisions and will set the speed correctly based
		// on whether we've collided with something.  Then we use the updated current
		// speed below to set the animation speed
		currentSpeed = new Vector2 (controller.velocity.x, controller.velocity.z).magnitude;

		// If we're on the ground, vertical speed from gravity is zero
		if (controller.isGrounded) {
			velocityY = 0;
		}
	}

	void Jump() {
		if (controller.isGrounded)
		{
			float jumpVelocity = Mathf.Sqrt (-2 * gravity * jumpHeight);

			velocityY = jumpVelocity;
		}
	}

	// Adjust the smoothtime if we're jumping.  The amount of adjustment is controlled
	// by the airControlPercent slider and basically the higher the smoothtime, the
	// slower the character's resonse to control inputs will be
	float GetModifiedSmoothTime(float smoothTime) {
		if (controller.isGrounded)
		{
			return smoothTime;
		}
		else
		{
			// prevent divide by zero below
			if (airControlPercent == 0)
			{
				return float.MaxValue;
			}

			return smoothTime / airControlPercent;
		}
	}
	
	void OnTriggerEnter(Collider triggerCollider) {
		if (triggerCollider.tag == "Sheep")
		{
			// What's triggered and got us in here is a child of the dummy sheep object, which is
			// not rendered and is only there for the triggering.  We need the child because the
			// main dummy cheep object has full collision on so the sheep doesn't fall through the
			// ground.  Also not that sheep layer (sheep layer) and player layer (default layer)
			// are set not to collide in physics settings
			Destroy(triggerCollider.gameObject.transform.parent.gameObject);

			GlobalVariables.score++;
			SetScoreText();
		}
    }

	void SetScoreText() {
		scoreText.text = "Sheep collected: " + GlobalVariables.score.ToString();
	}

	void SetTimeRemainingText() {
		timeRemainingText.text = "Time left: " + GlobalVariables.timeRemaining.ToString();
	}

	public void GameOver() {
		SceneManager.LoadScene(GlobalVariables.SCENE_INDEX_MAIN_MENU);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}
}
