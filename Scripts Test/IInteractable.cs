using UnityEngine;

// Это наш строгий контракт. Любой объект, с которым можно взаимодействовать,
// обязан иметь эти 4 метода.
public interface IInteractable
{
    // Основное действие (Нажатие F) - Взять в инвентарь, Открыть дверь, Начать диалог
    void Interact(GameObject player);

    // Вторичное действие (Нажатие G) - Взять физически в руки
    void SecondaryInteract(GameObject player);

    // Срабатывает, когда луч игрока смотрит на предмет
    void OnHoverEnter();

    // Срабатывает, когда луч игрока уходит с предмета
    void OnHoverExit();
}