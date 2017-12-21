using UnityEngine;

public static class MeshGenerator{


	/// <summary>
	/// Generates the terrain mesh (run in separate thread to the main
	/// game thread)
	/// </summary>
	public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) {
		int skipIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
		int numVertsPerLine = meshSettings.verticesPerLineCount;

		Vector2 topLeft = new Vector2 (-1, 1) * meshSettings.meshWorldSize / 2f;

		MeshData meshData = new MeshData (numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

		// Array of ints to hold the indices of all vertices in the mesh, including
		// the border ones.  Mesh vertex numbering will start at 0 and go up, according
		// to the order in which we loop over them below.  Border vertex numbering will
		// start at -1 and go down, i.e. they'll all have negative indices
		int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
		int meshVertexIndex = 0;
		int outOfMeshVertextIndex = -1;

		for (int y = 0; y < numVertsPerLine; y++) {
			for (int x = 0; x < numVertsPerLine; x ++) {
				bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
				bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				if (isOutOfMeshVertex) {
					vertexIndicesMap [x, y] = outOfMeshVertextIndex;
					outOfMeshVertextIndex--;
				} else if (!isSkippedVertex) {
					vertexIndicesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < numVertsPerLine; y ++) {
			for (int x = 0; x < numVertsPerLine; x++) {
				bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				if (!isSkippedVertex) {
					// Categorise the vertices.  These mesh edge and main vertices and so on were
					// introduced as part of the initiative to get rid of gaps between chunks of
					// different LODs.  There's a picture in the video that's a square of dots in
					// five different colours.
					bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
					bool isMeshEdgeVertex = y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2 && !isOutOfMeshVertex;
					bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
					bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
					int vertexIndex = vertexIndicesMap [x, y];

					// The UVs will be used to apply a texture to the mesh.  Each UV co-ordinate
					// is expressed as a float in the range zero to one, representing how far
					// over the mesh the vertex is
					Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);

					// The topLeft business is so that the mesh is centered
					Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;

					float height = heightMap [x, y];

					// The heights of edge connection vertices need to be adjusted so as to be along
					// the line of the adjacent main triangle edge
					if (isEdgeConnectionVertex) {
						// isVertical is a dirty shortcut so we don't have to calculate separately for
						// x and y axes
						bool isVertical = x == 2 || x == numVertsPerLine - 3;

						int distanceToMainVertexA = ((isVertical ? y - 2 : x - 2) - 2) % skipIncrement;
						int distanceToMainVertexB = skipIncrement - distanceToMainVertexA;
						float distancePercentFromAToB = distanceToMainVertexA / (float)skipIncrement;

						float heightMainVertexA = heightMap [(isVertical ? x : x - distanceToMainVertexA), (isVertical ? y - distanceToMainVertexA : y)];
						float heightMainVertexB = heightMap [(isVertical ? x : x + distanceToMainVertexB), (isVertical ? y + distanceToMainVertexB : y)];

						height = heightMainVertexA * (1 - distancePercentFromAToB) + heightMainVertexB * distancePercentFromAToB;
					}

					meshData.AddVertex (new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

					bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

					if (createTriangle) {
						// Current increment is basically the size of the triangle to draw.  Its
						// skip increment when we draw a big main size triangle and its 1 when we
						// draw a small triangle at the edge of the chunk
						int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;
						int a = vertexIndicesMap [x, y];
						int b = vertexIndicesMap [x + currentIncrement, y];
						int c = vertexIndicesMap [x, y + currentIncrement];
						int d = vertexIndicesMap [x + currentIncrement, y + currentIncrement];
						meshData.AddTriangle (a, d, c);
						meshData.AddTriangle (d, a, b);
					}
				}
			}
		}

		meshData.CompleteTerrainGeneration ();

		return meshData;
	}
}

public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] bakedNormals;

	Vector3[] outOfMeshVertices;
	int [] outOfMeshTriangles;

	int triangleIndex;
	int outOfMeshTriangleIndex;

	bool useFlatShading;

	public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading) {
		this.useFlatShading = useFlatShading;

		int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
		int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
		int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
		int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

		vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
		uvs = new Vector2[vertices.Length];
		int numMeshEdgeTriangles = ((numVertsPerLine - 3) * 4 - 4) * 2;
		int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
		triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

		outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];

		// The number of border squares is 4 * number of vertices per line, but
		// here we're talking about the vertices that make up the two triangles
		// per square, i.e. 3 * 2 * 4 * vertices per line => 24 * vertices per line
		//
		// This comment is out of date now that we're doing the out of mesh stuff
		// to get rid of gaps, but maybe it'll still help?!?
		outOfMeshTriangles = new int[((numVertsPerLine - 1) * 4 - 4) * 2 * 3];
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
		if (vertexIndex < 0) {
			// Border vertices have negative indices.  The minus 1 is because
			// the border vertex indices start at -1
			outOfMeshVertices [-vertexIndex - 1] = vertexPosition;
		} else {
			vertices [vertexIndex] = vertexPosition;
			uvs [vertexIndex] = uv;
		}
	}

	public void AddTriangle(int a, int b, int c) {
		// If any of the vertices making up the triangle are border vertices
		if (a < 0 || b < 0 || c < 0) {
			outOfMeshTriangles [outOfMeshTriangleIndex++] = a;
			outOfMeshTriangles [outOfMeshTriangleIndex++] = b;
			outOfMeshTriangles [outOfMeshTriangleIndex++] = c;
		} else {
			triangles [triangleIndex++] = a;
			triangles [triangleIndex++] = b;
			triangles [triangleIndex++] = c;
		}
	}

	// We'll override the default implementation for caluclating normals
	// to get rid of the seams we see at the intersection between two
	// map chunks by incorrect normals and therefore discFontinuities in
	// lighting.  The issue is that normals are calculated at each vertex
	// of a mesh by combining the normals of the faces surrounding the vertex.
	// But for vertices on the edge of a chunk, it doesn't know about all the
	// faces around it so it ends up wrong by the default implementation.
	Vector3[] CalculateNormals() {
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;

		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles [normalTriangleIndex];
			int vertexIndexB = triangles [normalTriangleIndex + 1];
			int vertexIndexC = triangles [normalTriangleIndex + 2];

			// Calculate the sorface normal for the surface (triangle) formed by the
			// the vertices
			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);

			// Add the calculated normal to the vertex normals at each of the points
			vertexNormals [vertexIndexA] += triangleNormal;
			vertexNormals [vertexIndexB] += triangleNormal;
			vertexNormals [vertexIndexC] += triangleNormal;
		}

		for (int i = 0; i < outOfMeshTriangles.Length / 3; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = outOfMeshTriangles [normalTriangleIndex];
			int vertexIndexB = outOfMeshTriangles [normalTriangleIndex + 1];
			int vertexIndexC = outOfMeshTriangles [normalTriangleIndex + 2];

			// Calculate the sorface normal for the surface (triangle) formed by the
			// the vertices
			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);

			// Add the calculated normal to the vertex normals at each of the points
			if (vertexIndexA >= 0) {
				vertexNormals [vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals [vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) {
				vertexNormals [vertexIndexC] += triangleNormal;
			}
		}

		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals [i].Normalize ();
		}

		return vertexNormals;
	}

	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = indexA < 0 ? outOfMeshVertices[-indexA - 1] : vertices [indexA];
		Vector3 pointB = indexB < 0 ? outOfMeshVertices[-indexB - 1] : vertices [indexB];
		Vector3 pointC = indexC < 0 ? outOfMeshVertices[-indexC - 1] : vertices [indexC];

		// Calculate the vectors of two of the sides of the triangle formed by
		// the 3 supplied points, so we can get the cross product of the two
		// sides, which is the normal vector
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;

		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public void CompleteTerrainGeneration() {
		if (useFlatShading) {
			FlatShading();
		} else {
			// No need to use our own calculate normals method if using flat
			// shading because with flat shading, each triangle and its normals
			// are completely independent of its neighbours
			BakeNormals();
		}	
	}

	void BakeNormals() {
		bakedNormals = CalculateNormals ();
	}

	/// <summary>
	/// For flat shading, we need a new set of vertices where no vertex is part of
	/// more than one triangle.  In this way each triangle can have its own set of
	/// normals that are independent of its neighbours' normals.
	/// </summary>
	void FlatShading() {
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++) {
			
			flatShadedVertices [i] = vertices [triangles [i]];
			flatShadedUvs [i] = uvs [triangles [i]];
			triangles [i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

		if (useFlatShading) {
			mesh.RecalculateNormals ();
		} else {
			mesh.normals = bakedNormals;
		}

		return mesh;
	}
}