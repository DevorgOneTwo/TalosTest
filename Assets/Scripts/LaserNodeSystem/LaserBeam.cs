using UnityEngine;

public class LaserBeam
{
    public LaserNode startNode;
    public LaserNode endNode;

    public Vector3 start;
    public Vector3 end;

    public LaserColorType laserType = LaserColorType.None;

    // если true — рисуем искры в hitPoint
    public bool hitSpark = false;
    public Vector3 hitPoint;

    // если прерван игроком — он не влияет на пересечения
    public bool blockedByPlayer = false;

    public LaserBeam(LaserNode from, LaserNode to, LaserColorType type)
    {
        startNode = from;
        endNode = to;
        start = from != null ? from.Position : Vector3.zero;
        end = to != null ? to.Position : Vector3.zero;
        laserType = type;
    }
}