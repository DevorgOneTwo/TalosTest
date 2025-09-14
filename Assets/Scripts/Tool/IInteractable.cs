using UnityEngine;

namespace TalosTest
{
    public interface IInteractable
    {
        public string GetInteractText(Interactor interactor);
        public void Interact(Interactor interactor);
    }
}