using System;
using System.Collections.Generic;
using System.Linq;
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
        
        [Space(10)]
        [SerializeField]
        private List<Receiver> _receivers;
        [SerializeField] 
        private GameObject _wall;

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
            UpdateWall();
        }

        private void UpdateWall()
        {
            var isOpenWall = true;
            for (var i = 0; i < _receivers.Count; i++)
            {
                var receiver = _receivers[i];
                if (!receiver.IsActive())
                {
                    isOpenWall = false;
                    break;
                }
            }
            
            _wall.SetActive(!isOpenWall);
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

                connectionNode.Depths.Clear();
                connectionNode.MinDepth = connectionNode.NodeType == NodeType.Generator ? 0 : int.MaxValue;
                if (connectionNode.NodeType == NodeType.Connector)
                {
                    connectionNode.EnergyType = EnergyType.None;
                }
                
                if (connectionNode.NodeType == NodeType.Receiver)
                {
                    connectionNode.HasEnergy = false;
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
                if (!connection.IsActive)
                {
                    debugLine.startColor = Color.gray;
                    debugLine.endColor = Color.gray;
                }
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
            var receivers = new List<ConnectionNode>();
            
            for (var i = 0; i < _activeConnectionsNodes.Count; i++)
            {
                var connectionNode = _activeConnectionsNodes[i];
                if (connectionNode.NodeType == NodeType.Generator)
                {
                    generators.Add(connectionNode);
                }
                else if (connectionNode.NodeType == NodeType.Receiver)
                {
                    receivers.Add(connectionNode);
                }
            }

            foreach (var generator in generators)
            {
                CalculateDistances(generator);
            }
            
            foreach (var node in _activeConnectionsNodes)
            {
                if (node.NodeType == NodeType.Generator) 
                    continue;

                if (node.Depths.Count == 0)
                {
                    node.EnergyType = EnergyType.None;
                    continue;
                }

                var minDepth = node.Depths.Values.Min();
                var closestGenerators = node.Depths
                    .Where(kv => kv.Value == minDepth)
                    .Select(kv => kv.Key)
                    .ToList();

                node.MinDepth = minDepth;

                var uniqueEnergies = new HashSet<EnergyType>(closestGenerators.Select(g => g.EnergyType));

                if (uniqueEnergies.Count == 1)
                {
                    node.EnergyType = uniqueEnergies.First();
                }
                else
                {
                    node.EnergyType = EnergyType.Mixed;
                }
            }
            
            foreach (var receiver in receivers)
            {
                if (receiver.Depths.Count == 0)
                {
                    receiver.HasEnergy = false;
                    continue;
                }

                var minDepth = receiver.Depths.Values.Min();
                var closestGenerators = receiver.Depths
                    .Where(kv => kv.Value == minDepth)
                    .Select(kv => kv.Key)
                    .ToList();

                receiver.MinDepth = minDepth;

                var uniqueEnergies = new HashSet<EnergyType>(closestGenerators.Select(g => g.EnergyType));
                
                if (uniqueEnergies.Count == 1 && uniqueEnergies.First() == receiver.EnergyType)
                {
                    receiver.HasEnergy = true;
                }
                else
                {
                    receiver.HasEnergy = false;
                }
            }
        }

        private void CalculateDistances(ConnectionNode generator)
        {
            if (generator.NodeType != NodeType.Generator)
            {
                return;
            }

            var distances = new Dictionary<ConnectionNode, int>();
            var queue = new Queue<ConnectionNode>();
            var visited = new HashSet<ConnectionNode>();

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
                        distances[connectingNode] = distances[current] + 1;
                        connectingNode.Depths[generator] = distances[connectingNode];
                        
                        if (connectingNode.CanPropagateEnergy)
                        {
                            queue.Enqueue(connectingNode);
                        }
                    }
                }
            }
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

        private void VisualizeConnectionsEnergy()
        {
            for (var i = 0; i < _connections.Count; i++)
            {
                var connection = _connections[i];
                var startNode = connection.FirstNode;
                var endNode = connection.SecondNode;
                var start = startNode.ConnectionTargetTransform.position;
                var end = endNode.ConnectionTargetTransform.position;

                if (!connection.IsActive && connection.HitPoint.HasValue)
                {
                    var startCloser = startNode.MinDepth <= endNode.MinDepth;
                    var closerPos = startCloser ? start : end;
                    var otherPos = startCloser ? end : start;
                    var closerEnergy = startCloser ? startNode.EnergyType : endNode.EnergyType;
                    var otherEnergy = startCloser ? endNode.EnergyType : startNode.EnergyType;
                    
                    var firstStart = closerPos;
                    var firstEnd = connection.HitPoint.Value;
                    var firstDirection = firstEnd - firstStart;
                    firstDirection = firstDirection.normalized;
                    var firstPrefab = GetLineRendererByEnergyType(closerEnergy);
                    var firstLine = Instantiate(firstPrefab, firstStart, Quaternion.LookRotation(firstDirection));
                    firstLine.useWorldSpace = true;
                    firstLine.positionCount = 2;
                    firstLine.SetPosition(0, firstStart);
                    firstLine.SetPosition(1, firstEnd);
                    _vfxGameObjects.Add(firstLine.gameObject);

                    if (otherEnergy != EnergyType.None && otherEnergy != closerEnergy)
                    {
                        var secondStart = connection.HitPoint.Value;
                        var secondEnd = otherPos;
                        var secondDirection = secondEnd - secondStart;
                        secondDirection = secondDirection.normalized;
                        var secondPrefab = GetLineRendererByEnergyType(otherEnergy);
                        var secondLine = Instantiate(secondPrefab, secondStart, Quaternion.LookRotation(secondDirection));
                        secondLine.useWorldSpace = true;
                        secondLine.positionCount = 2;
                        secondLine.SetPosition(0, secondStart);
                        secondLine.SetPosition(1, secondEnd);
                        _vfxGameObjects.Add(secondLine.gameObject);
                    }
                }
                else if (startNode.EnergyType != endNode.EnergyType && startNode.EnergyType != EnergyType.None && endNode.EnergyType != EnergyType.None)
                {
                    var isMixedInvolved = startNode.EnergyType == EnergyType.Mixed || endNode.EnergyType == EnergyType.Mixed;

                    if (isMixedInvolved)
                    {
                        EnergyType lineEnergy = startNode.EnergyType == EnergyType.Mixed ? endNode.EnergyType : startNode.EnergyType;
                        var prefab = GetLineRendererByEnergyType(lineEnergy);
                        var direction = end - start;
                        direction = direction.normalized;
                        var line = Instantiate(prefab, start, Quaternion.LookRotation(direction));
                        line.useWorldSpace = true;
                        line.positionCount = 2;
                        line.SetPosition(0, start);
                        line.SetPosition(1, end);
                        _vfxGameObjects.Add(line.gameObject);
                    }
                    else
                    {
                        var midPoint = (start + end) / 2;
                        var firstDirection = midPoint - start;
                        firstDirection = firstDirection.normalized;
                        var firstPrefab = GetLineRendererByEnergyType(startNode.EnergyType);
                        var firstLine = Instantiate(firstPrefab, start, Quaternion.LookRotation(firstDirection));
                        firstLine.useWorldSpace = true;
                        firstLine.positionCount = 2;
                        firstLine.SetPosition(0, start);
                        firstLine.SetPosition(1, midPoint);
                        _vfxGameObjects.Add(firstLine.gameObject);

                        var secondDirection = end - midPoint;
                        secondDirection = secondDirection.normalized;
                        var secondPrefab = GetLineRendererByEnergyType(endNode.EnergyType);
                        var secondLine = Instantiate(secondPrefab, midPoint, Quaternion.LookRotation(secondDirection));
                        secondLine.useWorldSpace = true;
                        secondLine.positionCount = 2;
                        secondLine.SetPosition(0, midPoint);
                        secondLine.SetPosition(1, end);
                        _vfxGameObjects.Add(secondLine.gameObject);
                    }
                }
                else
                {
                    var energyType = connection.GetEnergyType();
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
                case EnergyType.Mixed:
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