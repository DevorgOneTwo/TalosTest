using System;
using JetBrains.Annotations;
using UnityEngine;

namespace TalosTest
{
    public class Interactor : MonoBehaviour
    {
        public Transform CameraTransform;
        public Transform FirstPersonHandRoot;
        public float InteractDistance;
        public LayerMask InteractableLayer;
        public float PlaceDistance;

        public MovableTool HeldTool { get; private set; }
        
        public IInteractable GetLookingAt(LayerMask layerMask, float maxDistance)
        {
            if (Physics.Raycast(CameraTransform.position, CameraTransform.forward, out var hit, maxDistance, layerMask))
            {
                return hit.collider.GetComponentInParent<IInteractable>();
            }

            return null;
        }

        public void PickUpTool(MovableTool tool)
        {
            HeldTool = tool;

            int childCount = FirstPersonHandRoot.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(FirstPersonHandRoot.transform.GetChild(i).gameObject);
            }

            if (tool is not null)
            {
                Instantiate(tool.FirstPersonVisualsPrefab, FirstPersonHandRoot);
            }
        }

        public void HandleInteractInput()
        {
            if (HeldTool is null)
            {
                GetLookingAt(InteractableLayer, InteractDistance)?.Interact(this);
            }
            else
            {
                HeldTool.InteractWithToolInHands(this);
            }
        }

        [CanBeNull]
        public string GetInteractText()
        {
            if (HeldTool is null)
            {
                return GetLookingAt(InteractableLayer, InteractDistance)?.GetInteractText(this);
            }
            else
            {
                return HeldTool.GetInteractWithToolInHandsText(this);
            }
        }

        public void AddConnectionToHeldTool(IInteractable interactable)
        {
            
        }
    }
}