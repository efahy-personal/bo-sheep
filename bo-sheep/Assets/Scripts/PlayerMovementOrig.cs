using UnityEngine;

public class PlayerMovementOrig : MonoBehaviour
{
	public float speed = 3.0f;
	public float turnSpeed = 60.0f;
	public float gravity = 20.0f;

	public GameObject ground;

	Vector3 lastGroundClickPosition = Vector3.zero;
	private float clickArrivalDistanceTolerance = 0.4f;
	private bool turning = false;

	Animator anim;
	//CharacterController controller;
	Rigidbody playerRigidbody;
	int floorMask;
	private float camRayLength = 400f;

	void Awake()
	{
		floorMask = LayerMask.GetMask("Floor");
		//anim = GetComponent<Animator>();
		anim = gameObject.GetComponentInChildren<Animator>();
		//controller = GetComponent<CharacterController>();
		playerRigidbody = GetComponent<Rigidbody>();
	}

	void Update()
	{
		//anim.SetBool("IsWalking", Input.GetAxisRaw("Vertical") != 0f);

		// if (controller.isGrounded)
		// {
		// 	moveDirection = transform.forward * Input.GetAxis("Vertical") * speed;
		// }

		//float turn = Input.GetAxis("Horizontal");

		// transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);
		// controller.Move(moveDirection * Time.deltaTime);
		// moveDirection.y -= gravity * Time.deltaTime;

		//Ray ray;

		// if(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Stationary)
		// {
		// 	ray = Camera.main.ScreenPointToRay (Input.GetTouch(0).position);
  //       	Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
  //       }
  //       else
  //       {

		Vector3 moveToPosition = Vector3.zero;
		
		if (Input.mousePresent && Input.GetMouseButtonDown(0))
		{
			// If mouse has been clicked, get the point on the ground that was clicked
			moveToPosition = Input.mousePosition;
		}
		else
		{
			// Or if a screen has been touched on a touchscreen device, get the point
			// that was cliacked
			foreach (Touch touch in Input.touches)
			{
				if (touch.phase == TouchPhase.Began)
				{
					moveToPosition = touch.position;
				}
			}
		}

		// If we have a point to move to, either from the mouse or from a touch on a
		// touch screen, cast a ray and see where the touch hits the ground
		if (moveToPosition != Vector3.zero)
		{
			// Get a ray from the camera through to the mouse position
			Ray camRay = Camera.main.ScreenPointToRay(moveToPosition);

			// If the ray hits the ground, get the point of the hit and get the player moving
			// towards it
			RaycastHit rayHit;

			if (Physics.Raycast(camRay, out rayHit, camRayLength, floorMask))
			{
				lastGroundClickPosition = rayHit.point;
				turning = true;
			}
		}

		// Assume we're not walking until we learn otherwise
		anim.SetBool("IsWalking", false);
		anim.SetBool("IsRunning", false);

		if (lastGroundClickPosition != Vector3.zero)
		{
			float distanceToClick = Vector3.Distance (lastGroundClickPosition, transform.position);
			Vector3 playerToMouse = lastGroundClickPosition - transform.position;
			float angleToClick = Vector3.Angle(transform.forward, playerToMouse);
		    Vector3 cross = Vector3.Cross(transform.forward, playerToMouse);

		    float turningSign = (cross.y < 0 ? -1 : 1);

			if (distanceToClick > clickArrivalDistanceTolerance)
			{
				Debug.Log("distanceToClick: " + distanceToClick + "; clickArrivalDistanceTolerance: " + clickArrivalDistanceTolerance);

				playerToMouse.y = 0f;

				Vector3 movement = playerToMouse.normalized * speed * Time.deltaTime;

				playerRigidbody.MovePosition(transform.position + movement);

				anim.SetBool("IsWalking", true);
				anim.SetBool("IsRunning", true);
			}
			else
			{
				turning = false;
			}

			if (turning)
			{
				float maxPossibleTurnThisFrame = turnSpeed * Time.deltaTime;

				Debug.Log("angleToClick: " + angleToClick + "; maxPossibleTurnThisFrame: " + maxPossibleTurnThisFrame + "; turningSign: " + turningSign);

				if (angleToClick > maxPossibleTurnThisFrame)
				{
					transform.Rotate(0, turningSign * maxPossibleTurnThisFrame, 0);
				}
				else
				{
					transform.Rotate(0, turningSign * angleToClick, 0);
					turning = false;
				}
			}
		}

		
	}

	// Run before each physics update
	// void FixedUpdate()
	// {
	// 	float h = Input.GetAxisRaw("Horizontal");
	// 	float v = Input.GetAxisRaw("Vertical");

	// 	Move(h, v);
	// 	//Turn();
	// 	Animate(h, v);
	// }

	// void Move(float h, float v)
	// {
	// 	movement.Set(h, 0, v);
	// 	movement = movement.normalized * speed * Time.deltaTime;

	// 	playerRigidbody.MovePosition(transform.position + movement);
	// }

	// void Turn()
	// {
	// 	Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

	// 	RaycastHit floorHit;

	// 	if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
	// 	{
	// 		Vector3 playerToMouse = floorHit.point - transform.position;
	// 		playerToMouse.y = 0f;

	// 		Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

	// 		playerRigidbody.MoveRotation(newRotation);
	// 	}
	// }

	// void Animate(float h, float v)
	// {
	// 	bool walking = h != 0f || v != 0f;

	// 	anim.SetBool("IsWalking", walking);
	// }
}
