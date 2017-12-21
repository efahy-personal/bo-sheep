using System.Collections;
using System.Collections.Generic;
using UnityEditor;

// Class to handle clicks of the "Generate" button in the inspector pane of the
// Unity edit and when clicked, to generate a new noise map and from it a texture
// that is applied to the plane in the scene.  The class attribute puts the
// button there in the first place
[CustomEditor (typeof (MapPreview))]
public class MapPreviewEditor : Editor {
	public override void OnInspectorGUI() {
		MapPreview mapPreview = (MapPreview)target;

		if (DrawDefaultInspector ()) {
			if (mapPreview.autoUpdate) {
				mapPreview.DrawMapInEditor ();
			}
		}

		if (UnityEngine.GUILayout.Button ("Generate")) {
			mapPreview.DrawMapInEditor ();
		}
	}
}
