using UnityEngine;

public class RideableSurface : MonoBehaviour
{
    [Header("Настройки")]
    public string playerTag = "Player";

    private CharacterController playerCC;
    private Transform playerTransform;

    // Храним состояние
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;

    void Start()
    {
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerCC = other.GetComponent<CharacterController>();
            playerTransform = other.transform;

            _lastPosition = transform.position;
            _lastRotation = transform.rotation;

            Debug.Log($"[RideableSurface] Игрок на платформе {name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerCC = null;
            playerTransform = null;
        }
    }

    void LateUpdate()
    {
        Vector3 currentPos = transform.position;
        Quaternion currentRot = transform.rotation;

        // Выполняем логику, только если игрок на платформе и активен
        if (playerCC != null && playerCC.enabled)
        {
            // 1. Вычисляем изменения платформы
            Quaternion deltaRot = currentRot * Quaternion.Inverse(_lastRotation);
            Vector3 eulerRot = deltaRot.eulerAngles;

            // Расчет новой позиции игрока с учетом вращения платформы ("рычаг")
            Vector3 offsetFromPivot = playerTransform.position - _lastPosition;
            Vector3 rotatedOffset = deltaRot * offsetFromPivot;
            Vector3 targetPosition = currentPos + rotatedOffset;

            Vector3 finalMove = targetPosition - playerTransform.position;

            // --- ИСПРАВЛЕНИЕ ---

            // А. Пропускаем вызов Move, если платформа стоит на месте (чтобы не сбивать isGrounded)
            bool isMoving = finalMove.sqrMagnitude > 0.000001f || Mathf.Abs(eulerRot.y) > 0.000001f;

            if (isMoving)
            {
                // Б. "Липкий эффект": Если игрок стоял на земле, добавляем микро-прижатие вниз (-0.001f).
                // Это гарантирует, что при горизонтальном движении поезда CharacterController
                // продолжит считать, что он на земле.
                if (playerCC.isGrounded)
                {
                    finalMove.y -= 0.001f;
                }

                playerCC.Move(finalMove);
                playerTransform.Rotate(0, eulerRot.y, 0);
            }
        }

        // Запоминаем данные для следующего кадра
        _lastPosition = currentPos;
        _lastRotation = currentRot;
    }
}