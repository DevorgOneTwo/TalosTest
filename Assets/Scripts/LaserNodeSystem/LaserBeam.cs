using UnityEngine;

/// <summary>
/// Класс-структура описывающий сегмент луча между двумя нодами и разные типы «ограничителей» (геометрия, игрок, пересечение).
/// </summary>
public class LaserBeam
{
    public LaserNode startNode;
    public LaserNode endNode;

    public Vector3 start; // позиция старта (world)
    public Vector3 end;   // финальная позиция (рассчитывается позднее)

    public Vector3 dir;   // нормализованное направление от start к endNode
    public float fullLength; // расстояние до endNode

    public LaserColorType laserType;

    // детектированные хит-события
    public float geometryHitDistance = float.PositiveInfinity;
    public Vector3 geometryHitPoint;

    public float playerHitDistance = float.PositiveInfinity;
    public Vector3 playerHitPoint;

    // Итоговая доступная для пересечений длина (может уменьшаться при пересечениях/хитах)
    public float maxReachDistance;

    // флаги
    public bool hitSpark = false;       // true если надо показать спарк (столкновение лучей / попадание в геометрию / игрока)
    public bool blockedByPlayer = false;

    public LaserBeam(LaserNode startNode, LaserNode endNode, LaserColorType type)
    {
        this.startNode = startNode;
        this.endNode = endNode;
        this.laserType = type;

        this.start = startNode != null ? startNode.Position : Vector3.zero;
        this.end = endNode != null ? endNode.Position : start; // временно
        this.dir = (end - start).normalized;
        this.fullLength = Vector3.Distance(start, end);

        geometryHitDistance = float.PositiveInfinity;
        playerHitDistance = float.PositiveInfinity;
        maxReachDistance = fullLength;

        hitSpark = false;
        blockedByPlayer = false;
    }

    // Возвращает точку на луче на расстоянии d от старта (с учётом fullLength)
    public Vector3 GetPointAtDistance(float d)
    {
        float clamped = Mathf.Clamp(d, 0f, fullLength);
        return start + dir * clamped;
    }

    // Текущий реальный конец луча для рендера
    public Vector3 GetClampedEnd()
    {
        return GetPointAtDistance(maxReachDistance);
    }

    // Обрезать (уменьшить) доступную длину до distance (если меньше текущей)
    public void TruncateToDistance(float distance)
    {
        float d = Mathf.Clamp(distance, 0f, fullLength);
        if (d < maxReachDistance) maxReachDistance = d;
    }

    // Зарегистрировать попадание в геометрию (стена/уровень)
    public void RegisterGeometryHit(Vector3 point, float distance)
    {
        if (distance < geometryHitDistance)
        {
            geometryHitDistance = distance;
            geometryHitPoint = point;
            hitSpark = true;
            // геометрия ограничивает достижимую длину сразу
            TruncateToDistance(distance);
        }
    }

    // Зарегистрировать попадание игрока
    public void RegisterPlayerHit(Vector3 point, float distance)
    {
        if (distance < playerHitDistance)
        {
            playerHitDistance = distance;
            playerHitPoint = point;
            blockedByPlayer = true;
            hitSpark = true;
            // игрок тоже ограничивает длину (но окончательное решение — при финализации)
            TruncateToDistance(distance);
        }
    }

    // Зарегистрировать пересечение с другим лучом (середина столкновения)
    public void RegisterIntersection(Vector3 point, float distanceAlongThis)
    {
        // усечь до точки столкновения
        TruncateToDistance(distanceAlongThis);
        hitSpark = true;
    }
}
