using System;
using UnityEngine;



[ExecuteInEditMode]
public class ChunkManagerOld : MonoBehaviour
{
    static ChunkManager inst;
    static public ChunkManager Inst => inst ??= FindObjectOfType<ChunkManager>(true);

    [SerializeField] float worldSize = 16;
    [SerializeField] int pointPerAxis = 16;
    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField, Range(0, 1)] float gizmoPointSize = 0.1f;

    bool hasChanged = false;

    void OnValidate()
    {
        worldSize = Mathf.Max(worldSize, 10);
        pointPerAxis = Mathf.Max(pointPerAxis, 2);

        ChunkOld.WorldSize = worldSize;
        ChunkOld.PntPerAxis = pointPerAxis;
        ChunkOld.Material = material;
        ChunkOld.ComputeShader = computeShader;
        ChunkOld.GizmoPntSize = gizmoPointSize * ChunkOld.CubeWorldSize;

        hasChanged = true;
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
        ChunkOld.ResetAllChunks();
    }
}
