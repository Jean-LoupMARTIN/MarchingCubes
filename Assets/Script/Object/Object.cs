using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public abstract class Object : MonoBehaviour
{
    static public readonly Color boudingBoxColor = new Color(1, 1, 0, 0.2f);

    static public HashSet<Object> setEnable = new HashSet<Object>();

    [SerializeField] protected float marge = 2;
    [SerializeField] float fill = 1;
    [SerializeField] FillFunction fillFunction = FillFunction.Square;
    enum FillFunction { Linear, Square, SquareRoot, Chevron, ChevronSquare }
    [SerializeField, Range(0, 10)] int gizmoCut = 5;

    protected ComputeShader CShaderAdd;
    ComputeShader CShaderRemoveHistory;

    protected bool updateChunks;

    // track transform
    Vector3 lastPos;
    Quaternion lastRot;
    Vector3 lastScale;

    Dictionary<Chunk, PointAdd[]> addHistory = new Dictionary<Chunk, PointAdd[]>();

    public struct PointAdd
    {
        public int idx;
        public float height;
    };

    #region Getters/Setters

    public Dictionary<Chunk, PointAdd[]> AddHistory { get => addHistory; }


    static float Fill(FillFunction function, float t)
    {
        switch (function)
        {
            case FillFunction.Linear:        return t;
            case FillFunction.Square:        return t * t;
            case FillFunction.SquareRoot:    return Mathf.Sqrt(t);
            case FillFunction.Chevron:       return 1 - Mathf.Abs(1 - t * 2);
            case FillFunction.ChevronSquare:
                float chevron = 1 - Mathf.Abs(1 - t * 2);
                return chevron * chevron;
        }
        return t;
    }

    static public void ClearAllObjectsHistory()
    {
        foreach (Object obj in setEnable)
            obj.addHistory.Clear();
    }

    static public void AddAllObjectsToChunks()
    {
        foreach (Object obj in setEnable)
            obj.AddToChunks();
    }


    void PrintAddHistory()
    {
        string str = "";

        foreach (KeyValuePair<Chunk, PointAdd[]> e in addHistory)
        {
            str += e.Key.name + "\n\n";

            foreach (PointAdd pntAdd in e.Value)
                str += "height : " + pntAdd.height.ToString("F2") + "\tidx : " + pntAdd.idx + "\n";

            str += "\n\n";
        }

        print(str);
    }

    #endregion



    #region Gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        DrawCut();
        DrawBoundingBox();
    }

    void DrawCut()
    {
        Gizmos.color = Chunk.ColorAtHeight(Fill(fillFunction, 0));
        DrawCut(1);

        Gizmos.color = boudingBoxColor;
        DrawCut(0);

        for (int i = 0; i < gizmoCut; i++)
        {
            float t = (float)(i+1) / (gizmoCut+1);
            float height = fill * Fill(fillFunction, t);
            Gizmos.color = Chunk.ColorAtHeight(height);
            DrawCut(t);
        }
    }



    void DrawBoundingBox()
    {
        (Vector3 center, Vector3 size) boudingBox = BoudingBox();
        Gizmos.color = boudingBoxColor;
        Gizmos.DrawWireCube(boudingBox.center, boudingBox.size);
    }

    #endregion


    protected virtual void OnValidate()
    {
        marge = Mathf.Max(marge, 0);
        updateChunks = true;
    }

    protected virtual void Awake()
    {
        CShaderAdd           = Resources.Load<ComputeShader>(CShaderAddPath);
        CShaderRemoveHistory = Resources.Load<ComputeShader>("RemoveHistory");
    }

    protected virtual void OnEnable()
    {
        setEnable.Add(this);
        updateChunks = true;
    }

    protected virtual void OnDisable()
    {
        setEnable.Remove(this);
        RemoveAddHistory();
    }


    void Update()
    {
        if (TransformHasChanged())
        {
            SaveTransform();
            updateChunks = true;
        }

        if (updateChunks)
        {
            UpdateChunks();
            updateChunks = false;
        }
    }


    bool TransformHasChanged() => transform.position   != lastPos ||
                                  transform.rotation   != lastRot ||
                                  transform.lossyScale != lastScale;


    void SaveTransform()
    {
        lastPos   = transform.position;
        lastRot   = transform.rotation;
        lastScale = transform.lossyScale;
    }

    void UpdateChunks()
    {
        RemoveAddHistory();
        AddToChunks();
    }


    void AddToChunks()
    {
        if (addHistory.Count != 0)
            print($"Object seems to have already been added addHistory.Count = {addHistory.Count}");

        (Vector3 center, Vector3 size) box = BoudingBox();
        ChunkManager.inst.ForChunksInBox(box.center, box.size, chunk => AddToChunk(chunk));
    }


    void RemoveAddHistory()
    {
        if (addHistory.Count == 0)
            return;

        foreach (KeyValuePair<Chunk, PointAdd[]> e in addHistory)
            RemoveHistory(e.Key, e.Value);

        addHistory.Clear();
    }




    void AddToChunk(Chunk chunk)
    {
        if (fill == 0)
            return;

        (Vector3 center, Vector3 size) box = BoudingBox();
        (Vector3Int start, Vector3Int end) pointsInBox = chunk.PointsInBox(box.center, box.size);
        Vector3Int toEnd = pointsInBox.end - pointsInBox.start;

        if (toEnd.x < 0 ||
            toEnd.y < 0 ||
            toEnd.z < 0)
            return;

        float[] points = chunk.Points;

        ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, sizeof(float));
        pointsBuffer.SetData(points);

        ComputeBuffer historyBuffer = new ComputeBuffer(points.Length, sizeof(int) + sizeof(float), ComputeBufferType.Append);

        CShaderAdd.SetBuffer(0, "points", pointsBuffer);
        CShaderAdd.SetBuffer(0, "history", historyBuffer);
        CShaderAdd.SetInt("pntPerAxis", chunk.PointsPerAxis);
        CShaderAdd.SetInt("pntPerAxis2", chunk.PointsPerAxis2);
        CShaderAdd.SetInts("idxStart", new int[] { pointsInBox.start.x, pointsInBox.start.y, pointsInBox.start.z });
        CShaderAdd.SetInts("idxEnd",   new int[] { pointsInBox.end.x,   pointsInBox.end.y,   pointsInBox.end.z });
        CShaderAdd.SetFloat("fill", fill);
        CShaderAdd.SetInt("fillFunction", (int)fillFunction);
        CShaderAddSetParameters(chunk);

        int threadsPerAxis = 8;

        CShaderAdd.Dispatch(
            0,
            Mathf.CeilToInt((float)(toEnd.x + 1) / threadsPerAxis),
            Mathf.CeilToInt((float)(toEnd.y + 1) / threadsPerAxis),
            Mathf.CeilToInt((float)(toEnd.z + 1) / threadsPerAxis));

        CShaderAddDisposeBuffers();

        // get points
        pointsBuffer.GetData(points);
        pointsBuffer.Dispose();
        chunk.Points = points;

        // get historyCount
        ComputeBuffer historyCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(historyBuffer, historyCountBuffer, 0);
        int[] historyCountArray = { 0 };
        historyCountBuffer.GetData(historyCountArray);
        historyCountBuffer.Dispose();
        int historyCount = historyCountArray[0];

        // get history
        if (historyCount > 0)
        {
            PointAdd[] history = new PointAdd[historyCount];
            historyBuffer.GetData(history, 0, 0, historyCount);
            addHistory[chunk] = addHistory.ContainsKey(chunk) ? addHistory[chunk].Concat(history).ToArray() :
                                                                history;
        }
        historyBuffer.Dispose();
    }


    void RemoveHistory(Chunk chunk, PointAdd[] history)
    {
        float[] points = chunk.Points;

        ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, sizeof(float));
        pointsBuffer.SetData(points);

        ComputeBuffer historyBuffer = new ComputeBuffer(history.Length, sizeof(int) + sizeof(float));
        historyBuffer.SetData(history);

        CShaderRemoveHistory.SetBuffer(0, "points", pointsBuffer);
        CShaderRemoveHistory.SetBuffer(0, "history", historyBuffer);
        CShaderRemoveHistory.SetInt("idxMax", history.Length-1);

        int threadsCount = 512;

        CShaderRemoveHistory.Dispatch(0, Mathf.CeilToInt((float)history.Length / threadsCount), 1, 1);

        pointsBuffer.GetData(points);
        pointsBuffer.Dispose();
        chunk.Points = points;

        historyBuffer.Dispose();
    }

    protected abstract void DrawCut(float t);
    protected abstract (Vector3 center, Vector3 size) BoudingBox();
    protected abstract string CShaderAddPath { get; }
    protected abstract void CShaderAddSetParameters(Chunk chunk);
    protected abstract void CShaderAddDisposeBuffers();
}


