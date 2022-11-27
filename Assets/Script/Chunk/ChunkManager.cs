using System;
using System.Collections.Generic;
using UnityEngine;


public class ChunkManager : MonoBehaviour
{
    static public ChunkManager inst;

    Dictionary<Vector3Int, Chunk> dico = new Dictionary<Vector3Int, Chunk>();

    [SerializeField] float chunkWorldSize = 10;
    [SerializeField] int pointsPerAxis = 20;
    [SerializeField, Range(0, 1)] float surfaceLevel = 0.5f;
    [SerializeField] bool lerp = true;
    [SerializeField, Range(0, 1)] float gizmoPointSize = 0f;
    [SerializeField] Material material;

    bool resetChunks;

    Chunk GetChunk(int x, int y, int z) => GetChunk(new Vector3Int(x, y, z));
    Chunk GetChunk(Vector3Int dicoPos)
    {
        if (dico.TryGetValue(dicoPos, out Chunk chunk))
            return chunk;

        // create new chunk
        chunk = new GameObject().AddComponent<Chunk>();
        chunk.transform.parent = transform;
        chunk.name = $"Chunk ({dicoPos.x} {dicoPos.y} {dicoPos.z})";
        chunk.transform.position = ChunkToWorld(dicoPos);
        chunk.GetComponent<MeshRenderer>().material = material;
        chunk.PointsPerAxis = pointsPerAxis;
        chunk.WorldSize = chunkWorldSize;
        chunk.SurfaceLevel = surfaceLevel;
        chunk.Lerp = lerp;
        chunk.GizmoPointSize = gizmoPointSize;

        // add it to dico
        dico[dicoPos] = chunk;

        return chunk;
    }



    Vector3 ChunkToWorld(Vector3Int chunkPos)
        => (Vector3)chunkPos * chunkWorldSize; 

    Vector3 WorldToChunk(Vector3 worldPos)
        => worldPos / chunkWorldSize;

    Vector3Int WorldToChunkFloor(Vector3 worldPos)
    {
        worldPos /= chunkWorldSize; 

        return new Vector3Int(Mathf.FloorToInt(worldPos.x),
                              Mathf.FloorToInt(worldPos.y),
                              Mathf.FloorToInt(worldPos.z));
    }


    public (Vector3Int start, Vector3Int end) ChunksInBox(Vector3 center, Vector3 size)
    {
        center /= chunkWorldSize;
        size /= chunkWorldSize;
        size /= 2;

        int l = Mathf.FloorToInt(center.x - size.x);
        int d = Mathf.FloorToInt(center.y - size.y);
        int b = Mathf.FloorToInt(center.z - size.z);
        int r = Mathf.CeilToInt (center.x + size.x);
        int u = Mathf.CeilToInt (center.y + size.y);
        int f = Mathf.CeilToInt (center.z + size.z);

        return (new Vector3Int(l, d, b), new Vector3Int(r, u, f));
    }

    public void ForChunksInBox(Vector3 center, Vector3 size, Action<Chunk> action)
    {
        Vector3Int start, end;
        (start, end) = ChunksInBox(center, size);

        for (int x = start.x; x < end.x; x++)
            for (int y = start.y; y < end.y; y++)
                for (int z = start.z; z < end.z; z++)
                    action.Invoke(GetChunk(x, y, z));
    }




    void Awake()
    {
        inst = this;
    }

    void OnValidate()
    {
        chunkWorldSize = Mathf.Max(chunkWorldSize, 1);
        pointsPerAxis = Mathf.Max(pointsPerAxis, 2);
        resetChunks = true;
    }

    void Update()
    {
        if (resetChunks)
        {
            ResetChunks();
            resetChunks = false;
        }
    }

    void LateUpdate()
    {
        RemoveUselessChunks();
    }


    void ResetChunks()
    {
        ClearDico();
        Object.ClearAllObjectsHistory();
        Object.AddAllObjectsToChunks();
    }

    void ClearDico()
    {
        foreach (KeyValuePair<Vector3Int, Chunk> e in dico)
            Destroy(e.Value.gameObject);

        dico.Clear();
    }





    void RemoveUselessChunks()
    {
        List<Vector3Int> uselessChunksIdx = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, Chunk> e in dico)
            if (ChunkIsUseless(e.Value))
                uselessChunksIdx.Add(e.Key);

        foreach (Vector3Int idx in uselessChunksIdx)
        {
            Destroy(dico[idx].gameObject);
            dico.Remove(idx);
        }
    }

    static bool ChunkIsUseless(Chunk chunk)
    {
        foreach (Object obj in Object.setEnable)
            if (obj.AddHistory.ContainsKey(chunk))
                return false;

        return true;
    }
}
