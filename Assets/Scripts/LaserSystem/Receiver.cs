using System;
using UnityEngine;

namespace LaserSystem
{
    public class Receiver : ConnectionNode
    {
        [SerializeField]
        private EnergyType _energyType;
        [SerializeField]
        private bool _isActive;
        [SerializeField]
        private GameObject _activeEffect;

        public override bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        
        public override EnergyType EnergyType { get; set; }
        public override NodeType NodeType => NodeType.Receiver;

        private void Awake()
        {
            EnergyType = _energyType;
        }

        private void Update()
        {
            if (_activeEffect != null)
            {
                _activeEffect.SetActive(IsActive && EnergyType == _energyType);
            }
        }
    }
}