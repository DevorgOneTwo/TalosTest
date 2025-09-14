using UnityEngine;

public struct BeamSegment
{
    public Vector3 Start;
    public Vector3 End;
    public LaserColor Color;

    public BeamSegment(Vector3 start, Vector3 end, LaserColor color)
    {
        Start = start;
        End = end;
        Color = color;
    }
    
    public float OriginalLength => Vector3.Distance(Start, End);
}