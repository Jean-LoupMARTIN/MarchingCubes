using UnityEngine;



public static class MarchingCubes
{
    static public readonly int threadsPerAxis = 8;


    static ComputeShader ComputeShader => computeShader ??= Resources.Load<ComputeShader>("MarchingCubes");
    static ComputeShader computeShader;


    static public Mesh GenerateMesh(float[] points, Vector3Int pointsSize, float pointsDist, bool lerp = true, float surfaceLevel = 0.5f)
    {
        Mesh mesh = new Mesh();
        GenerateMesh(mesh, points, pointsSize, pointsDist, lerp, surfaceLevel);
        return mesh;
    }

    static public void GenerateMesh(Mesh mesh, float[] points, Vector3Int pointsSize, float pointsDist, bool lerp = true, float surfaceLevel = 0.5f)
    {
        // return if points is not a cube
        if (points.Length != pointsSize.x * pointsSize.y * pointsSize.z)
        {
            Debug.Log($"points.Length {points.Length} don't match pointsSize {pointsSize}");
            return;
        }

        // return if not enough points
        if (pointsSize.x < 2 || pointsSize.y < 2 || pointsSize.z < 2)
        {
            Debug.Log($"Not enough points : pointsSize {pointsSize}");
            return;
        }

        Vector3Int cubesSize = pointsSize - Vector3Int.one;

        Vector3Int threadGroups = new Vector3Int(
            Mathf.CeilToInt((float)cubesSize.x / threadsPerAxis),
            Mathf.CeilToInt((float)cubesSize.y / threadsPerAxis),
            Mathf.CeilToInt((float)cubesSize.z / threadsPerAxis));

        int cubeCount = cubesSize.x * cubesSize.y * cubesSize.z;
        int maxTriangles = cubeCount * 5;
 
        ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, sizeof(float));
        pointsBuffer.SetData(points);

        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangles, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        ComputeShader.SetBuffer(0, "points", pointsBuffer);
        ComputeShader.SetBuffer(0, "triangles", trianglesBuffer);
        ComputeShader.SetInt("pointsX", pointsSize.x);
        ComputeShader.SetInt("pointsXY", pointsSize.x * pointsSize.y);
        ComputeShader.SetInts("idxMax", new int[] { pointsSize.x - 2, pointsSize.y - 2 , pointsSize.z - 2});
        ComputeShader.SetFloat("pointsDist", pointsDist);
        ComputeShader.SetFloat("surfaceLevel", surfaceLevel);
        ComputeShader.SetBool("lerp", lerp);

        ComputeShader.Dispatch(0, threadGroups.x, threadGroups.y, threadGroups.z);

        // Get number of triangles in the triangle buffer
        ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(trianglesBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int trianglesCount = triCountArray[0];

        // Get triangle data from shader
        Triangle[] triangles = new Triangle[trianglesCount];
        trianglesBuffer.GetData(triangles, 0, 0, trianglesCount);

        pointsBuffer.Dispose();
        trianglesBuffer.Dispose();
        triCountBuffer.Dispose();

        Vector3[] vertices = new Vector3[trianglesCount * 3];
        int[] meshTriangles = new int[trianglesCount * 3];

        for (int i = 0; i < trianglesCount; i++) {
            for (int j = 0; j < 3; j++)
            {
                int idx = i * 3 + j;
                meshTriangles[idx] = idx;
                vertices[idx] = triangles[i][j];
            }
        }

        mesh.Clear();

        if (triangles.Length > 0)
        {
            mesh.vertices = vertices;
            mesh.triangles = meshTriangles;
            mesh.RecalculateNormals();
        }
    }
    

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this [int i]
        {
            get {
                switch (i)
                {
                    case 0: return a;
                    case 1: return b;
                    default:return c;
                }
            }
        }
    }
}
