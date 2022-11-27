using UnityEngine;




[RequireComponent(typeof(BezierCurveVolume))]
public class BezierCurveObj : Object
{
    BezierCurveVolume bezierCurve;
    ComputeBuffer pointsBuffer;

    protected override void Awake()
    {
        base.Awake();
        bezierCurve = GetComponent<BezierCurveVolume>();
        bezierCurve.onChanged.AddListener(() => updateChunks = true);
    }

    protected override string CShaderAddPath => "AddCurve";

    protected override (Vector3 center, Vector3 size) BoudingBox()
    {
        (Vector3 center, Vector3 size) box = bezierCurve.BoundingBox();
        return (box.center, box.size + marge * 2 * Vector3.one);
    }


    protected override void CShaderAddSetParameters(Chunk chunk)
    {
        CurvePoint[] points = FormatCurvePoints(bezierCurve.Points, chunk);
        pointsBuffer = new ComputeBuffer(points.Length, sizeof(float) * 4);
        pointsBuffer.SetData(points);

        CShaderAdd.SetBuffer(0, "curvePoints", pointsBuffer);
        CShaderAdd.SetInt("curvePointsCount", points.Length);
        CShaderAdd.SetFloat("radiusMarge", marge / chunk.CubeSize);
    }

    protected override void CShaderAddDisposeBuffers()
    {
        pointsBuffer.Dispose();
    }

    struct CurvePoint
    {
        public Vector3 pos;
        public float radius;
    }

    CurvePoint[] FormatCurvePoints((Vector3 pos, float radius)[] points, Chunk chunk)
    {
        CurvePoint[] curvePoints = new CurvePoint[points.Length];

        for (int i = 0; i < curvePoints.Length; i++)
        {
            CurvePoint pnt = new CurvePoint();
            pnt.pos = (points[i].pos - chunk.transform.position) / chunk.CubeSize;
            pnt.radius = points[i].radius / chunk.CubeSize;
            curvePoints[i] = pnt;
        }

        return curvePoints;
    }

    protected override void OnDrawGizmosSelected()
    {
        bezierCurve = GetComponent<BezierCurveVolume>();
        base.OnDrawGizmosSelected();
    }

    protected override void DrawCut(float t)
    {
        bezierCurve.DrawRadius(marge * (1-t));
    }
}
