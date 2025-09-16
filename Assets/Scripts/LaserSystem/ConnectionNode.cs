using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    public enum NodeType
    {
        Connector,
        Generator,
        Receiver
    }

    public enum EnergyType
    {
        None,
        Red,
        Blue,
        Mixed
    }

    public class Connection
    {
        public readonly ConnectionNode FirstNode;
        public readonly ConnectionNode SecondNode;
        public bool IsActive = true;
        public Vector3? HitPoint = null;
        
        public Connection(ConnectionNode firstNode, ConnectionNode secondNode)
        {
            FirstNode = firstNode;
            SecondNode = secondNode;
        }

        public bool IsConnectionWithNodes(ConnectionNode firstNode, ConnectionNode secondNode)
        {
            return FirstNode == firstNode && SecondNode == secondNode || FirstNode == secondNode && SecondNode == firstNode;
        }

        public EnergyType EnergyType()
        {
            var firstNodeEnergy = FirstNode.EnergyType;
            var secondNodeEnergy = SecondNode.EnergyType;
            var hasSameEnergyType = firstNodeEnergy == secondNodeEnergy;

            if (hasSameEnergyType)
            {
                return firstNodeEnergy;
            }

            return global::LaserSystem.EnergyType.None;
        }
    }

    public abstract class ConnectionNode : MonoBehaviour
    {
        [SerializeField]
        private Transform _connectionTargetPosition;
        
        public Transform ConnectionTargetTransform => _connectionTargetPosition;
        public bool IsActiveNode => _connectingNodes.Count > 0;
        public virtual bool IsActive { get; set; } // Добавляем для Receiver
        public abstract EnergyType EnergyType { get; set; }
        public abstract NodeType NodeType { get; }
        public int MinDepth = int.MaxValue;
        public EnergyType SecondaryEnergyType = EnergyType.None;
        public Dictionary<ConnectionNode, int> Depths = new Dictionary<ConnectionNode, int>();
        public List<ConnectionNode> ConnectingNodes => _connectingNodes;
        private List<ConnectionNode> _connectingNodes = new ();
        
        private List<Connection> _connections = new ();

        protected virtual void Start()
        {
            LaserSystem.Instance.RegisterConnectionNode(this);
        }

        protected void ClearConnections()
        {
            for (var i = 0; i < _connectingNodes.Count; i++)
            {
                var connectionNode = _connectingNodes[i];
                connectionNode.RemoveConnection(this);
            }

            _connectingNodes.Clear();
        }

        public void AddConnection(ConnectionNode connectionNode)
        {
            if (_connectingNodes.Contains(connectionNode))
            {
                return;
            }

            _connectingNodes.Add(connectionNode);
            connectionNode.AddConnection(this);
        }

        private void RemoveConnection(ConnectionNode connection)
        {
            _connectingNodes.Remove(connection);
        }

        public virtual bool CanPropagateEnergy => NodeType != NodeType.Receiver;
    }
}