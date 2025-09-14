using UnityEngine;

public class Generator : ConnectionDevice
{
    [SerializeField] private LaserColor color;

    public LaserColor Color => color;

    public override bool IsPropagator => true;

    private void Start()
    {
        LaserController.Instance.RegisterGenerator(this);
    }
}