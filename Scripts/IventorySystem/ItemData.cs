using UnityEngine;

public enum ItemType
{
    Default,
    Weapon,
    Consumable,
    Equipment
    // Placeable (на будущее)
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Default;

    [Header("World")]
    public GameObject worldPrefab;

    [Header("Equipping")]
    public GameObject handModelPrefab;

    [Header("Stacking")]
    public bool isStackable = false;
    public int maxStackSize = 1;

    // --- НОВОЕ ПОЛЕ ---
    [Header("Physics")]
    [Tooltip("Вес предмета. 1 = обычный. 5 = тяжелый (медленно тащится).")]
    [Range(0.1f, 20f)]
    public float weight = 1.0f;
    // ------------------

    public virtual void Use(GameObject user)
    {
        Debug.Log("Using: " + itemName + " by " + user.name);
    }
}