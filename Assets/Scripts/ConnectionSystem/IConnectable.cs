using UnityEngine;
using System.Collections.Generic;

public interface IConnectable
{
    Vector3 ConnectionPoint { get; }
    List<IConnectable> Connections { get; }
    void AddConnection(IConnectable other);
    void ClearConnections();
    bool IsPropagator { get; }
    LaserColor EffectiveColor { get; set; }
    int MinDistance { get; set; }
    bool IsHeld { get; } // Для коннекторов, игнорировать если в руках
    int GetID(); // либо заменить на Guid
    bool IsReached { get; set; }
}