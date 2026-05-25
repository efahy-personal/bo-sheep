using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour {

	public float walkSpeed = 2f;
	public float runSpeed = 6f;
	public float gravity = -12;
	public float jumpHeight = 1.0f;
	[Range(0,1)]
	public float airControlPercent;
	public float walkAnimSpeed = 1.3f;
	public float runAnimSpeed = 1.5f;
	public TextMeshProUGUI scoreText;
	public TextMeshProUGUI timeRemainingText;

	// Variables for smoothing and damping turn rate towards desired direction
	public float turnSmoothTime = 0.2f;
	float turnSmoothVelocity;

	// Variables for smoothing speed changes
	public float speedSmoothTime = 0.1f;
	float speedSmoothVelocity;
	float currentSpeed;
	float velocityY;

	public bool enableTimer = true;
	bool terrainReady = false;    // Phase 0→1: terrain collider confirmed at player position
	bool isInitialized = false;   // Phase 1→2: CharacterController first grounded
	public bool IsReady => isInitialized;
	TerrainGenerator terrainGenerator;
	Renderer[] playerRenderers;

	Animator animator;
	Transform cameraTransform;

	CharacterController controller;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator> ();
		if (animator == null) {
			animator = GetComponentInChildren<Animator> ();
			Debug.Log($"[PlayerController] Animator: GetComponent on self was null. GetComponentInChildren found: {(animator != null ? animator.gameObject.name : "null")}");
		} else {
			Debug.Log($"[PlayerController] Animator found on self.");
		}

		if (animator != null) {
			Debug.Log($"[PlayerController] Animator details -> GameObject: '{animator.gameObject.name}', " +
					  $"Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")}, " +
					  $"Avatar: {(animator.avatar != null ? animator.avatar.name : "null")}");
		} else {
			Debug.LogError("[PlayerController] Animator component is completely missing on player and all children!");
		}

		cameraTransform = Camera.main != null ? Camera.main.transform : null;
		controller = GetComponent<CharacterController> ();
		terrainGenerator = FindAnyObjectByType<TerrainGenerator>();
		SetScoreText();
		SetTimeRemainingText();

		// Hide the player until the terrain is ready and we've snapped to the
		// correct ground height. This prevents a visible drop/teleport on startup.
		playerRenderers = GetComponentsInChildren<Renderer>();
		foreach (Renderer r in playerRenderers) {
			r.enabled = false;
		}
		Debug.Log($"[PlayerController] Start: position={transform.position}, hiding {playerRenderers.Length} renderers. Waiting for terrain...");
	}
	
	// Update is called once per frame
	void Update () {

		// ── Phase 0: wait for the terrain collider to be ready at our position ──
		if (!terrainReady) {
			if (terrainGenerator == null) {
				terrainGenerator = FindAnyObjectByType<TerrainGenerator>();
			}
			if (terrainGenerator != null && terrainGenerator.IsTerrainReadyAt(transform.position)) {
				terrainReady = true;
				velocityY = 0;
				Debug.Log($"[PlayerController] Terrain ready! Releasing player to settle. Y={transform.position.y:F3}");
			} else {
				return; // Still waiting – keep everything frozen
			}
		}

		// ── Phase 1: terrain ready but not yet grounded ─────────────────────────
		// Let the CharacterController fall naturally under gravity (player still
		// invisible). This replicates the original game's settling behaviour and
		// lets the CharacterController find its own correct grounded height –
		// the same position the game always landed on before our changes.
		if (!isInitialized) {
			Move(Vector2.zero, false);
			if (controller.isGrounded) {
				isInitialized = true;
				foreach (Renderer r in playerRenderers) {
					r.enabled = true;
				}
				Debug.Log($"[PlayerController] Grounded and revealed at Y={transform.position.y:F3}");
			}
			return;
		}

		if (cameraTransform == null && Camera.main != null) {
			cameraTransform = Camera.main.transform;
		}

		// 1. Calculate remaining time and update on screen
		if (enableTimer) {
			GlobalVariables.timeRemaining = GlobalVariables.GAME_TIME_IN_SECONDS - Time.time;
			if (GlobalVariables.timeRemaining < 0) {
				GlobalVariables.timeRemaining = 0.0f;
			}

			SetTimeRemainingText();

			if (GlobalVariables.timeRemaining == 0.0f) {
				// Last two parameters mean the animation speed is damped
				animator.SetFloat ("SpeedPercent", 0.0f);

				Move (Vector2.zero, false);

				return;
			}
		} else {
			if (timeRemainingText != null) {
				timeRemainingText.text = "Timer Disabled";
			}
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
		if (animator != null) {
			animator.SetFloat ("SpeedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
			animator.speed = currentSpeed > 0.1f ? (isRunning ? runAnimSpeed : walkAnimSpeed) : 1.0f;
		}
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

		// Calculate vertical speed resulting from gravity
		velocityY += Time.deltaTime * gravity;

		// Move the player
		Vector3 velocity = transform.forward * currentSpeed + Vector3.up * velocityY;

		controller.Move (velocity * Time.deltaTime);

		// Update the current speed for animation logic (using the desired currentSpeed calculated above)
		// We'll skip overwriting with controller.velocity.magnitude for now to ensure animation plays
		// even if the character controller is having trouble reporting velocity

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

	void SnapToGround() {
		// Cast a ray from well above the player downwards to find the exact terrain height.
		RaycastHit hit;
		Vector3 rayOrigin = new Vector3(transform.position.x, 500.0f, transform.position.z);

		Debug.Log($"[PlayerController] SnapToGround: player at Y={transform.position.y:F3}. " +
				  $"CC height={controller.height:F3}, center.y={controller.center.y:F3}, skinWidth={controller.skinWidth:F3}");

		// Layer 8 is ground. Floor is standard floor layer.
		if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1000.0f, (1 << 8) | LayerMask.GetMask("Floor"))) {
			float groundHeight = hit.point.y;

			// The CharacterController capsule bottom is at:
			//   transform.position.y + controller.center.y - controller.height/2
			// We want that bottom to be at groundHeight + skinWidth*0.5 (inside grounding range).
			// Rearranging: targetY = groundHeight + height/2 - center.y + skinWidth*0.5
			float targetY = groundHeight + controller.height / 2.0f - controller.center.y + controller.skinWidth * 0.5f;
			float capsuleBottom = targetY + controller.center.y - controller.height / 2.0f;

			Debug.Log($"[PlayerController] SnapToGround HIT terrain Y={groundHeight:F3}. " +
					  $"Moving player: Y={transform.position.y:F3} -> Y={targetY:F3}. " +
					  $"Capsule bottom will be at Y={capsuleBottom:F3} (gap from terrain: {capsuleBottom - groundHeight:F3})");

			bool wasEnabled = controller.enabled;
			controller.enabled = false;
			transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
			controller.enabled = wasEnabled;
			velocityY = 0;
		} else {
			Debug.LogWarning($"[PlayerController] SnapToGround MISS — no Ground/Floor collider found from Y=500 at X={transform.position.x:F2}, Z={transform.position.z:F2}!");
		}
	}

	void SetScoreText() {
		if (scoreText != null) {
			scoreText.text = "Sheep collected: " + GlobalVariables.score.ToString();
		}
	}

	void SetTimeRemainingText() {
		if (timeRemainingText != null) {
			if (enableTimer) {
				timeRemainingText.text = "Time left: " + GlobalVariables.timeRemaining.ToString("F1");
			} else {
				timeRemainingText.text = "Timer Disabled";
			}
		}
	}
}
