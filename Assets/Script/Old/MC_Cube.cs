using UnityEngine;



public class MC_Cube : MC_Object
{
    [SerializeField] Vector3 size = new Vector3(2, 2, 2);
    Vector3 size2; // sizeStart / 2
    Vector3 sizeMarge; // size + marge * 2
    Vector3 sizeMarge2;



    protected override void DrawCut(float t)
    {
        GizmosExtension.DrawCube(transform.position, transform.rotation, Vector3.Lerp(size, sizeMarge, t), true);
    }

    protected override void Cache()
    {
        base.Cache();

        size.x = Mathf.Max(size.x, 0);
        size.y = Mathf.Max(size.y, 0);
        size.z = Mathf.Max(size.z, 0);

        size2 = size / 2;
        sizeMarge = size + marge * 2 * Vector3.one;
        sizeMarge2 = sizeMarge / 2;
    }

    protected override (Vector3 center, Vector3 size) BoundingBox()
    {
        float x = Mathf.Abs(transform.right.x * sizeMarge.x) + Mathf.Abs(transform.up.x * sizeMarge.y) + Mathf.Abs(transform.forward.x * sizeMarge.z);
        float y = Mathf.Abs(transform.right.y * sizeMarge.x) + Mathf.Abs(transform.up.y * sizeMarge.y) + Mathf.Abs(transform.forward.y * sizeMarge.z);
        float z = Mathf.Abs(transform.right.z * sizeMarge.x) + Mathf.Abs(transform.up.z * sizeMarge.y) + Mathf.Abs(transform.forward.z * sizeMarge.z);
        return (transform.position, new Vector3(x, y, z));
    }

    protected override float Height(Vector3 pos)
    {
        Vector3 localPos = WorldToLocal(pos, transform);

        float x = Mathf.Abs(localPos.x);
        float y = Mathf.Abs(localPos.y);
        float z = Mathf.Abs(localPos.z);

        if (x >= sizeMarge2.x ||
            y >= sizeMarge2.y ||
            z >= sizeMarge2.z)
            return 0;

        else if (x <= size2.x &&
                 y <= size2.y &&
                 z <= size2.z)
            return fillCurve0;

        float tx = Mathf.InverseLerp(size2.x, sizeMarge2.x, x);
        float ty = Mathf.InverseLerp(size2.y, sizeMarge2.y, y);
        float tz = Mathf.InverseLerp(size2.z, sizeMarge2.z, z);

        return FillHeight(Mathf.Max(tx, ty, tz));
    }


    static public Vector3 WorldToLocal(Vector3 pos, Vector3 refPos, Quaternion refRot)
        => Quaternion.Inverse(refRot) * (pos - refPos);

    static public Vector3 LocalToWorld(Vector3 pos, Vector3 refPos, Quaternion refRot)
        => refRot * pos + refPos;

    static public Vector3 WorldToLocal(Vector3 pos, Transform refTrans) => WorldToLocal(pos, refTrans.position, refTrans.rotation);
    static public Vector3 LocalToWorld(Vector3 pos, Transform refTrans) => LocalToWorld(pos, refTrans.position, refTrans.rotation);
}
