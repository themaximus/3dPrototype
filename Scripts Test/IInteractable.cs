using UnityEngine;

public interface IInteractable
{
    // Срабатывает при клике (E/F)
    void Interact(GameObject player);

    // Срабатывает, когда луч попал на объект
    void OnHoverEnter();

    // Срабатывает, когда луч ушел с объекта
    void OnHoverExit();
}