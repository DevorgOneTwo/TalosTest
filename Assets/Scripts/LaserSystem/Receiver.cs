using UnityEngine;

namespace LaserSystem
{
    public class Receiver : ConnectionNode
    {
        [SerializeField]
        private EnergyType _energyType;
        
        private bool _isActive;
        
        public override EnergyType EnergyType => _energyType;
        public override NodeType NodeType => NodeType.Receiver;

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        private EnergyType GetEnergyTypeByConnections()
        {
            return EnergyType.None;
        }
    }
}