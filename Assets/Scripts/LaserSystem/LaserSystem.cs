using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    public class LaserSystem : MonoBehaviour
    {
        [SerializeField] 
        private LineRenderer _simplePathPrefab;
        
        private List<ConnectionNode> _allConnectionNodes = new ();
        private List<ConnectionNode> _activeConnectionsNodes = new ();

        private List<Connection> _connections = new ();

        private List<GameObject> _vfxGameObjects = new();

        public static LaserSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterConnectionNode(ConnectionNode connectionNode)
        {
            _allConnectionNodes.Add(connectionNode);
        }

        private void CalculatePaths()
        {
            
        }

        private void Update()
        {
            CalculateConnections();
        }

        private void CalculateConnections()
        {
            ClearVfx();
            SelectActiveConnections();
            _connections.Clear();
            _connections = FindAllConnections();
            DrawConnectionsDebug(_connections);
        }

        private void ClearVfx()
        {
            for (var i = 0; i < _vfxGameObjects.Count; i++)
            {
                Destroy(_vfxGameObjects[i]);
            }
            
            _vfxGameObjects.Clear();
        }

        private void SelectActiveConnections()
        {
            _activeConnectionsNodes.Clear();
            for (var i = 0; i < _allConnectionNodes.Count; i++)
            {
                var connectionNode = _allConnectionNodes[i];
                if (connectionNode.IsActiveNode)
                {
                    _activeConnectionsNodes.Add(connectionNode);
                }
            }
        }

        private void DrawConnectionsDebug(List<Connection> connections)
        {
            for (var i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                var start = connection.FirstNode.ConnectionTargetTransform.position;
                var end = connection.SecondNode.ConnectionTargetTransform.position;
                var direction = end - start;
                direction = direction.normalized;
                var debugLine = Instantiate(_simplePathPrefab, start, Quaternion.LookRotation(direction));
                debugLine.useWorldSpace = true;
                debugLine.positionCount = 2;
                debugLine.SetPosition(0, start);
                debugLine.SetPosition(1, end);
                
                _vfxGameObjects.Add(debugLine.gameObject);
            }
        }

        private List<Connection> FindAllConnections()
        {
            var connections = new List<Connection>();
            
            for (var i = 0; i < _activeConnectionsNodes.Count; i++)
            {
                var connectionNode = _activeConnectionsNodes[i];
                var connectingNodes = connectionNode.ConnectingNodes;
                for (var j = 0; j < connectingNodes.Count; j++)
                {
                    var connectingNode = connectingNodes[j];
                    if (!HasConnectionWithNodes(connections, connectionNode, connectingNode))
                    {
                        var newConnection = new Connection(connectionNode, connectingNode);
                        connections.Add(newConnection);
                    }
                }
            }
            
            return connections;
        }

        private void SetEnergyTypes(List<Connection> connections)
        {
            //выстроить цепочку коннекшенов
        }

        private bool HasConnectionWithNodes(List<Connection> connections, ConnectionNode firstNode, ConnectionNode secondNode)
        {
            for (var i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                if (connection.IsConnectionWithNodes(firstNode, secondNode))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}