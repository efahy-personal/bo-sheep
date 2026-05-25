using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour {

	public bool lockCursor;
	public float mouseSensitivity = 10f;
	public Transform subject;
	public float distanceFromTarget = 2f;
	public Vector2 pitchLimits = new Vector2 (-12, 65);

	public float rotationSmoothTime = 0.12f;
	Vector3 rotationSmoothVelocity;
	Vector3 currentRotation;

	float yaw;
	float pitch;

	// Cache the PlayerController so we can check when it is fully initialised
	// (i.e. snapped to the terrain surface).  Until then we keep the camera
	// frozen so the view doesn't jump/drop as the player teleports.
	PlayerController playerController;

	void Start() {
		if (lockCursor) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		if (subject != null) {
			// Use GetComponentInParent so this works whether 'subject' is the player
			// root or a child transform (e.g. a camera-target bone).
			playerController = subject.GetComponentInParent<PlayerController>();
			if (playerController == null) {
				playerController = subject.GetComponentInChildren<PlayerController>();
			}
			if (playerController == null) {
				Debug.LogWarning("[ThirdPersonCameraController] Could not find PlayerController on or near 'subject'. Camera freeze disabled.");
			} else {
				Debug.Log($"[ThirdPersonCameraController] Found PlayerController on '{playerController.gameObject.name}'. Camera will freeze until player is ready.");
			}
		}
	}

	// LateUpdate is called after all the other update methods.  We use it here because at
	// this point we know the subject's position will have been set so the camera position,
	// which is based on it will be up to date and accurate
	void LateUpdate () {

		// Hold the camera still until the player has snapped to the terrain.
		// This prevents the view from dropping as the player teleports to the
		// correct ground height during startup.
		if (playerController != null && !playerController.IsReady) {
			return;
		}

		yaw += Input.GetAxis ("Mouse X") * mouseSensitivity;
		pitch -= Input.GetAxis ("Mouse Y") * mouseSensitivity;
		pitch = Mathf.Clamp (pitch, pitchLimits.x, pitchLimits.y);

		currentRotation = Vector3.SmoothDamp (currentRotation, new Vector3 (pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);

		transform.eulerAngles = currentRotation;

		// Set the position of the camera to be the configured distance behind
		// the target
		transform.position = subject.position - transform.forward * distanceFromTarget;
	}
}
