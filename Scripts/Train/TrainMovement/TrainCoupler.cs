using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TrainCoupler : MonoBehaviour
{
    public enum CouplerType { Front, Rear }

    [Header("Настройки")]
    public CouplerType type;
    public TrainBogie myBogie;

    [Header("Состояние")]
    public TrainCoupler connectedCoupler; // Наш партнер

    [Header("Поиск (Теги)")]
    [Tooltip("Тег, который ОБЯЗАН быть на объекте якоря (Anchor_Front/Rear)")]
    public string anchorTag = "Anchor";

    [Header("Защита")]
    public float couplingCooldown = 1.0f;
    private float lastCoupleTime = -999f;

    // Список всех подходящих якорей в зоне контакта
    public List<TrainCoupler> potentialPartners = new List<TrainCoupler>();

    public bool IsCoupled => connectedCoupler != null;

    void Awake()
    {
        if (myBogie == null) myBogie = GetComponentInParent<TrainBogie>();

        // Проверка настройки коллайдера
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogError($"[Coupler] ⚠️ ВНИМАНИЕ! На объекте {name} коллайдер должен быть триггером (Is Trigger = True)!");
            col.isTrigger = true; // Чиним сами
        }
    }

    void Update()
    {
        // Чистим список от "мертвых" объектов
        potentialPartners.RemoveAll(x => x == null);

        // Нажатие E
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Camera.main && Vector3.Distance(transform.position, Camera.main.transform.position) < 8.0f)
            {
                TryInteract();
            }
        }
    }

    // --- ВЗАИМОДЕЙСТВИЕ ---

    public void TryInteract()
    {
        // Защита от случайного дабл-клика
        if (Time.time < lastCoupleTime + couplingCooldown) return;

        if (IsCoupled)
        {
            Debug.Log($"[Coupler] 🔓 Отцепляем {name} от {connectedCoupler.name}");
            Disconnect();
        }
        else
        {
            // Ищем лучшего кандидата из тех, кто УЖЕ в триггере
            TrainCoupler partner = GetBestPartner();

            if (partner != null)
            {
                Connect(partner);
            }
            else
            {
                Debug.LogWarning($"[Coupler] ❌ В зоне нет свободных якорей с тегом '{anchorTag}'. (В зоне: {potentialPartners.Count})");
            }
        }
    }

    public void Connect(TrainCoupler other)
    {
        this.connectedCoupler = other;
        other.connectedCoupler = this;

        float now = Time.time;
        this.lastCoupleTime = now;
        other.lastCoupleTime = now;

        Debug.Log($"[Coupler] ✅ СЦЕПКА: {name} + {other.name}");

        // Мгновенно подгоняем вагоны друг к другу
        myBogie.ForceUpdatePosition();
        other.myBogie.ForceUpdatePosition();
    }

    public void Disconnect()
    {
        if (connectedCoupler != null)
        {
            connectedCoupler.connectedCoupler = null;
            this.connectedCoupler = null;
        }
    }

    // --- ТРИГГЕРЫ (Мгновенная реакция) ---

    private void OnTriggerEnter(Collider other)
    {
        // 1. Фильтр по Тегу (самый быстрый)
        if (!other.CompareTag(anchorTag)) return;

        // 2. Проверка компонента
        TrainCoupler otherCoupler = other.GetComponent<TrainCoupler>();
        if (otherCoupler == null) return;

        // 3. Логика "Свой-Чужой"
        if (otherCoupler.myBogie == myBogie) return; // Не цепляем сами себя

        // 4. Добавляем в список
        if (!potentialPartners.Contains(otherCoupler))
        {
            potentialPartners.Add(otherCoupler);
            Debug.Log($"[Coupler] 🟢 {name} коснулся якоря: {other.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(anchorTag)) return;

        TrainCoupler otherCoupler = other.GetComponent<TrainCoupler>();
        if (otherCoupler != null)
        {
            if (potentialPartners.Contains(otherCoupler))
            {
                potentialPartners.Remove(otherCoupler);
                // Debug.Log($"[Coupler] ⚪ {other.name} вышел из зоны.");
            }
        }
    }

    // --- ВЫБОР ПАРТНЕРА ---

    private TrainCoupler GetBestPartner()
    {
        if (potentialPartners.Count == 0) return null;

        TrainCoupler best = null;
        float minDst = float.MaxValue;

        foreach (var p in potentialPartners)
        {
            if (p == null || p.IsCoupled) continue; // Пропускаем занятых

            float dst = Vector3.Distance(transform.position, p.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                best = p;
            }
        }
        return best;
    }
}