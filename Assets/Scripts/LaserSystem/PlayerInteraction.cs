using LaserSystem;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField]
    private Transform _holdPoint;
    [SerializeField]
    public float _interactDistance = 3f;
    [SerializeField]
    public LayerMask _interactMask;

    private Connector _connectorInHands;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_connectorInHands == null)
                TryPickUp();
            else
                TryConnectOrDrop();
        }
    }

    private void TryPickUp()
    {
        var camera = Camera.main;
        if (camera == null) return;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, _interactDistance, _interactMask))
        {
            var connector = hit.collider.GetComponentInParent<Connector>();
            if (connector != null)
            {
                connector.PickUp(_holdPoint);
                _connectorInHands = connector;
            }
        }
    }

    private void TryConnectOrDrop()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }
        
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, _interactDistance, _interactMask))
        {
            var node = hit.collider.GetComponentInParent<ConnectionNode>();
            if (node != null && (_connectorInHands == null || node.gameObject != _connectorInHands.gameObject))
            {
                _connectorInHands.AddConnection(node);
                return;
            }
        }

        _connectorInHands.Drop();
        _connectorInHands = null;
    }
}