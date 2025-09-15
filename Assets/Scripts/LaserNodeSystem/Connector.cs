using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Connector : LaserNode
{
    public GameObject PlacedTool;
    public BoxCollider PlacedToolCollider;
    public LayerMask PlaceObstaclesMask;
    
    // Флаг читаемый извне (LaserGraphManager использует его)
    public bool IsHeldByPlayer { get; private set; } = false;

    private Transform holdParent;
    private Collider coll;

    private void Awake()
    {
        coll = GetComponent<Collider>();
        // гарантируем, что это коннектор по умолчанию
        nodeType = NodeType.Connector;
    }

    /// <summary>
    /// Поднять коннектор в руку у игрока.
    /// По условию: взятие сбрасывает все подключения -> DisconnectAll()
    /// </summary>
    public void PickUp(Transform playerHoldPoint)
    {
        if (IsHeldByPlayer) return;

        IsHeldByPlayer = true;
        holdParent = playerHoldPoint;

        // фиксируем физику, отключаем коллайдер, чтобы не мешал взгляду/стрельбе
       
        if (coll != null) coll.enabled = false;

        // привязать к holdPoint (локальные координаты)
        transform.SetParent(holdParent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // удаляем все подключения при взятии (пункт задания)
        DisconnectAll();
    }

    /// <summary>
    /// Сбросить коннектор на землю
    /// </summary>
    public void Drop()
    {
        if (!IsHeldByPlayer) return;

        IsHeldByPlayer = false;
        holdParent = null;

        // отвязать и вернуть физику
        transform.SetParent(null, true);
        if (coll != null) coll.enabled = true;

        transform.position = GetPlacePosition();
        transform.localRotation = Quaternion.identity;
    }
    
    private Vector3 GetPlacePosition()
    {
        var halfExtents = PlacedToolCollider.size * 0.5f;
        var offset = PlacedToolCollider.center;
        var forwardDistance = 2f;
        
        if (Physics.BoxCast(Camera.main.gameObject.transform.position + offset, halfExtents,
                Camera.main.gameObject.transform.forward, out var forwardHit,
                Quaternion.identity, forwardDistance, PlaceObstaclesMask))
        {
            forwardDistance = forwardHit.distance;
        }
        
        var forwardPos = Camera.main.gameObject.transform.position + Camera.main.gameObject.transform.forward * (forwardDistance - 0.01f);
        var downDistance = 1000f;
        
        if (Physics.BoxCast(forwardPos + offset, halfExtents, Vector3.down, out var downHit, 
                Quaternion.identity, downDistance, PlaceObstaclesMask))
        {
            downDistance = downHit.distance - offset.y;
        }
            
        return forwardPos + Vector3.down * downDistance - offset;
    }

    /// <summary>
    /// При наведении на ноду: подключить/отключить
    /// Вызывается, когда игрок с коннектором в руках нажал ЛКМ по целевой ноде.
    /// </summary>
    public void ToggleConnection(LaserNode target)
    {
        if (!IsHeldByPlayer || target == null || target == this) return;

        if (connections.Contains(target))
        {
            DisconnectFrom(target);
        }
        else
        {
            ConnectTo(target);
        }
    }
}
