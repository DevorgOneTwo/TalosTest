using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

public class LaserController : MonoBehaviour
{
    public static LaserController Instance { get; private set; }

    [SerializeField] private GameObject _laserPathPrefab;
    [SerializeField] private GameObject _laserSphereRedPrefab;
    [SerializeField] private GameObject _laserSphereBluePrefab;
    [SerializeField] private GameObject _laserBeamRedPrefab;
    [SerializeField] private GameObject _laserBeamBluePrefab;
    [SerializeField] private GameObject _laserHitSparksPrefab;

    [FormerlySerializedAs("doorObject")] [SerializeField] private GameObject _wall;
    [FormerlySerializedAs("playerMask")] [SerializeField] private LayerMask _playerMask; // LayerMask только для игрока

    private List<Generator> _generators = new ();
    private List<Connector> _connectors = new ();
    private List<Receiver> _receivers = new ();
    private List<GameObject> _activeVFX = new ();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateLasers();
    }

    public void RegisterGenerator(Generator gen) => _generators.Add(gen);
    public void RegisterConnector(Connector conn) => _connectors.Add(conn);
    public void RegisterReceiver(Receiver rec) => _receivers.Add(rec);

    public void UpdateLasers()
    {
        ClearFX();

        var activeDevices = GetActiveDevices();
        var computeDistances = ComputeDistances();
        
        AssignColorsAndDistances(computeDistances, activeDevices);
        DrawDashedLines();
        
        var potentialSegments = GenerateSegments(activeDevices);
        var (clippedSegments, intersections) = ClipSegments(potentialSegments, activeDevices);
        AdjustReachedAndColors(activeDevices, clippedSegments, intersections);
        
        var finalSegments = GenerateSegments(activeDevices);
        var (finalClippedSegments, finalIntersections) = ClipSegments(finalSegments, activeDevices);
        
        RenderBeams(finalClippedSegments);
        RenderSpheres(finalClippedSegments, activeDevices);
        RenderSparks(finalClippedSegments, finalIntersections, activeDevices);
        UpdateReceivers(finalClippedSegments);
        UpdateDoor();
    }

    private List<IConnectable> GetActiveDevices()
    {
        return _generators
            .Concat(_connectors.Where(c => !c.IsHeld).Cast<IConnectable>())
            .Concat(_receivers)
            .ToList();
    }

    private Dictionary<Generator, Dictionary<IConnectable, int>> ComputeDistances()
    {
        var computeDistances = new Dictionary<Generator, Dictionary<IConnectable, int>>();
        foreach (var generator in _generators)
        {
            computeDistances[generator] = BFSFrom(generator);
        }
        return computeDistances;
    }

    private Dictionary<IConnectable, int> BFSFrom(IConnectable start)
    {
        var distances = new Dictionary<IConnectable, int>();
        var queue = new Queue<IConnectable>();
        var visited = new HashSet<IConnectable>();

        queue.Enqueue(start);
        distances[start] = 0;
        visited.Add(start);

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (!curr.IsPropagator)
            {
                continue;
            }
            
            foreach (var neigh in curr.Connections)
            {
                if (visited.Add(neigh))
                {
                    distances[neigh] = distances[curr] + 1;
                    queue.Enqueue(neigh);
                }
            }
        }
        return distances;
    }

    private void AssignColorsAndDistances(Dictionary<Generator, Dictionary<IConnectable, int>> distances, List<IConnectable> devices)
    {
        foreach (var device in devices)
        {
            device.MinDistance = int.MaxValue;
            device.IsReached = false;
            var reachingGenerators = new List<Generator>();

            foreach (var gen in _generators)
            {
                if (distances[gen].TryGetValue(device, out var distance))
                {
                    if (distance < device.MinDistance)
                    {
                        device.MinDistance = distance;
                        reachingGenerators.Clear();
                        reachingGenerators.Add(gen);
                    }
                    else if (distance == device.MinDistance)
                    {
                        reachingGenerators.Add(gen);
                    }
                }
            }

            var colors = reachingGenerators.Select(g => g.Color).Distinct().ToList();
            device.EffectiveColor = colors.Count == 1 ? colors[0] : LaserColor.None;
        }
        
        foreach (var generator in _generators)
        {
            generator.IsReached = true;
        }
    }

    private void DrawDashedLines()
    {
        var lines = new HashSet<(int, int)>();
        foreach (var connector in _connectors.Where(c => !c.IsHeld))
        {
            foreach (var target in connector.Connections)
            {
                var ids = (Mathf.Min(connector.GetID(), target.GetID()), Mathf.Max(connector.GetID(), target.GetID()));
                if (!lines.Add(ids))
                {
                    continue;
                }

                var path = Instantiate(_laserPathPrefab, Vector3.zero, Quaternion.identity);
                LineRenderer lineRenderer = path.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.useWorldSpace = true;
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, connector.ConnectionPoint);
                    lineRenderer.SetPosition(1, target.ConnectionPoint);
                }
                _activeVFX.Add(path);
            }
        }
    }

    private List<BeamSegment> GenerateSegments(List<IConnectable> devices)
    {
        var segments = new List<BeamSegment>();
        var processedEdges = new HashSet<(int, int)>();

        foreach (var device in devices)
        {
            foreach (var connection in device.Connections)
            {
                var ids = (Mathf.Min(device.GetID(), connection.GetID()), Mathf.Max(device.GetID(), connection.GetID()));
                if (!processedEdges.Add(ids))
                {
                    continue;
                }

                var colorA = device.EffectiveColor;
                var colorB = connection.EffectiveColor;
                var posA = device.ConnectionPoint;
                var posB = connection.ConnectionPoint;

                if (colorA == LaserColor.None && colorB == LaserColor.None) continue;
                
                if (colorA == colorB && colorA != LaserColor.None)
                {
                    if (device.MinDistance <= connection.MinDistance)
                    {
                        segments.Add(new BeamSegment(posA, posB, colorA));
                    }
                    else
                    {
                        segments.Add(new BeamSegment(posB, posA, colorB));
                    }
                }
                else
                {
                    if (colorA != LaserColor.None)
                    {
                        segments.Add(new BeamSegment(posA, posB, colorA));
                    }
                    if (colorB != LaserColor.None)
                    {
                        segments.Add(new BeamSegment(posB, posA, colorB));
                    }
                }
            }
        }

        return segments;
    }

    private (List<BeamSegment>, List<Vector3>) ClipSegments(List<BeamSegment> segments, List<IConnectable> devices)
    {
        var clipped = new List<BeamSegment>();
        var intersections = new List<Vector3>();

        for (int i = 0; i < segments.Count; i++)
        {
            var beamSegment = segments[i];
            var direction = beamSegment.End - beamSegment.Start;
            var distance = direction.magnitude;
            direction.Normalize();

            var playerIntersected = false;
            var playerIntersectPoint = beamSegment.End;
            var minDistance = distance;

            if (Physics.Raycast(beamSegment.Start, direction, out RaycastHit hit, distance, _playerMask))
            {
                playerIntersected = true;
                minDistance = hit.distance;
                playerIntersectPoint = hit.point;
            }

            var laserIntersected = false;
            var closestIntersect = playerIntersectPoint;

            for (int j = 0; j < segments.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }
                
                var other = segments[j];
                if (Vector3.Distance(beamSegment.Start, other.End) < 0.001f &&
                    Vector3.Distance(beamSegment.End, other.Start) < 0.001f)
                {
                    continue;
                }
                
                if (FindIntersection(beamSegment.Start, beamSegment.End, other.Start, other.End, out Vector3 intersect))
                {
                    var newDist = Vector3.Distance(beamSegment.Start, intersect);
                    if (newDist < minDistance && newDist > 0.001f)
                    {
                        minDistance = newDist;
                        closestIntersect = intersect;
                        laserIntersected = true;
                        playerIntersected = false;
                    }
                }
            }

            var colorConflict = false;
            var colorConflictPoint = beamSegment.End;

            if (!playerIntersected && !laserIntersected)
            {
                IConnectable startDev = devices.FirstOrDefault(d => Vector3.Distance(d.ConnectionPoint, beamSegment.Start) < 0.01f);
                IConnectable endDev = devices.FirstOrDefault(d => Vector3.Distance(d.ConnectionPoint, beamSegment.End) < 0.01f);
                if (startDev != null && endDev != null && startDev.EffectiveColor != endDev.EffectiveColor &&
                    startDev.EffectiveColor != LaserColor.None && endDev.EffectiveColor != LaserColor.None)
                {
                    colorConflict = true;
                    colorConflictPoint = (beamSegment.Start + beamSegment.End) / 2f;
                }
            }

            if (playerIntersected)
            {
                clipped.Add(new BeamSegment(beamSegment.Start, playerIntersectPoint, beamSegment.Color));
                intersections.Add(playerIntersectPoint);
            }
            else if (laserIntersected)
            {
                clipped.Add(new BeamSegment(beamSegment.Start, closestIntersect, beamSegment.Color));
                intersections.Add(closestIntersect);
            }
            else if (colorConflict)
            {
                clipped.Add(new BeamSegment(beamSegment.Start, colorConflictPoint, beamSegment.Color));
                intersections.Add(colorConflictPoint);
            }
            else
            {
                clipped.Add(beamSegment);
            }
        }

        return (clipped, intersections);
    }
    
    private bool FindIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, 
        out Vector3 intersection, float tolerance = 0.1f)
    {
        intersection = Vector3.zero;
    
        var d1 = p2 - p1;
        var d2 = p4 - p3;
        var delta = p3 - p1;
    
        var d1d2 = Vector3.Dot(d1, d2);
        var d1d1 = Vector3.Dot(d1, d1);
        var d2d2 = Vector3.Dot(d2, d2);
        var deltaD1 = Vector3.Dot(delta, d1);
        var deltaD2 = Vector3.Dot(delta, d2);
    
        var denominator = d1d1 * d2d2 - d1d2 * d1d2;
    
        if (Mathf.Abs(denominator) < tolerance)
        {
            return false;
        }
    
        float t = (deltaD1 * d2d2 - deltaD2 * d1d2) / denominator;
        float s = (deltaD1 * d1d2 - deltaD2 * d1d1) / denominator;
    
        if (t < 0 || t > 1 || s < 0 || s > 1)
        {
            return false;
        }
    
        Vector3 pointOnLine1 = p1 + t * d1;
        Vector3 pointOnLine2 = p3 + s * d2;
    
        if (Vector3.Distance(pointOnLine1, pointOnLine2) > tolerance)
        {
            return false;
        }
    
        intersection = (pointOnLine1 + pointOnLine2) * 0.5f;
        return true;
    }
    
    private bool LineSegmentsIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        var thicknessThreshold = 0.15f;

        var da = a2 - a1;
        var db = b2 - b1;
        var dc = b1 - a1;

        var da_da = Vector3.Dot(da, da);
        var db_db = Vector3.Dot(db, db);
        var da_db = Vector3.Dot(da, db);
        var da_dc = Vector3.Dot(da, dc);
        var db_dc = Vector3.Dot(db, dc);

        float denom = da_da * db_db - da_db * da_db;

        if (Mathf.Abs(denom) < 0.0001f)
        {
            Vector3[] pointsA = { a1, a2 };
            Vector3[] pointsB = { b1, b2 };
            var minDist = float.MaxValue;
            var closestA = a1;
            var closestB = b1;

            foreach (var pa in pointsA)
            {
                foreach (var pb in pointsB)
                {
                    float dist = Vector3.Distance(pa, pb);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestA = pa;
                        closestB = pb;
                    }
                }
            }

            var tA = Vector3.Dot(closestB - a1, da) / da_da;
            var tB = Vector3.Dot(closestA - b1, db) / db_db;
            tA = Mathf.Clamp01(tA);
            tB = Mathf.Clamp01(tB);
            var projectionA = a1 + tA * da;
            var projectionB = b1 + tB * db;

            if (Vector3.Distance(projectionA, projectionB) <= thicknessThreshold)
            {
                intersection = (projectionA + projectionB) / 2f;
                return true;
            }

            return false;
        }

        var s = (da_db * db_dc - db_db * da_dc) / denom;
        var t = (da_da * db_dc - da_db * da_dc) / denom;

        s = Mathf.Clamp01(s);
        t = Mathf.Clamp01(t);

        var closestPointA = a1 + s * da;
        var closestPointB = b1 + t * db;

        var distance = Vector3.Distance(closestPointA, closestPointB);
        if (distance > thicknessThreshold)
        {
            return false;
        }
        
        var sCheck = Vector3.Dot(closestPointA - a1, da) / da_da;
        var tCheck = Vector3.Dot(closestPointB - b1, db) / db_db;
        if (sCheck < -0.001f || sCheck > 1.001f || tCheck < -0.001f || tCheck > 1.001f)
        {
            return false;
        }
        
        intersection = (closestPointA + closestPointB) / 2f;
        return true;
    }

    private void AdjustReachedAndColors(List<IConnectable> devices, List<BeamSegment> clippedSegments, List<Vector3> intersections)
    {
        foreach (var dev in devices)
        {
            dev.IsReached = false;
        }

        foreach (var gen in _generators)
        {
            gen.IsReached = true;
        }

        var fullSegments = clippedSegments.Where(s => Mathf.Approximately(Vector3.Distance(s.Start, s.End), s.OriginalLength)).ToList();

        var changed = true;
        var maxIterations = 100;
        var iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            foreach (var segment in fullSegments)
            {
                IConnectable startDev = devices.FirstOrDefault(d => Vector3.Distance(d.ConnectionPoint, segment.Start) < 0.1f);
                IConnectable endDevice = devices.FirstOrDefault(d => Vector3.Distance(d.ConnectionPoint, segment.End) < 0.1f);

                if (startDev != null && endDevice != null && startDev.IsReached && !endDevice.IsReached)
                {
                    bool isBlocked = false;
                    for (int i = 0; i < clippedSegments.Count; i++)
                    {
                        var other = clippedSegments[i];
                        if (other.Start == segment.Start && other.End == segment.End)
                        {
                            continue;
                        }

                        if (LineSegmentsIntersect(segment.Start, segment.End, other.Start, other.End, out _))
                        {
                            isBlocked = true;
                            break;
                        }
                    }

                    if (!isBlocked)
                    {
                        var direction = segment.End - segment.Start;
                        var distance = direction.magnitude;
                        direction.Normalize();
                        if (Physics.Raycast(segment.Start, direction, distance, _playerMask))
                        {
                            isBlocked = true;
                        }
                    }

                    if (!isBlocked)
                    {
                        endDevice.IsReached = true;
                        changed = true;
                    }
                }
            }
            iteration++;
        }

        foreach (var device in devices)
        {
            if (!device.IsReached)
            {
                device.EffectiveColor = LaserColor.None;
            }
        }
    }

    private void RenderBeams(List<BeamSegment> segments)
    {
        foreach (var segment in segments)
        {
            var beamPrefab = segment.Color == LaserColor.Red ? _laserBeamRedPrefab : _laserBeamBluePrefab;
            if (beamPrefab == null)
            {
                continue;
            }
            
            var direction = segment.End - segment.Start;
            direction.Normalize();

            var beam = Instantiate(beamPrefab, segment.Start, Quaternion.LookRotation(direction));
            LineRenderer lineRenderer = beam.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.useWorldSpace = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, segment.Start);
                lineRenderer.SetPosition(1, segment.End);
            }
            
            _activeVFX.Add(beam);
        }
    }

    private void RenderSpheres(List<BeamSegment> segments, List<IConnectable> devices)
    {
        var reachedPoints = new HashSet<Vector3>();
        foreach (var segment in segments)
        {
            reachedPoints.Add(segment.Start);
            reachedPoints.Add(segment.End);
        }

        foreach (var device in devices)
        {
            if (device.IsReached && reachedPoints.Contains(device.ConnectionPoint) && device.EffectiveColor != LaserColor.None)
            {
                GameObject spherePrefab = device.EffectiveColor == LaserColor.Red ? _laserSphereRedPrefab : _laserSphereBluePrefab;
                var sphere = Instantiate(spherePrefab, device.ConnectionPoint, Quaternion.identity);
                _activeVFX.Add(sphere);
            }
        }
    }

    private void RenderSparks(List<BeamSegment> segments, List<Vector3> intersections, List<IConnectable> devices)
    {
        var devicePoints = new HashSet<Vector3>(devices.Select(d => d.ConnectionPoint));

        foreach (var segment in segments)
        {
            float length = Vector3.Distance(segment.Start, segment.End);
            if (length < segment.OriginalLength - 0.001f && !devicePoints.Contains(segment.End))
            {
                var spark = Instantiate(_laserHitSparksPrefab, segment.End, Quaternion.identity);
                _activeVFX.Add(spark);
            }
        }

        foreach (var intersection in intersections)
        {
            if (!devicePoints.Contains(intersection))
            {
                var spark = Instantiate(_laserHitSparksPrefab, intersection, Quaternion.identity);
                _activeVFX.Add(spark);
            }
        }

        foreach (var receiver in _receivers)
        {
            var incomingSegments = segments.Where(s => Vector3.Distance(s.End, receiver.ConnectionPoint) < 0.01f && s.Color != LaserColor.None);
            if (incomingSegments.Any() && !incomingSegments.Any(s => s.Color == receiver.RequiredColor))
            {
                var spark = Instantiate(_laserHitSparksPrefab, receiver.ConnectionPoint, Quaternion.identity);
                _activeVFX.Add(spark);
            }
        }
    }

    private void UpdateReceivers(List<BeamSegment> segments)
    {
        foreach (var receiver in _receivers)
        {
            var correctReach = segments.Any(s => Vector3.Distance(s.End, receiver.ConnectionPoint) < 0.1f && s.Color == receiver.RequiredColor);
            receiver.CheckActivation(correctReach);
        }
    }

    private void UpdateDoor()
    {
        var allActivated = _receivers.All(r => r.IsActivated);
        _wall.SetActive(!allActivated);
    }

    private void ClearFX()
    {
        foreach (var fx in _activeVFX)
        {
            Destroy(fx);
        }
        _activeVFX.Clear();
    }
}