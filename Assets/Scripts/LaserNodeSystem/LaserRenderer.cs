using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Рисует лазеры и вспомогательные эффекты через префабы из менеджера.
/// </summary>
[RequireComponent(typeof(LaserGraphManager))]
public class LaserRenderer : MonoBehaviour
{
    private LaserGraphManager manager;
    private List<GameObject> activeObjects = new List<GameObject>();

    public void Setup(LaserGraphManager mgr)
    {
        manager = mgr;
    }

    public void ClearAll()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            var go = activeObjects[i];
            if (go != null) Destroy(go);
        }
        activeObjects.Clear();
    }

    public void RenderBeam(LaserBeam beam)
    {
        if (beam == null) return;

        Vector3 from = beam.start;
        Vector3 to = beam.GetClampedEnd();

        // 1) основной визуал (beam prefab)
        SpawnBeamPrefab(from, to, beam.laserType);

        // 2) сферы на нодах: старт всегда; конец — только если луч дошёл до ноды полностью
        if (beam.startNode != null)
            SpawnNodeSphere(beam.startNode.Position, beam.laserType);

        if (beam.endNode != null)
        {
            // если луч дошёл до ноды (maxReach >= fullLength) — отрисовываем сферу на конце
            if (beam.maxReachDistance >= beam.fullLength - 1e-5f)
                SpawnNodeSphere(beam.endNode.Position, beam.laserType);
        }

        // 3) пунктирный Path всегда от ноды к ноде (по заданию)
        if (beam.startNode != null && beam.endNode != null)
            SpawnPath(beam.startNode.Position, beam.endNode.Position);

        // 4) спарк (если нужно)
        if (beam.hitSpark)
            SpawnSpark(beam.geometryHitPoint == Vector3.zero ? beam.playerHitPoint : beam.geometryHitPoint);
    }

    private void SpawnBeamPrefab(Vector3 from, Vector3 to, LaserColorType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case LaserColorType.Red: prefab = manager.laserBeamRedPrefab; break;
            case LaserColorType.Blue: prefab = manager.laserBeamBluePrefab; break;
            case LaserColorType.Green: prefab = manager.laserBeamGreenPrefab; break;
            default: prefab = null; break;
        }

        if (prefab != null)
        {
            var go = Instantiate(prefab, transform);
            var lr = go.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
            }
            activeObjects.Add(go);
            return;
        }

        // fallback — простой LineRenderer если префаб не задан
        var fallback = new GameObject("LaserFallback");
        fallback.transform.SetParent(transform, true);
        var line = fallback.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.widthMultiplier = 0.05f;
        activeObjects.Add(fallback);
    }

    private void SpawnNodeSphere(Vector3 pos, LaserColorType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case LaserColorType.Red: prefab = manager.laserSphereRedPrefab; break;
            case LaserColorType.Blue: prefab = manager.laserSphereBluePrefab; break;
            case LaserColorType.Green: prefab = manager.laserSphereGreenPrefab; break;
            default: prefab = null; break;
        }

        if (prefab != null)
        {
            var go = Instantiate(prefab, pos, Quaternion.identity, transform);
            activeObjects.Add(go);
        }
    }

    private void SpawnPath(Vector3 from, Vector3 to)
    {
        if (manager.laserPathPrefab == null) return;
        var go = Instantiate(manager.laserPathPrefab, transform);
        var lr = go.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }
        activeObjects.Add(go);
    }

    private void SpawnSpark(Vector3 pos)
    {
        if (manager.laserHitSparksPrefab != null)
        {
            var fx = Instantiate(manager.laserHitSparksPrefab, pos, Quaternion.identity, transform);
            activeObjects.Add(fx);
        }
    }
}
