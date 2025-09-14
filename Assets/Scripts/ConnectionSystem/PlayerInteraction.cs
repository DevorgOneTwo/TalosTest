using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] public Camera _playerCamera;
    [SerializeField] public float _interactionDistance = 2f;
    [SerializeField] public LayerMask _interactionMask;

    private Connector _heldConnector;

    public void HandleInteractInput()
    {
        var ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionMask))
        {
            if (_heldConnector == null)
            {
                var connector = hit.collider.GetComponentInParent<Connector>();
                if (connector != null)
                {
                    _heldConnector = connector;
                    connector.Pickup();
                }
            }
            else
            {
                var target = hit.collider.GetComponentInParent<IConnectable>();
                if (target != null && target != _heldConnector)
                {
                    _heldConnector.Connect(target);
                }
            }
        }
        else if (_heldConnector != null)
        {
            _heldConnector.Place(this);
            _heldConnector = null;
        }
    }
}