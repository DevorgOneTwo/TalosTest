using LaserSystem;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public Transform holdPoint; // точка перед камерой, куда крепится поднятый коннектор
    public float interactDistance = 3f;
    public LayerMask interactMask; // маска для Raycast взаимодействия

    private Connector heldConnector;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (heldConnector == null)
                TryPickUp();
            else
                TryConnectOrDrop();
        }
    }

    private void TryPickUp()
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactMask))
        {
            var connector = hit.collider.GetComponentInParent<Connector>();
            if (connector != null)
            {
                connector.PickUp(holdPoint);
                heldConnector = connector;
            }
        }
    }

    private void TryConnectOrDrop()
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactMask))
        {
            var node = hit.collider.GetComponentInParent<ConnectionNode>();
            // если смотрим на какую-то ноду (и это не сам удерживаемый объект) — переключаем подключение
            if (node != null && (heldConnector == null || node.gameObject != heldConnector.gameObject))
            {
                heldConnector.AddConnection(node);
                return;
            }
        }

        // если не подключили ни к чему — сбрасываем
        heldConnector.Drop();
        heldConnector = null;
    }
}