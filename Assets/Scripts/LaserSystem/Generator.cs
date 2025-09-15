using UnityEngine;

namespace LaserSystem
{
    public class Generator : ConnectionNode
    {
        [SerializeField]
        private EnergyType _energyType;
        
        public override EnergyType EnergyType => _energyType;
        public override NodeType NodeType => NodeType.Connector;
    }
}