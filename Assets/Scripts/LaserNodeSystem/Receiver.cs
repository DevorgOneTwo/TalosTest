using UnityEngine;
using System;


[DisallowMultipleComponent]
public class Receiver : LaserNode
{
    public LaserColorType requiredType = LaserColorType.Red;
    public bool IsActive { get; private set; }
    public event Action<bool> OnStateChanged;


    private void Reset()
    {
        nodeType = NodeType.Receiver;
    }


    public void SetActive(bool a)
    {
        if (IsActive == a) return;
        IsActive = a;
        OnStateChanged?.Invoke(IsActive);
    }
}