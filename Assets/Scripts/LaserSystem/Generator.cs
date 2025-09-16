using System;
using UnityEngine;

namespace LaserSystem
{
    public class Generator : ConnectionNode
    {
        [SerializeField]
        private EnergyType _energyType;

        public override EnergyType EnergyType { get; set; }
        public override NodeType NodeType => NodeType.Generator;

        private void Awake()
        {
            EnergyType = _energyType;
        }
    }
}