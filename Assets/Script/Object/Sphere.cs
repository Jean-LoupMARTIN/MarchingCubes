using UnityEngine;



public class Sphere : Object
{
    [SerializeField] float radiusStart = 2;
    float radiusEnd; // radiusStart + marge



    protected override void OnValidate()
    {
        base.OnValidate();

        radiusStart = Mathf.Max(radiusStart, 0);
        radiusEnd = radiusStart + marge;
    }


    protected override void DrawCut(float t)
        => GizmosExtension.DrawSphereCircle(transform.position, Mathf.Lerp(radiusEnd, radiusStart, t));


    protected override (Vector3 center, Vector3 size) BoudingBox()
        => (transform.position, radiusEnd * 2 * Vector3.one);

    protected override string CShaderAddPath => "AddSphere";

    protected override void CShaderAddSetParameters(Chunk chunk)
    {
        Vector3 center = transform.position - chunk.transform.position;
        center /= chunk.CubeSize;

        float radiusStart = this.radiusStart / chunk.CubeSize;
        float radiusEnd = this.radiusEnd / chunk.CubeSize;

        CShaderAdd.SetFloats("center", new float[] { center.x, center.y, center.z });
        CShaderAdd.SetFloat("radiusStart", radiusStart);
        CShaderAdd.SetFloat("radiusStart2", radiusStart * radiusStart);
        CShaderAdd.SetFloat("radiusEnd", radiusEnd);
        CShaderAdd.SetFloat("radiusEnd2", radiusEnd * radiusEnd);
        CShaderAdd.SetFloat("dRadius", radiusEnd - radiusStart);
    }

    protected override void CShaderAddDisposeBuffers() {}
}
