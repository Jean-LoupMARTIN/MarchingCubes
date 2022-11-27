using UnityEngine;



public static class MarchingCubes
{
    static public readonly int threadsPerAxis = 8;


    static ComputeShader ComputeShader => computeShader ??= Resources.Load<ComputeShader>("MarchingCubes");
    static ComputeShader computeShader;


    static public Mesh GenerateMesh(float[] points, float worldSize, bool lerp = true, float surfaceLevel = 0.5f)
    {
        Mesh mesh = new Mesh();
        GenerateMesh(mesh, points, worldSize, lerp, surfaceLevel);
        return mesh;
    }

    static public void GenerateMesh(Mesh mesh, float[] points, float worldSize, bool lerp = true, float surfaceLevel = 0.5f)
    {
        int pntPerAxis = (int)Mathf.Pow(points.Length, 1 / 3f);

        // return if points is not a cube
        if (pntPerAxis * pntPerAxis * pntPerAxis != points.Length)
        {
            Debug.Log($"points is not a cube : pntPerAxis = {pntPerAxis}   points.Length = {points.Length}");
            return;
        }

        // return if not enough points
        if (pntPerAxis < 2)
        {
            Debug.Log($"not enough points : pntPerAxis (={pntPerAxis}) < 2");
            return;
        }

        int cubePerAxis = pntPerAxis - 1;
        int threadGroups = Mathf.CeilToInt((float)cubePerAxis / threadsPerAxis);
        float cubeSize = worldSize / cubePerAxis;
        int cubeCount = cubePerAxis * cubePerAxis * cubePerAxis;
        int maxTriangles = cubeCount * 5;
 
        ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, sizeof(float));
        pointsBuffer.SetData(points);

        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangles, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        ComputeShader.SetBuffer(0, "points", pointsBuffer);
        ComputeShader.SetBuffer(0, "triangles", trianglesBuffer);
        ComputeShader.SetInt("pntPerAxis", pntPerAxis);
        ComputeShader.SetInt("idxMax", pntPerAxis - 2);
        ComputeShader.SetInt("pntPerAxis2", pntPerAxis * pntPerAxis);
        ComputeShader.SetFloat("cubeSize", cubeSize);
        ComputeShader.SetFloat("surfaceLevel", surfaceLevel);
        ComputeShader.SetBool("lerp", lerp);

        ComputeShader.Dispatch(0, threadGroups, threadGroups, threadGroups);

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
