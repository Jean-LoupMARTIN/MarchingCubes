using System;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public abstract class MC_Object : MonoBehaviour
{
    static public List<MC_Object> list = new List<MC_Object>();


    [SerializeField] protected float marge = 1;

    [SerializeField] float fillCurveHeight = 1;
    [SerializeField] AnimationCurve fillCurve;
    protected float fillCurve0;

    [SerializeField, Range(0, 20)] int gizmoCutCount = 10;

    // track transform changed
    Vector3 lastPos;
    Quaternion lastRot;
    Vector3 lastScale;

    List<(Vector3Int pointPos, float add)> addHistory = new List<(Vector3Int, float)>();
    public void ClearAddHistory() => addHistory.Clear();


    public float FillHeight(float t) => fillCurveHeight * fillCurve.Evaluate(t);





    protected virtual void OnValidate()
    {
        Cache();

        if (enabled)
            UpdateChunks();
    }

    protected virtual void Cache()
    {
        marge = Mathf.Max(marge, 0);
        fillCurve0 = FillHeight(0);
    }




    
    void Update()
    {
        if (TransformHasChanged())
        {
            CacheTransform();
            UpdateChunks();
        }
    }

    bool TransformHasChanged()
    {
        return transform.position   != lastPos ||
               transform.rotation   != lastRot ||
               transform.lossyScale != lastScale;
    }

    void CacheTransform()
    {
        lastPos   = transform.position;
        lastRot   = transform.rotation;
        lastScale = transform.lossyScale;
    }





    void OnEnable()
    {
        list.Add(this);
        CacheTransform();
        UpdateChunks();
    }


    void OnDisable()
    {
        RemoveAddHistory();
        list.Remove(this);
    }






    protected virtual void OnDrawGizmosSelected()
    {
        DrawBoundingBox();
        DrawCut();
    }

    void DrawBoundingBox()
    {
        Gizmos.color = Color.yellow;
        (Vector3 center, Vector3 size) bc = BoundingBox();
        Gizmos.DrawWireCube(bc.center, bc.size);
    }



    void DrawCut()
    {
        Color c = fillCurve0 >= 0 ? Color.white : Color.red;
        c.a = Mathf.Abs(fillCurve0);
        Gizmos.color = c;
        DrawCut(0);

        Gizmos.color = Color.yellow;
        DrawCut(1);

        for (float i = 0; i < gizmoCutCount; i++)
        {
            float t = (i + 1) / (gizmoCutCount + 1);
            float h = FillHeight(t);

            c = h >= 0 ? Color.white : Color.red;
            c.a = Mathf.Abs(h);
            Gizmos.color = c;

            DrawCut(t);
        }
    }




    public void UpdateChunks()
    {
        RemoveAddHistory();
        AddToChunks();
    }

    public void AddToChunks()
    {
        Vector3 center;
        Vector3 size;
        (center, size) = BoundingBox();

        ChunkOld.PointsInBox(center, size, (pointPos, worldPos) =>
        {
            float height = Height(worldPos);
            ChunkOld.PointAdd(pointPos, height);
            addHistory.Add((pointPos, height));
        });
    }

    void RemoveAddHistory()
    {
        foreach ((Vector3Int pointPos, float height) e in addHistory)
            ChunkOld.PointAdd(e.pointPos, -e.height);

        addHistory.Clear();
    }


    protected abstract void DrawCut(float t);
    protected abstract float Height(Vector3 pos);
    protected abstract (Vector3 center, Vector3 size) BoundingBox();
}
