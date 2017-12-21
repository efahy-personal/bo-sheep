using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
	public GameObject player;

	private float offsetX = 0.0f;
	private float offsetZ = 0.0f;
	private float height = 0.0f;

	void Start ()
	{
		offsetX = transform.position.x - player.transform.position.x;
		offsetZ = transform.position.z - player.transform.position.z;
		height = transform.position.y;
	}

	// Update is called once per frame
	void Update ()
	{
		transform.position = new Vector3(
			player.transform.position.x + offsetX,
			height,
			player.transform.position.z + offsetZ);
	}
}
