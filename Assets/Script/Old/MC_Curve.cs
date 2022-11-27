using UnityEngine;

[RequireComponent(typeof(BezierCurveVolume))]
public class MC_Curve : MC_Object
{
    BezierCurveVolume bezierCurve;


    protected override void Cache()
    {
        base.Cache();

        bezierCurve = GetComponent<BezierCurveVolume>();
        bezierCurve.onChanged.RemoveListener(UpdateChunks);
        bezierCurve.onChanged.AddListener(UpdateChunks);
    }

    protected override void DrawCut(float t)
    {
        bezierCurve.DrawRadius(marge * t);
    }


    protected override float Height(Vector3 pos)
    {
        return 0;
        /*
        return FillHeight(bezierCurve.MargeTime(pos, marge));

        float distToSrf = bezierCurve.DistToSurface(pos);

        if (distToSrf >= marge)
            return 0;

        else if (distToSrf <= 0)
            return fillCurve0;

        else return FillHeight(Mathf.InverseLerp(0, marge, distToSrf));
        */
    }

    protected override (Vector3 center, Vector3 size) BoundingBox()
    {
        Vector3 center, size;
        (center, size) = bezierCurve.BoundingBox();
        return (center, size + Vector3.one * marge * 2);
    }
}
