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

	void Start() {
		if (lockCursor) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	// LateUpdate is called after all the other update methods.  We use it here because at
	// this point we know the subject's position will have been set so the camera position,
	// which is based on it will be up to date and accurate
	void LateUpdate () {

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
