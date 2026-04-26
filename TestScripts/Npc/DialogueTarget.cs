using UnityEngine;
using Yarn.Unity;

public class DialogueTarget : MonoBehaviour, IInteractable
{
    [Header("Настройки диалога")]
    public string dialogueNode = "Start";

    private DialogueRunner dialogueRunner;

    void Start()
    {
        dialogueRunner = FindObjectOfType<DialogueRunner>();
    }

    public void Interact(GameObject player)
    {
        if (dialogueRunner == null || dialogueRunner.IsDialogueRunning) return;
        dialogueRunner.StartDialogue(dialogueNode);
    }

    public void SecondaryInteract(GameObject player)
    {
        // Людей нельзя брать в физические руки (если только это не фича!)
    }

    public void OnHoverEnter()
    {
        // Включить подсветку персонажа
    }

    public void OnHoverExit()
    {
        // Выключить подсветку персонажа
    }
}