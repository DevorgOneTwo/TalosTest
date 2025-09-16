using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserSystem
{
    public class LaserSystem : MonoBehaviour
    {
        [SerializeField] 
        private LineRenderer _simplePathPrefab;
        [SerializeField] 
        private LineRenderer _redConnectionLine;
        [SerializeField]
        private LineRenderer _blueConnectionLine;

        [SerializeField] 
        private LayerMask _collisionMask;
        
        private List<ConnectionNode> _allConnectionNodes = new();
        private List<ConnectionNode> _activeConnectionsNodes = new();

        private List<Connection> _connections = new();

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

        private void Update()
        {
            CalculateConnections();
        }

        private void CalculateConnections()
        {
            ClearVfx();
            SelectActiveConnections();
            
            SetEnergyTypes();
            _connections.Clear();
            _connections = FindAllConnections();
            DrawConnectionsDebug(_connections);
            VisualizeConnectionsEnergy();
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

                if (connectionNode.NodeType == NodeType.Connector)
                {
                    connectionNode.EnergyType = EnergyType.None;
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
                        RaycastHit hit;
                        newConnection.IsActive = !IsNodeBlocked(connectionNode, connectingNode, out hit);
                        newConnection.HitPoint = newConnection.IsActive ? null : hit.point;
                        connections.Add(newConnection);
                    }
                }
            }
            
            return connections;
        }

        private void SetEnergyTypes()
        {
            var generators = new List<ConnectionNode>();
            
            for (var i = 0; i < _activeConnectionsNodes.Count; i++)
            {
                var connectionNode = _activeConnectionsNodes[i];
                if (connectionNode.NodeType == NodeType.Generator)
                {
                    generators.Add(connectionNode);
                }
            }
            
            for (var i = 0; i < generators.Count; i++)
            {
                CalculateDistances(generators[i]);
            }
        }

        private void CalculateDistances(ConnectionNode generator)
        {
            if (generator.NodeType != NodeType.Generator)
            {
                return;
            }

            Dictionary<ConnectionNode, int> distances = new Dictionary<ConnectionNode, int>();
            Queue<ConnectionNode> queue = new Queue<ConnectionNode>();
            HashSet<ConnectionNode> visited = new HashSet<ConnectionNode>();

            queue.Enqueue(generator);
            visited.Add(generator);
            distances[generator] = 0;

            while (queue.Count > 0)
            {
                ConnectionNode current = queue.Dequeue();

                foreach (ConnectionNode connectingNode in current.ConnectingNodes)
                {
                    bool isNodeBlocked = IsNodeBlocked(current, connectingNode, out var hit);
                    if (!visited.Contains(connectingNode) && !isNodeBlocked)
                    {
                        visited.Add(connectingNode);
                        queue.Enqueue(connectingNode);
                        distances[connectingNode] = distances[current] + 1;
                        connectingNode.Depth = distances[connectingNode];
                    }
                }
            }

            List<ConnectionNode> closestConnectors = new List<ConnectionNode>();
            List<ConnectionNode> farthestConnectors = new List<ConnectionNode>();

            var minDistance = int.MaxValue;
            var maxDistance = 0;

            foreach (var distance in distances)
            {
                if (distance.Key.NodeType == NodeType.Generator)
                {
                    continue;
                }

                var distanceValue = distance.Value;
                
                if (distanceValue < minDistance && distanceValue > 0)
                {
                    minDistance = distanceValue;
                    closestConnectors.Clear();
                    closestConnectors.Add(distance.Key);
                }
                else if (distanceValue == minDistance)
                {
                    closestConnectors.Add(distance.Key);
                }

                if (distanceValue > maxDistance)
                {
                    maxDistance = distanceValue;
                    farthestConnectors.Clear();
                    farthestConnectors.Add(distance.Key);
                }
                else if (distanceValue == maxDistance)
                {
                    farthestConnectors.Add(distance.Key);
                }
            }
            
            SpreadEnergy(generator, distances);
        }

        private bool IsNodeBlocked(ConnectionNode first, ConnectionNode second, out RaycastHit hit)
        {
            var start = first.ConnectionTargetTransform.position;
            var end = second.ConnectionTargetTransform.position;
            var direction = end - start;
            var distance = direction.magnitude;
            direction = direction.normalized;
            return Physics.Raycast(start, direction, out hit, distance, _collisionMask);
        }

        private void SpreadEnergy(ConnectionNode generator, Dictionary<ConnectionNode, int> distances)
        {
            var sortedNodes = new List<KeyValuePair<ConnectionNode, int>>(distances);
            sortedNodes.Sort((a, b) => a.Value.CompareTo(b.Value));

            foreach (var pair in sortedNodes)
            {
                if (pair.Key.NodeType != NodeType.Connector)
                {
                    continue;
                }

                if (IsPathBlocked(generator, pair.Key))
                {
                    continue;
                }

                pair.Key.EnergyType = generator.EnergyType;
            }
        }
        
        private bool IsPathBlocked(ConnectionNode generator, ConnectionNode target)
        {
            Queue<ConnectionNode> queue = new Queue<ConnectionNode>();
            HashSet<ConnectionNode> visited = new HashSet<ConnectionNode>();
            Dictionary<ConnectionNode, ConnectionNode> parent = new Dictionary<ConnectionNode, ConnectionNode>();

            queue.Enqueue(generator);
            visited.Add(generator);

            while (queue.Count > 0)
            {
                ConnectionNode current = queue.Dequeue();

                if (current == target)
                {
                    return false;
                }

                for (int i = 0; i < current.ConnectingNodes.Count; i++)
                {
                    ConnectionNode neighbor = current.ConnectingNodes[i];
                    RaycastHit hit;
                    var isNodeBlocked = IsNodeBlocked(current, neighbor, out hit);

                    if (!visited.Contains(neighbor) && !isNodeBlocked)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                        parent[neighbor] = current;
                    }
                }
            }

            return true;
        }

        private void VisualizeConnectionsEnergy()
        {
            for (var i = 0; i < _connections.Count; i++)
            {
                var connection = _connections[i];
                var startNode = connection.FirstNode;
                var endNode = connection.SecondNode;
                
                Vector3 start = startNode.ConnectionTargetTransform.position;
                Vector3 end;

                var energyType = connection.EnergyType();
                if (!connection.IsActive && connection.HitPoint.HasValue)
                {
                    var mainNode = startNode.Depth <= endNode.Depth || endNode.Depth == -1 
                        ? startNode : endNode;
                    start = mainNode.ConnectionTargetTransform.position;
                    end = connection.HitPoint.Value;
                    energyType = mainNode.EnergyType;
                }
                else
                {
                    end = endNode.ConnectionTargetTransform.position;
                }

                var energyPrefab = GetLineRendererByEnergyType(energyType);
                var direction = end - start;
                direction = direction.normalized;
                var energyLine = Instantiate(energyPrefab, start, Quaternion.LookRotation(direction));
                energyLine.useWorldSpace = true;
                energyLine.positionCount = 2;
                energyLine.SetPosition(0, start);
                energyLine.SetPosition(1, end);
                _vfxGameObjects.Add(energyLine.gameObject);
            }
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

        private LineRenderer GetLineRendererByEnergyType(EnergyType energyType)
        {
            switch (energyType)
            {
                case EnergyType.None:
                    return _simplePathPrefab;
                case EnergyType.Red:
                    return _redConnectionLine;
                case EnergyType.Blue:
                    return _blueConnectionLine;
                default:
                    throw new ArgumentOutOfRangeException(nameof(energyType), energyType, null);
            }
        }
    }
}