using UnityEngine;



public class Cube : Object
{
    [SerializeField] Vector3 sizeStart = Vector3.one * 4;
    Vector3 sizeEnd; // sizeStart + marge * 2 * Vector3.one


    protected override void OnValidate()
    {
        base.OnValidate();

        sizeStart.x = Mathf.Max(sizeStart.x, 0);
        sizeStart.y = Mathf.Max(sizeStart.y, 0);
        sizeStart.z = Mathf.Max(sizeStart.z, 0);

        sizeEnd = sizeStart + marge * 2 * Vector3.one;
    }


    protected override void DrawCut(float t)
        => Gizmos.DrawWireCube(transform.position, Vector3.Lerp(sizeEnd, sizeStart, t));


    protected override (Vector3 center, Vector3 size) BoudingBox()
        => (transform.position, sizeEnd);

    protected override string CShaderAddPath => "AddCube";

    protected override void CShaderAddSetParameters(Chunk chunk)
    {
        Vector3 center = (transform.position - chunk.transform.position) / chunk.CubeSize;
        Vector3 sizeStart2 = sizeStart / 2 / chunk.CubeSize;
        Vector3 sizeEnd2 = sizeEnd / 2 / chunk.CubeSize;
        Vector3 dSize2 = sizeEnd2 - sizeStart2;

        CShaderAdd.SetFloats("cubeCenter",      new float[] { center.x, center.y, center.z });
        CShaderAdd.SetFloats("cubeSizeStart2",  new float[] { sizeStart2.x, sizeStart2.y, sizeStart2.z });
        CShaderAdd.SetFloats("cubeSizeEnd2",    new float[] { sizeEnd2.x, sizeEnd2.y, sizeEnd2.z });
        CShaderAdd.SetFloats("cubeDSize2",      new float[] { dSize2.x, dSize2.y, dSize2.z });
    }

    protected override void CShaderAddDisposeBuffers() { }
}