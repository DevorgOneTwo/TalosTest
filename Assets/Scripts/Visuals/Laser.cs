using UnityEngine;

namespace TalosTest
{
    // based on Hovl Studio laser script
    [RequireComponent(typeof(LineRenderer))]
    public class Laser : MonoBehaviour
    {

        public float MaxLength;

        public float MainTextureLength = 1f;
        public float NoiseTextureLength = 1f;
            
        private LineRenderer lineRenderer;
        private ParticleSystem[] _effects;

        private Vector4 _length = new Vector4(1, 1, 1, 1);

        private bool _laserSaver = false;
        private bool _updateSaver = false;

        void Start()
        {
            //Get LineRender and ParticleSystem components from current prefab;  
            lineRenderer = GetComponent<LineRenderer>();
            _effects = GetComponentsInChildren<ParticleSystem>();
        }

        void Update()
        {
            lineRenderer.material.SetTextureScale("_MainTex", new Vector2(_length[0], _length[1]));
            lineRenderer.material.SetTextureScale("_Noise", new Vector2(_length[2], _length[3]));
            // //To set LineRender position
            // if (lineRenderer != null && _updateSaver == false)
            // {
            //     lineRenderer.SetPosition(0, transform.position);
            //     var EndPos = transform.position + transform.forward * MaxLength;
            //     lineRenderer.SetPosition(1, EndPos);
            //
            //     //Texture tiling
            //     _length[0] = MainTextureLength * (Vector3.Distance(transform.position, EndPos));
            //     _length[2] = NoiseTextureLength * (Vector3.Distance(transform.position, EndPos));
            //
            //     //Insurance against the appearance of a laser in the center of coordinates!
            //     if (lineRenderer.enabled == false && _laserSaver == false)
            //     {
            //         _laserSaver = true;
            //         lineRenderer.enabled = true;
            //     }
            // }
        }
    }
}