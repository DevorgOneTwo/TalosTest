using UnityEngine;
using System.Collections.Generic;

public abstract class ConnectionDevice : MonoBehaviour, IConnectable
{
    [SerializeField] protected Transform connectionPoint;
    private List<IConnectable> _connections = new ();

    public List<IConnectable> Connections => _connections;
    public Vector3 ConnectionPoint => connectionPoint ? connectionPoint.position : transform.position;
    public abstract bool IsPropagator { get; }
    public LaserColor EffectiveColor { get; set; }
    public int MinDistance { get; set; } = int.MaxValue;
    public bool IsReached { get; set; }

    public virtual bool IsHeld { get; }

    public virtual void AddConnection(IConnectable other)
    {
        if (!_connections.Contains(other))
        {
            _connections.Add(other);
            other.AddConnection(this);
        }
    }

    public virtual void ClearConnections()
    {
        foreach (var conn in _connections)
        {
            conn.Connections.Remove(this);
        }
        _connections.Clear();
    }

    public int GetID()
    {
        return GetInstanceID();
    }
}