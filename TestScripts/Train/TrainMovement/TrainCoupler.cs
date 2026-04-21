using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrainCoupler : MonoBehaviour
{
    public enum CouplerType { Front, Rear }

    public CouplerType type;
    public TrainBogie myBogie;

    [Header("Статус")]
    public TrainCoupler connectedCoupler;
    public bool IsCoupled => connectedCoupler != null;

    private float lastCoupleTime;
    private const float COOLDOWN = 0.5f;

    void Awake()
    {
        if (myBogie == null) myBogie = GetComponentInParent<TrainBogie>();
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        // Проверяем нажатие E с помощью луча
        if (Input.GetKeyDown(KeyCode.E))
        {
            CheckAim();
        }
    }

    void CheckAim()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, 4.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            // Если игрок смотрит ПРЯМО НА ЭТУ сцепку
            if (hit.collider.gameObject == this.gameObject)
            {
                TryInteract();
            }
        }
    }

    public void TryInteract()
    {
        if (Time.time < lastCoupleTime + COOLDOWN) return;
        lastCoupleTime = Time.time;

        if (IsCoupled) Disconnect();
        else TryConnect();
    }

    void TryConnect()
    {
        // Ищем партнера в радиусе 1 метра
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            TrainCoupler other = hit.GetComponent<TrainCoupler>();
            if (other != null && other != this && other.myBogie != myBogie && !other.IsCoupled)
            {
                // Соединяем
                this.connectedCoupler = other;
                other.connectedCoupler = this;
                Debug.Log($"Сцеплено: {name} + {other.name}");
                return;
            }
        }
    }

    public void Disconnect()
    {
        if (connectedCoupler != null)
        {
            connectedCoupler.connectedCoupler = null;
            connectedCoupler = null;
            Debug.Log("Расцеплено");
        }
    }
}