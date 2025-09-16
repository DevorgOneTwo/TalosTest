using UnityEngine;

namespace LaserSystem
{
    public class Connector : ConnectionNode
    {
        [SerializeField]
        private BoxCollider _placedToolCollider;
        [SerializeField]
        private LayerMask _placeObstaclesMask;
        [SerializeField]
        private Collider _collider;

        public override EnergyType EnergyType { get; set; }
        public override NodeType NodeType => NodeType.Connector;

        private bool IsHeldByPlayer { get; set; }

        protected override void Start()
        {
            base.Start();
            _collider = GetComponentInChildren<BoxCollider>();
        }

        public void PickUp(Transform playerHoldPoint)
        {
            if (IsHeldByPlayer)
            {
                return;
            }
            
            IsHeldByPlayer = true;
            
            _collider.enabled = false;
        
            transform.SetParent(playerHoldPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            ClearConnections();
        }
        
        public void Drop()
        {
            if (!IsHeldByPlayer)
            {
                return;
            }
            
            IsHeldByPlayer = false;
            
            _collider.enabled = true;
            transform.SetParent(null, true);
            transform.position = GetPlacePosition();
            transform.localRotation = Quaternion.identity;
        }
        
        private Vector3 GetPlacePosition()
        {
            var halfExtents = _placedToolCollider.size * 0.5f;
            var offset = _placedToolCollider.center;
            var forwardDistance = 2f;
        
            if (Physics.BoxCast(Camera.main.gameObject.transform.position + offset, halfExtents,
                    Camera.main.gameObject.transform.forward, out var forwardHit,
                    Quaternion.identity, forwardDistance, _placeObstaclesMask))
            {
                forwardDistance = forwardHit.distance;
            }
        
            var forwardPos = Camera.main.gameObject.transform.position + Camera.main.gameObject.transform.forward * (forwardDistance - 0.01f);
            var downDistance = 1000f;
        
            if (Physics.BoxCast(forwardPos + offset, halfExtents, Vector3.down, out var downHit, 
                    Quaternion.identity, downDistance, _placeObstaclesMask))
            {
                downDistance = downHit.distance - offset.y;
            }
            
            return forwardPos + Vector3.down * downDistance - offset;
        }

    }
}