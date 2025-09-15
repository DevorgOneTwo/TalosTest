using System.Collections.Generic;
using UnityEngine;

public class LaserGraphManager : MonoBehaviour
{
    [Header("References")]
    public GameObject laserPathPrefab; // dashed line prefab (optional)
    public GameObject laserBeamRedPrefab;
    public GameObject laserBeamBluePrefab;
    public GameObject laserBeamGreenPrefab; // optional
    public GameObject laserSphereRedPrefab;
    public GameObject laserSphereBluePrefab;
    public GameObject laserSphereGreenPrefab;
    public GameObject laserHitSparksPrefab;

    [Header("Settings")]
    public float segmentCollisionThreshold = 0.25f; // distance to consider beams intersecting
    public LayerMask blockingLayers; // level geometry + player layer
    public LayerMask playerLayerMask; // mask to detect player-specific hits (optional)
    public Transform playerTransform; // optional reference to player
    public int maxDepth = 64; // safety

    private List<Generator> generators = new List<Generator>();
    private List<Connector> connectors = new List<Connector>();
    private List<Receiver> receivers = new List<Receiver>();

    private LaserRenderer rendererComponent;

    private void Awake()
    {
        rendererComponent = GetComponent<LaserRenderer>();
        if (rendererComponent == null) rendererComponent = gameObject.AddComponent<LaserRenderer>();
        rendererComponent.Setup(this);
    }

    private void Start()
    {
        RefreshNodeLists();
    }

    [ContextMenu("RefreshNodeLists")]
    public void RefreshNodeLists()
    {
        generators.Clear(); connectors.Clear(); receivers.Clear();
        foreach (var n in FindObjectsOfType<LaserNode>())
        {
            if (n is Generator g) generators.Add(g);
            else if (n is Connector c) connectors.Add(c);
            else if (n is Receiver r) receivers.Add(r);
        }
    }

    private void Update()
    {
        PropagateAll();
    }

    public void PropagateAll()
    {
        rendererComponent.ClearAll();

        var activeBeams = new List<LaserBeam>();

        foreach (var gen in generators)
        {
            var type = gen.laserType;
            var beamsFromGen = PropagateFromGenerator(gen, type);
            activeBeams.AddRange(beamsFromGen);
        }

        // Detect beam-beam intersections (segment-segment closest points)
        for (int i = 0; i < activeBeams.Count; i++)
        {
            for (int j = i + 1; j < activeBeams.Count; j++)
            {
                var a = activeBeams[i];
                var b = activeBeams[j];

                // skip same color
                if (a.laserType == b.laserType) continue;

                // skip beams that were blocked by player — они не влияют на другие
                if (a.blockedByPlayer || b.blockedByPlayer) continue;

                if (SegmentUtility.SegmentSegmentClosestPoints(a.start, a.end, b.start, b.end, out Vector3 pa, out Vector3 pb))
                {
                    var dist = Vector3.Distance(pa, pb);
                    if (dist <= segmentCollisionThreshold)
                    {
                        var collision = (pa + pb) * 0.5f;

                        a.end = collision;
                        b.end = collision;

                        a.hitSpark = true;
                        b.hitSpark = true;
                        a.hitPoint = collision;
                        b.hitPoint = collision;

                        // since LaserBeam is a class, we updated fields directly
                    }
                }
            }
        }

        // Render beams
        foreach (var beam in activeBeams)
        {
            rendererComponent.RenderBeam(beam);
        }
    }

    private List<LaserBeam> PropagateFromGenerator(Generator gen, LaserColorType type)
    {
        var result = new List<LaserBeam>();
        var stack = new Stack<PathState>();

        // initial push: all adjacent nodes
        foreach (var n in gen.connections)
        {
            if (n is Connector c && c.IsHeldByPlayer) continue; // ignore held connector
            stack.Push(new PathState { current = n, prev = gen, depth = 1, path = new List<LaserNode> { gen, n } });
        }

        while (stack.Count > 0)
        {
            var st = stack.Pop();
            if (st.depth > maxDepth) continue;

            Vector3 from = st.prev.Position;
            Vector3 to = st.current.Position;

            RaycastHit hit;
            bool blocked = Physics.Raycast(from, (to - from).normalized, out hit, Vector3.Distance(from, to), blockingLayers);

            if (blocked)
            {
                var beam = new LaserBeam(st.prev, st.current, type);
                beam.start = from;
                beam.end = hit.point;

                // determine whether hit was the player (so this beam should not affect others)
                bool hitPlayer = false;
                if (playerTransform != null && hit.collider != null && hit.collider.transform.IsChildOf(playerTransform)) hitPlayer = true;
                if (hit.collider != null && hit.collider.CompareTag("Player")) hitPlayer = true;
                if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayerMask) != 0) hitPlayer = true;

                beam.blockedByPlayer = hitPlayer;
                beam.hitSpark = true;
                beam.hitPoint = hit.point;

                result.Add(beam);
                continue; // stop path here
            }

            // not blocked — full segment
            var seg = new LaserBeam(st.prev, st.current, type);
            result.Add(seg);

            // if we reached receiver — stop and set state
            if (st.current is Receiver rec)
            {
                bool matches = (type == rec.requiredType);
                rec.SetActive(matches);
                continue;
            }

            // if connector — continue to its neighbors
            if (st.current is Connector conn)
            {
                foreach (var next in conn.connections)
                {
                    if (next == st.prev) continue;
                    if (next is Connector c2 && c2.IsHeldByPlayer) continue;

                    // prevent simple cycles: don't revisit nodes already in path
                    if (st.path.Contains(next))
                        continue;

                    var newPath = new List<LaserNode>(st.path);
                    newPath.Add(next);
                    stack.Push(new PathState { current = next, prev = st.current, depth = st.depth + 1, path = newPath });
                }
            }
        }

        return result;
    }

    private struct PathState
    {
        public LaserNode current;
        public LaserNode prev;
        public int depth;
        public List<LaserNode> path;
    }
}