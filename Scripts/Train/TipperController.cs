using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TipperController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Объект, который будет вращаться (Ротор)")]
    public Transform rotor;
    [Tooltip("Точка стыковки (должна быть в центре вращения)")]
    public Transform tipperAnchor;
    [Tooltip("Ссылка на триггер-зону")]
    public TipperRelay tipperRelay;

    [Header("Настройки Вращения (Локальная ось Z)")]
    [Tooltip("На сколько градусов повернуть")]
    public float tipAngle = 75f;
    public float rotationSpeed = 15f;
    public float dumpTime = 5f;

    private List<WagonAnchor> wagonsInZone = new List<WagonAnchor>();
    private bool isOperating = false;

    // --- ЛОГИКА РЕЛЕ (Без изменений) ---
    public void AddWagon(WagonAnchor wagon)
    {
        if (wagon != null && !wagonsInZone.Contains(wagon))
            wagonsInZone.Add(wagon);
    }

    public void RemoveWagon(WagonAnchor wagon)
    {
        if (wagon != null && wagonsInZone.Contains(wagon))
            wagonsInZone.Remove(wagon);
    }

    // --- УПРАВЛЕНИЕ ---
    [ContextMenu("Start Tipping")]
    public void StartTipping()
    {
        if (isOperating) return;
        WagonAnchor targetWagon = FindClosestWagon();
        if (targetWagon != null) StartCoroutine(ProcessWagon(targetWagon));
        else Debug.LogWarning("Вагон не найден или далеко!");
    }

    WagonAnchor FindClosestWagon()
    {
        WagonAnchor closest = null;
        float minDistance = float.MaxValue;
        foreach (var wagon in wagonsInZone)
        {
            if (wagon == null) continue;
            float dist = Vector3.Distance(wagon.anchorPoint.position, tipperAnchor.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = wagon;
            }
        }
        return closest;
    }

    IEnumerator ProcessWagon(WagonAnchor wagon)
    {
        isOperating = true;

        // 1. СТЫКОВКА
        if (wagon.rb) wagon.rb.isKinematic = true;

        // Сначала делаем родителем, чтобы зафиксировать "вместе"
        wagon.transform.SetParent(rotor);

        // Подтягиваем позицию вагона к якорю
        // (Локально выравниваем вагон, чтобы его якорь совпал с якорем типера)
        // Самый надежный способ - через вычисление смещения в мировых координатах
        Vector3 offset = tipperAnchor.position - wagon.anchorPoint.position;
        wagon.transform.position += offset;

        // (Опционально) Если нужно выровнять и поворот вагона по рельсам:
        // wagon.transform.rotation = tipperAnchor.rotation;

        yield return new WaitForSeconds(0.5f);

        // 2. ВРАЩЕНИЕ (ОСЬ Z - ОТНОСИТЕЛЬНОЕ)

        // Запоминаем, как ротор стоял ДО начала вращения
        Quaternion initialRot = rotor.localRotation;

        // Вычисляем конечный поворот: "Текущий + 75 градусов по Z"
        // Умножение кватернионов делает "сложение" поворотов
        Quaternion targetRot = initialRot * Quaternion.Euler(0, 0, tipAngle);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (rotationSpeed / Mathf.Abs(tipAngle));
            // Lerp между стартовым и целевым
            rotor.localRotation = Quaternion.Lerp(initialRot, targetRot, t);
            yield return null;
        }
        rotor.localRotation = targetRot;

        // 3. ПАУЗА
        yield return new WaitForSeconds(dumpTime);

        // 4. ВРАЩЕНИЕ ОБРАТНО (к initialRot)
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (rotationSpeed / Mathf.Abs(tipAngle));
            rotor.localRotation = Quaternion.Lerp(targetRot, initialRot, t);
            yield return null;
        }
        rotor.localRotation = initialRot;

        // 5. ОТЦЕПКА
        yield return new WaitForSeconds(0.5f);
        wagon.transform.SetParent(null);
        if (wagon.rb) wagon.rb.isKinematic = false;

        if (wagonsInZone.Contains(wagon)) wagonsInZone.Remove(wagon);
        isOperating = false;
    }

    // Рисуем ось вращения Z, чтобы вы видели, как она стоит
    private void OnDrawGizmos()
    {
        if (rotor != null)
        {
            Gizmos.matrix = rotor.localToWorldMatrix;
            Gizmos.color = Color.blue;
            // Линия оси Z (Синяя стрелка)
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 3);
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        }
    }
}