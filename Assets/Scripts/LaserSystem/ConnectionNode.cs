using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        Blue
    }

    public class Connection
    {
        public readonly bool IsFirstChainConnection;
        public readonly ConnectionNode FirstNode;
        public readonly ConnectionNode SecondNode;
        public int Depth = -1;
        
        public Connection(ConnectionNode firstNode, ConnectionNode secondNode)
        {
            FirstNode = firstNode;
            SecondNode = secondNode;
            IsFirstChainConnection = firstNode.NodeType == NodeType.Generator || secondNode.NodeType == NodeType.Connector;
        }

        public bool IsConnectionWithNodes(ConnectionNode firstNode, ConnectionNode secondNode)
        {
            return FirstNode == firstNode && SecondNode == secondNode || FirstNode == secondNode && SecondNode == firstNode;
        }
    }

    public abstract class ConnectionNode : MonoBehaviour
    {
        [SerializeField]
        private Transform _connectionTargetPosition;
        
        public Transform ConnectionTargetTransform => _connectionTargetPosition;
        public bool IsActiveNode => _connectingNodes.Count > 0;
        public abstract EnergyType EnergyType { get; }
        public abstract NodeType NodeType { get; }
        
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

        public void RemoveConnection(ConnectionNode connection)
        {
            if (_connectingNodes.Remove(connection))
            {
                connection.RemoveConnection(this);
            }
        }

        public bool HasConnectionWithNode(ConnectionNode connection)
        {
            return _connectingNodes.Contains(connection);
        }
    }
}