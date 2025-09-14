using UnityEngine;

public class Receiver : ConnectionDevice
{
    [SerializeField] private LaserColor requiredColor;

    public override bool IsPropagator => false;

    public bool IsActivated { get; private set; }

    public LaserColor RequiredColor => requiredColor;

    private void Start()
    {
        LaserController.Instance.RegisterReceiver(this);
    }
    
    public void CheckActivation(bool reachedWithCorrectColor)
    {
        IsActivated = reachedWithCorrectColor;
    }
}