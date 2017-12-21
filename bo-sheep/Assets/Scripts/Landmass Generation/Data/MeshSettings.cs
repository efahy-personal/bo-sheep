using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdateableData {
	
	public const int maxSupportedLodCount = 5;
	public const int numSupportedChunkSizes = 9; // The length of supportedChunkSizes
	public const int numSupportedFlatShadedChunkSizes = 3; // The length of supportedFlatShadedChunkSizes
	public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
	// Scale of the terrain (in relation to the player)
	public float meshScale = 2.5f;
	public bool useFlatShading;
	[Range(0, numSupportedChunkSizes - 1)]
	public int chunkSizeIndex;
	[Range(0, numSupportedFlatShadedChunkSizes - 1)]
	public int flatShadedChunkSizeIndex;

	// For LOD implementation, the size of each map chunk is this.  Used to be
	// 241 before the border vertex complication.  Then it was 239 before
	// flatshading came along.  It needed to be lower for flatshading because
	// flatshading means more vertices per chunk and 239 would blow the Unity
	// vertices limit
	//
	// SL says: Num verts per line rendered at the highest resolution (i.e. LOD 0).
	// Includes the two extra vertices that are excluded from final mesh but used
	// for calculating normals
	public int verticesPerLineCount {
		get {
			// In the fixing gaps episode, we changed the "+ 1" at the end to a "+ 5"
			// since we're now working with two extra rings of vertices around each
			// chunk
			return supportedChunkSizes [(useFlatShading ? flatShadedChunkSizeIndex : chunkSizeIndex)] + 5;
		}
	}

	public float meshWorldSize {
		get {
			// Minus 1 for the size calc and minus another 2 for the border verts
			return (verticesPerLineCount - 3) * meshScale;
		}
	}

}
