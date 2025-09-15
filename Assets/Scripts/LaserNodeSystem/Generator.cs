using UnityEngine;


[DisallowMultipleComponent]
public class Generator : LaserNode
{
    private void Reset()
    {
        nodeType = NodeType.Generator;
    }
}