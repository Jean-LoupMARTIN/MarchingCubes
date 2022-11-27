using UnityEngine;





public static class MathExtension
{
    public static Quaternion QuatAvgApprox(Quaternion[] quats, float[] weights = null)
    {
        if (quats.Length == 0)
            return Quaternion.identity;

        if (weights != null && quats.Length != weights.Length)
            return Quaternion.identity;

        Vector4[] vects = new Vector4[quats.Length];
        for (int i = 0; i < vects.Length; i++)
            vects[i] = Quat2Vect(quats[i]);

        Vector4 vectsAvg = Vector4.zero;

        for (int i = 0; i < vects.Length; i++)
        {
            Vector4 v = vects[i];
            float w = weights == null ? 1 : weights[i];

            if (i > 0 && Vector4.Dot(v, vects[0]) < 0)
                w *= -1;

            vectsAvg += v * w;
        }

        vectsAvg.Normalize();

        return new Quaternion(vectsAvg.x, vectsAvg.y, vectsAvg.z, vectsAvg.w);
    }

    public static Vector4 Quat2Vect(Quaternion q) => new Vector4(q.x, q.y, q.z, q.w);

    // https://diego.assencio.com/?index=ec3d5dfdfc0b6a0d147a656f0af332bd
    public static Vector3 ClosestPointOnLine(Vector3 A, Vector3 B, Vector3 X, bool segment, out float t)
    {
        Vector3 AB = B - A;
        Vector3 AX = X - A;
        t = Vector3.Dot(AX, AB) / Vector3.Dot(AB, AB);

        if (segment)
            t = Mathf.Clamp01(t);

        return A + t * AB;
    }

    public static Vector3 Divide(Vector3 a, Vector3 b)
        => new Vector3(a.x / b.x,
                       a.y / b.y,
                       a.z / b.z);
}
