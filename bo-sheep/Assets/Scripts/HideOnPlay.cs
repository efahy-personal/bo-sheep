using UnityEngine;

public class HideOnPlay : MonoBehaviour {
	// Use this for initialization.  Awake() is called before any Start()s
	void Awake () {
		gameObject.SetActive (false);
	}
}
