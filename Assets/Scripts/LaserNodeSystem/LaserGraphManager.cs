using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер графа: собирает ноды, распространяет лучи от генераторов через коннекторы,
/// учитывает попадания в геометрию/игрока и пересечения между лучами (по типу).
/// </summary>
public class LaserGraphManager : MonoBehaviour
{
    [Header("FX Prefabs")]
    public GameObject laserPathPrefab;
    public GameObject laserBeamRedPrefab;
    public GameObject laserBeamBluePrefab;
    public GameObject laserBeamGreenPrefab;
    public GameObject laserSphereRedPrefab;
    public GameObject laserSphereBluePrefab;
    public GameObject laserSphereGreenPrefab;
    public GameObject laserHitSparksPrefab;

    [Header("Settings")]
    public float segmentCollisionThreshold = 0.25f;
    public LayerMask blockingLayers;     // геометрия + игрок
    public LayerMask playerLayerMask;    // отдельная маска для игрока (если задана)
    public Transform playerTransform;    // опционально
    public int maxDepth = 64;

    private List<Generator> generators = new List<Generator>();
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
        generators.Clear();
        foreach (var n in FindObjectsOfType<LaserNode>())
        {
            if (n is Generator g) generators.Add(g);
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

        // 1) собрать все сегменты (лучи) от каждого генератора
        foreach (var gen in generators)
        {
            var beams = PropagateFromGenerator(gen, gen.laserType);
            activeBeams.AddRange(beams);
        }

        // 2) попарно найти пересечения между лучами разных типов
        int n = activeBeams.Count;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var a = activeBeams[i];
                var b = activeBeams[j];

                if (a.laserType == b.laserType) continue; // одинаковые типы не создают spark

                // используем текущие ограниченные концы (maxReachDistance)
                Vector3 aEndEffective = a.GetPointAtDistance(a.maxReachDistance);
                Vector3 bEndEffective = b.GetPointAtDistance(b.maxReachDistance);

                if (SegmentUtility.SegmentSegmentClosestPoints(a.start, aEndEffective, b.start, bEndEffective, out Vector3 pa, out Vector3 pb))
                {
                    float distBetween = Vector3.Distance(pa, pb);
                    if (distBetween <= segmentCollisionThreshold)
                    {
                        float distAlongA = Vector3.Distance(a.start, pa);
                        float distAlongB = Vector3.Distance(b.start, pb);

                        // Обрабатываем столкновение, только если точки находятся **внутри** доступных отрезков
                        if (distAlongA <= a.maxReachDistance + 1e-5f && distAlongB <= b.maxReachDistance + 1e-5f)
                        {
                            // средняя точка между ближайшими точками
                            Vector3 collisionPoint = (pa + pb) * 0.5f;

                            a.RegisterIntersection(collisionPoint, distAlongA);
                            b.RegisterIntersection(collisionPoint, distAlongB);

                            // сохраняем конкретную точку попадания (позже рендерер её использует)
                            a.geometryHitPoint = collisionPoint;
                            b.geometryHitPoint = collisionPoint;
                        }
                    }
                }
            }
        }

        // 3) финализация: учесть геометрию/игрока (если попали) и отрисовать
        foreach (var beam in activeBeams)
        {
            // если геометрия была ближе или равна текущему maxReach => это реальный хит
            if (beam.geometryHitDistance < float.PositiveInfinity && beam.geometryHitDistance <= beam.maxReachDistance + 1e-5f)
            {
                beam.hitSpark = true;
                beam.end = beam.GetPointAtDistance(beam.geometryHitDistance);
            }

            // если игрок попал в доступный отрезок — он блокирует
            if (beam.playerHitDistance < float.PositiveInfinity && beam.playerHitDistance <= beam.maxReachDistance + 1e-5f)
            {
                beam.blockedByPlayer = true;
                beam.hitSpark = true;
                beam.end = beam.GetPointAtDistance(beam.playerHitDistance);
            }

            // если всё ещё не установлено — поставить конец по текущему ограничению
            beam.end = beam.GetClampedEnd();

            // отрисовать
            rendererComponent.RenderBeam(beam);
        }
    }

    // Построение сегментов от генератора по графу нод
    private List<LaserBeam> PropagateFromGenerator(Generator gen, LaserColorType type)
    {
        var result = new List<LaserBeam>();
        var stack = new Stack<PathState>();

        foreach (var n in gen.connections)
        {
            if (n is Connector c && c.IsHeldByPlayer) continue;
            stack.Push(new PathState { prev = gen, current = n, depth = 1, path = new List<LaserNode> { gen, n } });
        }

        while (stack.Count > 0)
        {
            var st = stack.Pop();
            if (st.depth > maxDepth) continue;

            Vector3 from = st.prev.Position;
            Vector3 to = st.current.Position;
            Vector3 dir = (to - from).normalized;
            float fullDist = Vector3.Distance(from, to);

            // создать объект луча между st.prev и st.current
            var beam = new LaserBeam(st.prev, st.current, type);
            beam.start = from;
            beam.end = to;
            beam.dir = dir;
            beam.fullLength = fullDist;
            beam.maxReachDistance = fullDist; // по умолчанию

            // проверяем коллизию с геометрией/игроком на сегменте
            if (Physics.Raycast(from, dir, out RaycastHit hit, fullDist, blockingLayers))
            {
                // определить — игрок ли это
                bool hitPlayer = false;
                if (playerTransform != null && hit.collider != null && hit.collider.transform.IsChildOf(playerTransform)) hitPlayer = true;
                if (hit.collider != null && hit.collider.CompareTag("Player")) hitPlayer = true;
                if (playerLayerMask != (LayerMask)0 && hit.collider != null)
                {
                    int layerMaskOfHit = (1 << hit.collider.gameObject.layer);
                    if ((layerMaskOfHit & playerLayerMask) != 0) hitPlayer = true;
                }

                if (hitPlayer)
                {
                    beam.RegisterPlayerHit(hit.point, hit.distance);
                    // добавляем и не продолжаем путь дальше за игроком
                    result.Add(beam);
                    continue;
                }
                else
                {
                    beam.RegisterGeometryHit(hit.point, hit.distance);
                    result.Add(beam);
                    continue;
                }
            }

            // не заблокировано — полный сегмент
            result.Add(beam);

            // если получили приёмник — не идём дальше
            if (st.current is Receiver rec)
            {
                bool matches = (type == rec.requiredType);
                rec.SetActive(matches);
                continue;
            }

            // если это коннектор — продолжить по его подключениям
            if (st.current is Connector conn)
            {
                foreach (var next in conn.connections)
                {
                    if (next == st.prev) continue;
                    if (next is Connector c2 && c2.IsHeldByPlayer) continue;
                    if (st.path.Contains(next)) continue; // предотвращаем простые циклы

                    var newPath = new List<LaserNode>(st.path) { next };
                    stack.Push(new PathState { prev = st.current, current = next, depth = st.depth + 1, path = newPath });
                }
            }
        }

        return result;
    }

    private struct PathState
    {
        public LaserNode prev;
        public LaserNode current;
        public int depth;
        public List<LaserNode> path;
    }
}
