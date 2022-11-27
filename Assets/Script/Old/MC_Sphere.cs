using UnityEngine;



public class MC_Sphere : MC_Object
{
    [SerializeField] float radius = 1;
    float radius2; // radius * radius
    float radiusMarge; // radius + marge
    float radiusMarge2;



    protected override void DrawCut(float t)
    {
        GizmosExtension.DrawSphereCircle(transform.position, Mathf.Lerp(radius, radiusMarge, t));
    }

    protected override void Cache()
    {
        base.Cache();

        radius = Mathf.Max(radius, 0);
        radius2 = radius * radius;
        radiusMarge = radius + marge;
        radiusMarge2 = radiusMarge * radiusMarge;
    }

    protected override float Height(Vector3 pos)
    {
        Vector3 toPos = pos - transform.position;
        float dist2 = toPos.sqrMagnitude;

        if (dist2 >= radiusMarge2)
            return 0;

        else if (dist2 <= radius2)
            return fillCurve0;

        float dist = Mathf.Sqrt(dist2);
        float t = Mathf.InverseLerp(radius, radiusMarge, dist);
        return FillHeight(t);
    }

    protected override (Vector3 center, Vector3 size) BoundingBox() => (transform.position, Vector3.one * radiusMarge * 2);
}
