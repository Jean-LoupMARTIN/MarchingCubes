using System;
using UnityEngine;



[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    static public readonly Color boxColor = new Color(1, 1, 1, 0.1f);
    static public readonly Color positifPointColor = Color.white;
    static public readonly Color negatifPointColor = Color.red;

    [SerializeField] float worldSize = 10;

    [SerializeField] int pointsPerAxis = 10;
    int pointsPerAxis2; // pointsPerAxis ^ 2
    int pointsCount; // pointsPerAxis ^ 3
    int cubesPerAxis; // pointsPerAxis - 1
    int cubesCount; // cubesPerAxis ^ 3
    float cubeSize; // worldSize / cubesPerAxis

    [SerializeField, Range(0, 1)] float surfaceLevel = 0.5f;
    [SerializeField] bool lerp = true;

    [SerializeField, Range(0, 1)] float gizmoPointSize = 0.1f;

    float[] points;
    Mesh mesh;
    bool updateMesh = false;

    #region Getters/Setters



    public float[] Points
    {
        get => points;

        set {
            if (value.Length != pointsCount)
            {
                print($"Set points has been canceled because value.Length ({value.Length}) != pointsCount ({pointsCount})");
                return;
            }

            points = value;
            updateMesh = true;
        }
    }

    public float WorldSize
    {
        set {
            worldSize = Mathf.Max(value, 0);
            worldSize = value;
            cubeSize = worldSize / cubesPerAxis;
            updateMesh = true;
        }
    }

    public int PointsPerAxis
    {
        get => pointsPerAxis;

        set {
            pointsPerAxis = Mathf.Max(value, 2);
            pointsPerAxis2 = pointsPerAxis * pointsPerAxis;
            pointsCount = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            cubesPerAxis = pointsPerAxis - 1;
            cubesCount = cubesPerAxis * cubesPerAxis * cubesPerAxis;
            cubeSize = worldSize / cubesPerAxis;

            points = new float[pointsCount];

            updateMesh = true;
        }
    }

    public float SurfaceLevel
    {
        set {
            surfaceLevel = Mathf.Clamp01(value);
            updateMesh = true;
        }
    }

    public bool Lerp
    {
        set {
            lerp = value;
            updateMesh = true;
        }
    }

    public float GizmoPointSize { set => gizmoPointSize = Mathf.Clamp01(value); }
    public int PointsPerAxis2 { get => pointsPerAxis2; }
    public int CubesPerAxis { get => cubesPerAxis; }
    public float CubeSize { get => cubeSize; }


    Vector3 PointToWorld(Vector3Int pointPos)
        => (Vector3)pointPos * cubeSize;

    Vector3 WorldToPoint(Vector3 worldPos)
        => worldPos / cubeSize;

    Vector3Int WorldToPointFloor(Vector3 worldPos)
    {
        worldPos /= cubeSize;

        return new Vector3Int(Mathf.FloorToInt(worldPos.x),
                              Mathf.FloorToInt(worldPos.y),
                              Mathf.FloorToInt(worldPos.z));
    }


    int Idx(int x, int y, int z) => x + pointsPerAxis * y + pointsPerAxis2 * z;

    float GetPoint(int x, int y, int z) => points[Idx(x, y, z)];

    void ForPoints(Action<int, int, int> action)
    {
        for (int x = 0; x < pointsPerAxis; x++)
            for (int y = 0; y < pointsPerAxis; y++)
                for (int z = 0; z < pointsPerAxis; z++)
                    action.Invoke(x, y, z);
    }

    static public Color ColorAtHeight(float height)
    {
        Color c = height >= 0 ? positifPointColor : negatifPointColor;
        c.a = Mathf.Min(Mathf.Abs(height), 1);
        return c;
    }


    public (Vector3Int start, Vector3Int end) PointsInBox(Vector3 center, Vector3 size)
    {
        center -= transform.position;
        center /= cubeSize;
        size /= cubeSize;
        size /= 2;

        int l = Mathf.CeilToInt (center.x - size.x);
        int d = Mathf.CeilToInt (center.y - size.y);
        int b = Mathf.CeilToInt (center.z - size.z);
        int r = Mathf.FloorToInt(center.x + size.x);
        int u = Mathf.FloorToInt(center.y + size.y);
        int f = Mathf.FloorToInt(center.z + size.z);

        l = Mathf.Clamp(l, 0, pointsPerAxis);
        d = Mathf.Clamp(d, 0, pointsPerAxis);
        b = Mathf.Clamp(b, 0, pointsPerAxis);
        r = Mathf.Clamp(r, -1, cubesPerAxis);
        u = Mathf.Clamp(u, -1, cubesPerAxis);
        f = Mathf.Clamp(f, -1, cubesPerAxis);

        return (new Vector3Int(l, d, b), new Vector3Int(r, u, f));
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        DrawBox();
        DrawPoints();
    }

    void DrawBox()
    {
        Gizmos.color = boxColor;
        Vector3 worldSize3 = worldSize * Vector3.one;
        Gizmos.DrawWireCube(transform.position + worldSize3 / 2, worldSize3);
    }

    void DrawPoints()
    {
        if (gizmoPointSize == 0)
            return;

        float pointSize = gizmoPointSize * cubeSize;

        ForPoints((x, y, z) =>
        {
            float height = GetPoint(x, y, z);

            if (height == 0)
                return;

            Gizmos.color = ColorAtHeight(height);
            GizmosExtension.DrawPoint(transform.position + PointToWorld(new Vector3Int(x, y, z)), pointSize);
        });
    }

    #endregion


    void OnValidate()
    {
        PointsPerAxis = pointsPerAxis;
        WorldSize = worldSize;
        updateMesh = true;
    }

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (updateMesh)
        {
            MarchingCubes.GenerateMesh(mesh, points, worldSize, lerp, surfaceLevel);
            updateMesh = false;
        }
    }
}
