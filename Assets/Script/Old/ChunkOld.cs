using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkOld : MonoBehaviour
{
    static public Dictionary<Vector3Int, ChunkOld> dico = new Dictionary<Vector3Int, ChunkOld>();

    static float worldSize;
    static Vector3 worldSize3; // worldSize * Vector.one
    static Vector3 worldSize3Half; // worldSize3 / 2

    static int pntPerAxis;
    static int pntPerAxisSqr; // pntPerAxis^2
    static int pntCount; // pntPerAxis^3

    static int cubePerAxis; // pntPerAxis - 1
    static int cubePerAxisSqr; // cubePerAxis^2
    static int cubeCount; // cubePerAxis^3
    static float cubeWorldSize;

    static float surfaceLevel = 0.5f;

    static ComputeShader computeShader;
    static readonly int threadPerAxis = 8;
    static int threadGroups;
    static int maxTriangles;

    static Material material;

    static float gizmoPntSize;



    float[] points;
    Vector3 chunkPos;
    bool hasChanged = false;


    #region Getters/Setters


    static public float WorldSize
    {
        set {
            worldSize = Mathf.Max(value, 1);
            worldSize3 = worldSize * Vector3.one;
            worldSize3Half = worldSize3 / 2;

            cubeWorldSize = worldSize / (pntPerAxis-1);
        }
    }

    static public int PntPerAxis
    {
        set {
            pntPerAxis = Mathf.Max(value, 2);
            pntPerAxisSqr = pntPerAxis * pntPerAxis;
            pntCount = pntPerAxis * pntPerAxis * pntPerAxis;

            CubePerAxis = pntPerAxis - 1;

            threadGroups = Mathf.CeilToInt(pntPerAxis / (float)threadPerAxis);
        }
    }

    static int CubePerAxis
    {
        set {
            cubePerAxis = value;
            cubePerAxisSqr = cubePerAxis * cubePerAxis;
            cubeCount = cubePerAxis * cubePerAxis * cubePerAxis;
            cubeWorldSize = worldSize / (pntPerAxis - 1);

            maxTriangles = cubeCount * 5;
        }
    }

    static public Material Material { set => material = value; }
    static public ComputeShader ComputeShader { set => computeShader = value; }
    static public float GizmoPntSize { set => gizmoPntSize = value; }
    static public float CubeWorldSize { get => cubeWorldSize; }


    static public Vector3 PointToWorld(Vector3Int pointPos)
        => (Vector3)pointPos * cubeWorldSize;

    static public Vector3Int WorldToPoint(Vector3 worldPos)
    {
        worldPos /= cubeWorldSize;

        return new Vector3Int(Mathf.FloorToInt(worldPos.x),
                              Mathf.FloorToInt(worldPos.y),
                              Mathf.FloorToInt(worldPos.z));
    }

    static public Vector3 ChunkToWorld(Vector3Int chunkPos)
        => (Vector3)chunkPos * worldSize;

    static public Vector3Int WorldToChunk(Vector3 worldPos)
    {
        worldPos /= worldSize;

        return new Vector3Int(Mathf.FloorToInt(worldPos.x),
                              Mathf.FloorToInt(worldPos.y),
                              Mathf.FloorToInt(worldPos.z));
    }


    static int Idx(int x, int y, int z)
        => x + y * pntPerAxis + z * pntPerAxisSqr;

    static int Idx(Vector3Int v)
        => Idx(v.x, v.y, v.z);

    static void ForPoints(Action<int, int, int> action)
    {
        for (int x = 0; x < pntPerAxis; x++)
            for (int y = 0; y < pntPerAxis; y++)
                for (int z = 0; z < pntPerAxis; z++)
                    action.Invoke(x, y, z);
    }


    static public void PointsInBox(Vector3 center, Vector3 size, Action<Vector3Int, Vector3> action)
    {
        center /= cubeWorldSize;
        size /= cubeWorldSize;
        size /= 2;

        int l = Mathf.CeilToInt (center.x - size.x);
        int d = Mathf.CeilToInt (center.y - size.y);
        int b = Mathf.CeilToInt (center.z - size.z);
        int r = Mathf.FloorToInt(center.x + size.x);
        int u = Mathf.FloorToInt(center.y + size.y);
        int f = Mathf.FloorToInt(center.z + size.z);

        for (int x = l; x <= r; x++)
            for (int y = d; y <= u; y++)
                for (int z = b; z <= f; z++)
                    action.Invoke(new Vector3Int(x, y, z), PointToWorld(new Vector3Int(x, y, z)));
    }

    static public void PointAdd(Vector3Int pointPos, float add)
    {
        Vector3 worldPos = PointToWorld(pointPos);
        Vector3Int chunkPos = WorldToChunk(worldPos);

        ChunkOld chunk = GetChunk(chunkPos);

        Vector3Int chunkPointPos = pointPos - WorldToPoint(ChunkToWorld(chunkPos));

        chunk.ChunkPointAdd(chunkPointPos, add);

        // TODO opti
        if (chunkPointPos.x == 0) GetChunk(chunkPos + Vector3Int.left).ChunkPointAdd(chunkPointPos + Vector3Int.right   * cubePerAxis, add);
        if (chunkPointPos.y == 0) GetChunk(chunkPos + Vector3Int.down).ChunkPointAdd(chunkPointPos + Vector3Int.up      * cubePerAxis, add);
        if (chunkPointPos.z == 0) GetChunk(chunkPos + Vector3Int.back).ChunkPointAdd(chunkPointPos + Vector3Int.forward * cubePerAxis, add);

        if (chunkPointPos.x == 0 && chunkPointPos.y == 0) GetChunk(chunkPos + Vector3Int.left + Vector3Int.down).ChunkPointAdd(chunkPointPos + (Vector3Int.right + Vector3Int.up)      * cubePerAxis, add);
        if (chunkPointPos.x == 0 && chunkPointPos.z == 0) GetChunk(chunkPos + Vector3Int.left + Vector3Int.back).ChunkPointAdd(chunkPointPos + (Vector3Int.right + Vector3Int.forward) * cubePerAxis, add);
        if (chunkPointPos.y == 0 && chunkPointPos.z == 0) GetChunk(chunkPos + Vector3Int.down + Vector3Int.back).ChunkPointAdd(chunkPointPos + (Vector3Int.up    + Vector3Int.forward) * cubePerAxis, add);
    }


    static public ChunkOld GetChunk(Vector3Int chunkPos)
    {
        if (dico.TryGetValue(chunkPos, out ChunkOld chunk))
            return chunk;

        chunk = new GameObject().AddComponent<ChunkOld>();
        dico[chunkPos] = chunk;
        chunk.Init(chunkPos);

        return chunk;
    }







    float ChunkPoint(Vector3Int pointPos)
        => points[Idx(pointPos)];

    void ChunkPointAdd(Vector3Int pointPos, float add)
    {
        points[Idx(pointPos)] += add;
        hasChanged = true;
    }

    #endregion


    #region Gizmos

    void OnDrawGizmos()
    {
        DrawWorldSize();
        DrawPoints();
    }

    void DrawWorldSize()
    {
        Gizmos.DrawWireCube(transform.position + worldSize3Half, worldSize3);
    }

    void DrawPoints()
    {
        if (gizmoPntSize > 0)
            ForPoints((x, y, z) => DrawPoint(new Vector3Int(x, y, z)));
    }

    void DrawPoint(Vector3Int pointPos)
    {
        float height = ChunkPoint(pointPos);

        if (height == 0)
            return;

        Color c = height > 0 ? Color.white : Color.red;
        c.a = Mathf.Min(Mathf.Abs(height), 1);
        Gizmos.color = c;

        GizmosExtension.DrawPoint(transform.position + PointToWorld(pointPos), gizmoPntSize);
    }

    #endregion



    void Init(Vector3Int chunkPos)
    {
        name = $"Chunk ({chunkPos.x} {chunkPos.y} {chunkPos.z})";
        transform.position = ChunkToWorld(chunkPos);
        this.chunkPos = chunkPos;
        points = new float[pntCount];
    }

    void Update()
    {
        if (hasChanged)
        {
            OnChanged();
            hasChanged = false;
        }
    }

    void OnChanged()
    {
        //GenerateMesh();
    }



    static public void ResetAllChunks()
    {
        DestroyAllChunks();
        AddAllObjects();
    }

    static void DestroyAllChunks()
    {
        foreach (KeyValuePair<Vector3Int, ChunkOld> e in dico)
            if (e.Value)
                DestroyImmediate(e.Value.gameObject);

        foreach (MC_Object obj in MC_Object.list)
            obj.ClearAddHistory();

        dico.Clear();
    }

    static void AddAllObjects()
    {
        foreach (MC_Object obj in MC_Object.list)
            obj.AddToChunks();
    }
}
