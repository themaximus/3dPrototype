using UnityEngine;

public class TrainCoupler : MonoBehaviour
{
    public enum CouplerType { Front, Rear }

    [Header("Настройки")]
    public CouplerType type;
    public TrainBogie myBogie;

    [Header("Взаимодействие")]
    public float interactionRadius = 1.5f;
    public LayerMask couplerLayer;
    public bool alignWithRail = true;

    void Awake()
    {
        if (myBogie == null) myBogie = GetComponentInParent<TrainBogie>();
    }

    void Update()
    {
        // Нажатие E (инициация сцепки/расцепки)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Camera.main && Vector3.Distance(transform.position, Camera.main.transform.position) < 4.0f)
            {
                Debug.Log($"[Coupler] 🟡 Игрок нажал E на сцепке {name} ({type}). Начинаем процесс...");
                TryInteract();
            }
        }
    }

    // --- ДЕБАГ ВХОДА В ЗОНУ (ИСПРАВЛЕНО) ---
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & couplerLayer) != 0)
        {
            TrainCoupler otherCoupler = other.GetComponent<TrainCoupler>();
            if (otherCoupler != null)
            {
                Debug.Log($"[Coupler] 👀 КОНТАКТ! Сцепка '{other.name}' вошла в зону '{name}'.");

                // Логика аварии (если врезались, но не сцеплены)
                if (otherCoupler.myBogie != myBogie)
                {
                    if (myBogie.leaderBogie != otherCoupler.myBogie && otherCoupler.myBogie.leaderBogie != myBogie)
                    {
                        // БЫЛО: other.myBogie.name (Ошибка)
                        // СТАЛО: otherCoupler.myBogie.name (Правильно)
                        Debug.LogWarning($"[Coupler] 💥 Обнаружено столкновение с несцепленным вагоном {otherCoupler.myBogie.name}!");

                        // Раскомментируйте, когда захотите включить остановку
                        // myBogie.TriggerEmergencyStop(); 
                    }
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & couplerLayer) != 0)
        {
            Debug.Log($"[Coupler] 👋 Сцепка '{other.name}' покинула зону '{name}'.");
        }
    }
    // ---------------------------

    void LateUpdate()
    {
        if (alignWithRail && myBogie != null && myBogie.currentRail != null)
        {
            float distOnRail = (type == CouplerType.Front) ? myBogie.distanceOnRail : myBogie.rearRailDist;
            Vector3 railPos; Quaternion railRot;
            myBogie.currentRail.GetPointAtDistance(distOnRail, out railPos, out railRot);
            transform.rotation = railRot;
        }
    }

    public void TryInteract()
    {
        if (myBogie.leaderBogie != null)
        {
            Debug.Log($"[Coupler] 🔓 Расцепляем вагон {myBogie.name} от {myBogie.leaderBogie.name}...");
            myBogie.leaderBogie = null;
            myBogie.connectedViaRear = false; // Сбрасываем флаг толкания
            Debug.Log("[Coupler] ✅ Вагон отцеплен успешно.");
            return;
        }

        Debug.Log($"[Coupler] 🔍 Ищем подходящую сцепку в радиусе {interactionRadius}м...");
        TrainCoupler partner = FindPartner();

        if (partner != null)
        {
            Debug.Log($"[Coupler] ✅ Партнер найден: '{partner.name}' (Тип: {partner.type})");

            if (this.type == CouplerType.Front && partner.type == CouplerType.Rear)
            {
                myBogie.leaderBogie = partner.myBogie;
                myBogie.connectedViaRear = false;
                Debug.Log($"[Coupler] 🔗 УСПЕШНО! {myBogie.name} прицепился ПЕРЕДОМ к {partner.myBogie.name}");
            }
            else if (this.type == CouplerType.Rear && partner.type == CouplerType.Front)
            {
                myBogie.leaderBogie = partner.myBogie;
                myBogie.connectedViaRear = true; // Включаем режим толкания
                Debug.Log($"[Coupler] 🔗 УСПЕШНО! {myBogie.name} прицепился ЗАДОМ к {partner.myBogie.name} (Толкание)");
            }
            else
            {
                Debug.LogError($"[Coupler] ❌ ОШИБКА СЦЕПКИ! Несовместимые типы: {this.type} и {partner.type}.");
            }
        }
        else
        {
            Debug.LogWarning("[Coupler] ❌ Партнер не найден! Подгоните вагоны ближе.");
        }
    }

    TrainCoupler FindPartner()
    {
        // ДЕБАГ: Показываем, где ищем
        Debug.Log($"[DeepDebug] Запускаем OverlapSphere в точке {transform.position} радиусом {interactionRadius} на слое {couplerLayer.value}");

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, couplerLayer);

        if (hits.Length == 0)
        {
            Debug.LogWarning("[DeepDebug] ❌ OverlapSphere ничего не нашел! Проверьте слои и дистанцию.");
            return null;
        }

        float minDst = float.MaxValue;
        TrainCoupler best = null;

        foreach (var hit in hits)
        {
            // 1. Проверка компонента
            TrainCoupler other = hit.GetComponent<TrainCoupler>();
            if (other == null)
            {
                Debug.Log($"[DeepDebug] Объект '{hit.name}' попал в радиус, но на нем нет скрипта TrainCoupler.");
                continue;
            }

            // 2. Проверка "Свой/Чужой"
            if (other.myBogie == myBogie)
            {
                Debug.Log($"[DeepDebug] Нашли '{other.name}', но это сцепка НАШЕГО вагона. Пропускаем.");
                continue;
            }

            // 3. Проверка Типа
            if (other.type == this.type)
            {
                Debug.Log($"[DeepDebug] Нашли '{other.name}', но типы совпадают ({this.type} <-> {other.type}). Нельзя сцепить.");
                continue;
            }

            // 4. Если дошли сюда - кандидат подходит!
            float dst = Vector3.Distance(transform.position, other.transform.position);
            Debug.Log($"[DeepDebug] ✅ КАНДИДАТ! '{other.name}' подходит. Дистанция: {dst}");

            if (dst < minDst)
            {
                minDst = dst;
                best = other;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (type == CouplerType.Front) ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}