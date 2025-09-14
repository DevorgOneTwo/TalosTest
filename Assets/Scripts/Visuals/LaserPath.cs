using UnityEngine;

namespace TalosTest
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserPath : MonoBehaviour
    {
        public float MaxLength = 30f;
        
        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        void Update()
        {
            // var EndPos = transform.position + transform.forward * MaxLength;
            // _lineRenderer.SetPosition(0, transform.worldToLocalMatrix.MultiplyPoint(transform.position));
            // _lineRenderer.SetPosition(1, transform.worldToLocalMatrix.MultiplyPoint(EndPos));
            // _lineRenderer.textureScale = new Vector2(MaxLength, 1f);
        }
    }
}