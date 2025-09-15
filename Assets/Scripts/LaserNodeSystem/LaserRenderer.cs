// ===== LaserRenderer.cs =====
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LaserGraphManager))]
public class LaserRenderer : MonoBehaviour
{
    private LaserGraphManager manager;

    // pooling — пока простая: уничтожаем/создаём каждый кадр
    private List<GameObject> activeObjects = new List<GameObject>();

    public void Setup(LaserGraphManager mgr)
    {
        manager = mgr;
    }

    public void ClearAll()
    {
        foreach (var go in activeObjects)
        {
            if (go) Destroy(go);
        }
        activeObjects.Clear();
    }

    public void RenderBeam(LaserBeam beam)
    {
        SpawnBeam(beam.start, beam.end, beam.laserType);

        if (beam.startNode != null)
            SpawnNodeSphere(beam.startNode.Position, beam.laserType);
        if (beam.endNode != null)
            SpawnNodeSphere(beam.endNode.Position, beam.laserType);

        if (beam.hitSpark)
            SpawnSpark(beam.hitPoint);

        if (beam.startNode != null && beam.endNode != null)
            SpawnPath(beam.startNode.Position, beam.endNode.Position);
    }
    
    private void SpawnBeam(Vector3 from, Vector3 to, LaserColorType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case LaserColorType.Red: prefab = manager.laserBeamRedPrefab; break;
            case LaserColorType.Blue: prefab = manager.laserBeamBluePrefab; break;
        }
        if (prefab == null) return;

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
    }

    private void SpawnNodeSphere(Vector3 pos, LaserColorType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case LaserColorType.Red: prefab = manager.laserSphereRedPrefab; break;
            case LaserColorType.Blue: prefab = manager.laserSphereBluePrefab; break;
        }
        if (prefab == null) return;

        var go = Instantiate(prefab, pos, Quaternion.identity, transform);
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
}