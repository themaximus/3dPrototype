using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GrassInteractor : MonoBehaviour
{
    [Header("Настройки Взаимодействия")]
    [Range(0.1f, 5f)] public float radius = 1.0f; // Радиус приминания
    [Range(0.1f, 5f)] public float strength = 2.0f; // Сила отталкивания

    // Глобальный список всех интеракторов в сцене
    public static List<GrassInteractor> AllInteractors = new List<GrassInteractor>();

    private void OnEnable()
    {
        if (!AllInteractors.Contains(this)) AllInteractors.Add(this);
    }

    private void OnDisable()
    {
        if (AllInteractors.Contains(this)) AllInteractors.Remove(this);
    }

    // Рисуем сферу в редакторе для удобства
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}