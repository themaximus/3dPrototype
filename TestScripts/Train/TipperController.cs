using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TipperController : MonoBehaviour
{
    [Header("Компоненты")]
    public Transform rotor;
    public Transform tipperAnchor; // Точка, куда должен примагнититься якорь вагона
    public TipperRelay tipperRelay;

    [Header("Настройки Вращения (Ось Z)")]
    public float tipAngle = 170f;
    public float rotationSpeed = 15f;
    public float dumpTime = 3f;

    private List<WagonAnchor> wagonsInZone = new List<WagonAnchor>();
    private bool isOperating = false;

    public void AddWagon(WagonAnchor wagon)
    {
        if (wagon != null && !wagonsInZone.Contains(wagon)) wagonsInZone.Add(wagon);
    }

    public void RemoveWagon(WagonAnchor wagon)
    {
        if (wagon != null && wagonsInZone.Contains(wagon)) wagonsInZone.Remove(wagon);
    }

    [ContextMenu("Start Tipping")]
    public void StartTipping()
    {
        if (isOperating) return;
        WagonAnchor targetWagon = FindClosestWagon();
        if (targetWagon != null) StartCoroutine(ProcessWagon(targetWagon));
        else Debug.LogWarning("Tipper: Вагон не найден!");
    }

    WagonAnchor FindClosestWagon()
    {
        WagonAnchor closest = null;
        float minDistance = 3.0f;
        foreach (var wagon in wagonsInZone)
        {
            if (wagon == null) continue;
            float dist = Vector3.Distance(wagon.anchorPoint.position, tipperAnchor.position);
            if (dist < minDistance) { minDistance = dist; closest = wagon; }
        }
        return closest;
    }

    IEnumerator ProcessWagon(WagonAnchor wagon)
    {
        isOperating = true;

        TrainBogie bogie = wagon.GetComponent<TrainBogie>();
        if (bogie == null) bogie = wagon.GetComponentInParent<TrainBogie>();

        // 1. ОТКЛЮЧАЕМ ФИЗИКУ
        if (bogie != null) bogie.isLockedByTipper = true;

        // 2. ПРИВЯЗКА И РАСЧЕТ
        wagon.transform.SetParent(rotor);

        Vector3 startPos = wagon.transform.position;
        Quaternion startRot = wagon.transform.rotation;

        // --- МАТЕМАТИКА ВЫРАВНИВАНИЯ ---
        // Мы хотим, чтобы wagon.anchorPoint совпал с tipperAnchor.

        // Шаг А: Вычисляем нужный поворот КОРНЯ вагона.
        // Формула: TargetRootRot = TipperAnchorRot * Inverse(AnchorLocalRot)
        // Это "вычитает" поворот якоря из целевого поворота.
        Quaternion targetWagonRot = tipperAnchor.rotation * Quaternion.Inverse(wagon.anchorPoint.localRotation);

        // Шаг Б: Вычисляем нужную позицию КОРНЯ вагона.
        // Нам нужно сместить корень так, чтобы якорь оказался в точке tipperAnchor.position.
        // Учитываем, что вагон уже повернут по-новому (targetWagonRot).
        Vector3 targetWagonPos = tipperAnchor.position - (targetWagonRot * wagon.anchorPoint.localPosition);

        // --- ПЛАВНОЕ ПЕРЕМЕЩЕНИЕ ---
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f; // Скорость стыковки (0.5 сек)
            wagon.transform.position = Vector3.Lerp(startPos, targetWagonPos, t);
            wagon.transform.rotation = Quaternion.Lerp(startRot, targetWagonRot, t);
            yield return null;
        }
        // Финальная фиксация
        wagon.transform.position = targetWagonPos;
        wagon.transform.rotation = targetWagonRot;

        yield return new WaitForSeconds(0.2f);

        // 3. ВРАЩЕНИЕ РОТОРА (Ось Z)
        Quaternion initialRotorRot = rotor.localRotation;
        Quaternion targetRotorRot = initialRotorRot * Quaternion.Euler(0, 0, tipAngle);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (rotationSpeed / Mathf.Abs(tipAngle));
            rotor.localRotation = Quaternion.Lerp(initialRotorRot, targetRotorRot, t);
            yield return null;
        }
        rotor.localRotation = targetRotorRot;

        // 4. СБРОС
        yield return new WaitForSeconds(dumpTime);

        // 5. ВОЗВРАТ
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (rotationSpeed / Mathf.Abs(tipAngle));
            rotor.localRotation = Quaternion.Lerp(targetRotorRot, initialRotorRot, t);
            yield return null;
        }
        rotor.localRotation = initialRotorRot;

        yield return new WaitForSeconds(0.5f);

        // 6. ОТПУСКАЕМ
        wagon.transform.SetParent(null);

        if (bogie != null)
        {
            bogie.isLockedByTipper = false;
            bogie.ForceUpdatePosition();
        }

        isOperating = false;
    }

    private void OnDrawGizmos()
    {
        if (tipperAnchor != null)
        {
            Gizmos.matrix = tipperAnchor.localToWorldMatrix;
            Gizmos.color = Color.green; Gizmos.DrawLine(Vector3.zero, Vector3.up * 2);     // Верх
            Gizmos.color = Color.blue; Gizmos.DrawLine(Vector3.zero, Vector3.forward * 2); // Вперед
            Gizmos.DrawWireSphere(Vector3.zero, 0.2f);
        }
    }
}