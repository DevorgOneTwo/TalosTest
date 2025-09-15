using System.Collections.Generic;
using UnityEngine;

public enum NodeType { Generator, Connector, Receiver }
public enum LaserColorType { None, Red, Blue, Green }

[DisallowMultipleComponent]
public class LaserNode : MonoBehaviour
{
    [SerializeField]
    private Transform _laserTarget;
    public NodeType nodeType;

    // тип лазера для генератора и передаваемого луча
    public LaserColorType laserType = LaserColorType.None;

    public List<LaserNode> connections = new List<LaserNode>();

    public Vector3 Position => _laserTarget.position;

    public virtual void ConnectTo(LaserNode other)
    {
        if (other == null || other == this) return;
        if (!connections.Contains(other)) connections.Add(other);
        if (!other.connections.Contains(this)) other.connections.Add(this);
    }

    public virtual void DisconnectFrom(LaserNode other)
    {
        if (other == null) return;
        connections.Remove(other);
        other.connections.Remove(this);
    }

    public virtual void DisconnectAll()
    {
        var copy = new List<LaserNode>(connections);
        foreach (var n in copy)
        {
            if (n != null) n.connections.Remove(this);
        }
        connections.Clear();
    }
}