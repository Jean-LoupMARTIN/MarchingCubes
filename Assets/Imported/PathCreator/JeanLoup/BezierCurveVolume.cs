using System.Collections.Generic;
using PathCreation;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PathCreator))]
public class BezierCurveVolume : MonoBehaviour
{
    [SerializeField] float radius = 1;
    [SerializeField] AnimationCurve radiusCurve;
    [SerializeField] int nbPoints = 10;
    (Vector3 pos, float radius)[] points;

    [SerializeField] bool drawCircle = true;
    [SerializeField] bool drawLine = true;

    PathCreator pathCreator;
    Vector3[] lastPathPoints = new Vector3[0];

    public UnityEvent onChanged = new UnityEvent();



    public float Radius(float t) => radius * radiusCurve.Evaluate(t);

    public (Vector3 pos, float radius)[] Points { get => points; }


    public (Vector3 center, Vector3 size) BoundingBox()
    {
        Bounds bounds = pathCreator.path.bounds;
        return (transform.position + bounds.center, bounds.size + radius * 2 * Vector3.one);
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        DrawRadius();
    }



    public void DrawRadius(float marge = 0)
    {
        pathCreator = GetComponent<PathCreator>();

        Vector3 lastPnt = Vector3.zero;
        Vector3 lastUp = Vector3.zero;
        Vector3 lastRight = Vector3.zero;
        float lastRadius = 0;

        for (int i = 0; i < nbPoints; i++)
        {
            float t = (float)i / (nbPoints - 1);
            t = Mathf.Clamp(t, 0, 0.999f);

            float radiusCrt = radius * radiusCurve.Evaluate(t) + marge;

            Vector3 pnt = pathCreator.path.GetPointAtTime(t);
            Quaternion rot = pathCreator.path.GetRotation(t);

            Vector3 up = rot * Vector3.up;
            Vector3 forward = rot * Vector3.forward;
            Vector3 right = Vector3.Cross(forward, up);

            if (drawCircle)
                GizmosExtension.DrawCircle(pnt, forward, radiusCrt);

            if (drawLine)
            {
                if (i > 0)
                {
                    Gizmos.DrawLine(lastPnt + lastUp    * lastRadius, pnt + up    * radiusCrt);
                    Gizmos.DrawLine(lastPnt - lastUp    * lastRadius, pnt - up    * radiusCrt);
                    Gizmos.DrawLine(lastPnt + lastRight * lastRadius, pnt + right * radiusCrt);
                    Gizmos.DrawLine(lastPnt - lastRight * lastRadius, pnt - right * radiusCrt);
                }

                if (i == 0 || i == nbPoints - 1)
                {
                    Quaternion arcRot = rot;
                    if (i == 0) arcRot *= Quaternion.Euler(0, 180, 0);
                    GizmosExtension.DrawArc(pnt, 180, radiusCrt, arcRot);
                    arcRot *= Quaternion.Euler(0, 0, 90);
                    GizmosExtension.DrawArc(pnt, 180, radiusCrt, arcRot);
                }
            }

            lastPnt = pnt;
            lastUp = up;
            lastRight = right;
            lastRadius = radiusCrt;
        }
    }


    void OnValidate()
    {
        radius = Mathf.Max(radius, 0);
        nbPoints = Mathf.Max(nbPoints, 2);

        if (Application.isPlaying)
        {
            if (!pathCreator)
                pathCreator = GetComponent<PathCreator>();

            CachePoints();
            onChanged.Invoke();
        }
    }


    void Awake()
    {
        pathCreator = GetComponent<PathCreator>();
        CachePathPoints();
        CachePoints();
    }

    void Update()
    {
        if (CurveHasChanged())
        {
            onChanged.Invoke();
            CachePathPoints();
            CachePoints();
        }
    }



    bool CurveHasChanged()
    {
        if (lastPathPoints.Length != pathCreator.path.NumPoints)
            return true;

        for (int i = 0; i < lastPathPoints.Length; i++)
            if (lastPathPoints[i] != pathCreator.path.GetPoint(i))
                return true;

        return false;
    }

    void CachePathPoints()
    {
        lastPathPoints = new Vector3[pathCreator.path.NumPoints];

        for (int i = 0; i < pathCreator.path.NumPoints; i++)
            lastPathPoints[i] = pathCreator.path.GetPoint(i);
    }


    void CachePoints()
    {
        points = new (Vector3, float)[nbPoints];

        for (int i = 0; i < nbPoints; i++)
        {
            float t = (float)i / (nbPoints - 1);
            t = Mathf.Clamp(t, 0, 0.999f);
            points[i] = (pathCreator.path.GetPointAtTime(t), Radius(t));
        }
    }


    /*
    public float DistToSurface(Vector3 pos)
    {
        float distToSrf = float.MaxValue;

        Vector3 pathPntL = pathCreator.path.GetPointAtTime(0);

        for (int i = 1; i < nbPoints; i++)
        {
            float t = (float)i / (nbPoints - 1);
            t = Mathf.Clamp(t, 0, 0.999f);

            Vector3 pathPntR = pathCreator.path.GetPointAtTime(t);
            Vector3 pathPnt = MathExtension.ClosestPointOnLine(pathPntL, pathPntR, pos, true, out float lineTime);
            float pathPntTime = t - (1 - lineTime) / (nbPoints - 1);
            float r = Radius(pathPntTime);

            Vector3 toPos = pos - pathPnt;
            float dist2ToPos = toPos.sqrMagnitude;

            if (dist2ToPos <= r * r)
                return 0;

            float distToSrfCrt = Mathf.Sqrt(dist2ToPos) - r;
            distToSrf = Mathf.Min(distToSrf, distToSrfCrt);

            pathPntL = pathPntR;
        }

        return distToSrf;
    }
    */
}
