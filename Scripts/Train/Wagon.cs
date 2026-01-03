using UnityEngine;

public class WagonAnchor : MonoBehaviour
{
    [Tooltip("Пустой объект, который должен стыковаться с якорем опрокидывателя.")]
    public Transform anchorPoint;

    [HideInInspector]
    public Rigidbody rb;

    void Awake()
    {
        // Убедимся, что на вагоне есть Rigidbody для физики
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError($"Wagon {name} requires a Rigidbody component on its root!");
    }
}