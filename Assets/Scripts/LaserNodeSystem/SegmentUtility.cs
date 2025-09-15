using UnityEngine;

public static class SegmentUtility
{
    // Returns closest points on each segment; returns false when degenerate
    public static bool SegmentSegmentClosestPoints(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
    {
        c1 = Vector3.zero; c2 = Vector3.zero;
        Vector3 d1 = q1 - p1; // direction vector of segment S1
        Vector3 d2 = q2 - p2; // direction vector of segment S2
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);

        float s, t;

        float EPS = 1e-6f;
        if (a <= EPS && e <= EPS)
        {
            // both segments degenerate to points
            c1 = p1; c2 = p2; return true;
        }
        if (a <= EPS)
        {
            // first degenerate
            s = 0f;
            t = Mathf.Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= EPS)
            {
                t = 0f;
                s = Mathf.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denom = a * e - b * b;
                if (denom != 0f) s = Mathf.Clamp((b * f - c * e) / denom, 0f, 1f);
                else s = 0f; // parallel
                t = (b * s + f) / e;

                if (t < 0f)
                {
                    t = 0f;
                    s = Mathf.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Mathf.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }

        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
        return true;
    }
}