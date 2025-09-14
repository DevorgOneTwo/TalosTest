using UnityEngine;
using UnityEngine.Events;

public class Connector : ConnectionDevice
{
    public GameObject PlacedTool;
    public BoxCollider PlacedToolCollider;
    public GameObject FirstPersonVisualsPrefab;
    public LayerMask PlaceObstaclesMask;
    
    public UnityEvent OnConnectionsChanged = new (); // Для уведомления менеджера

    public override bool IsPropagator => true;

    private bool _isHeld = false;
    public override bool IsHeld => _isHeld;
    
    public void Pickup()
    {
        ClearConnections();
        _isHeld = true;
        OnConnectionsChanged.Invoke();
        
        transform.position = Vector3.zero;
        PlacedTool.SetActive(false);
    }

    public void Place(PlayerInteraction interactor)
    {
        _isHeld = false;
        
        transform.position = GetPlacePosition(interactor);
        transform.rotation = Quaternion.identity;
        
        PlacedTool.SetActive(true);
        OnConnectionsChanged.Invoke();
    }

    public void Connect(IConnectable target)
    {
        AddConnection(target);
    }

    private void Start()
    {
        LaserController.Instance.RegisterConnector(this);
        OnConnectionsChanged.AddListener(LaserController.Instance.UpdateLasers);
    }

    private void OnDestroy()
    {
        OnConnectionsChanged.RemoveListener(LaserController.Instance.UpdateLasers);
    }

    private Vector3 GetPlacePosition(PlayerInteraction interactor)
    {
        var halfExtents = PlacedToolCollider.size * 0.5f;
        var offset = PlacedToolCollider.center;
        var forwardDistance = interactor._interactionDistance;
        
        if (Physics.BoxCast(interactor._playerCamera.gameObject.transform.position + offset, halfExtents,
                interactor._playerCamera.gameObject.transform.forward, out var forwardHit,
                Quaternion.identity, forwardDistance, PlaceObstaclesMask))
        {
            forwardDistance = forwardHit.distance;
        }
        
        var forwardPos = interactor._playerCamera.gameObject.transform.position + interactor._playerCamera.gameObject.transform.forward * (forwardDistance - 0.01f);
        var downDistance = 1000f;
        
        if (Physics.BoxCast(forwardPos + offset, halfExtents, Vector3.down, out var downHit, 
                Quaternion.identity, downDistance, PlaceObstaclesMask))
        {
            downDistance = downHit.distance - offset.y;
        }
            
        return forwardPos + Vector3.down * downDistance - offset;
    }
}