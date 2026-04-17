using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Настройки подбора")]
    public string pickupTag = "Pickupable";
    public float pickupDistance = 3f;
    public float throwForce = 10f;
    public Collider[] ignoreColliders;

    [Header("Настройки удержания (SCROLL)")]
    public float targetHoldDistance = 1.0f;
    public float clampMinDistance = 0.5f;
    public float clampMaxDistance = 1.5f;
    [Tooltip("Сила рук.")]
    public float moveSmoothness = 20f;

    [Header("Условия срыва (DROP)")]
    public float autoDropDistance = 5.0f;
    public float autoDropAngle = 30f;
    public float autoDropLag = 3.0f;

    [Header("Управление")]
    public float scrollSensitivity = 1f;
    public float rotationSpeed = 100f;

    // Скрытые переменные
    private Camera playerCamera;
    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private Collider heldObjectCollider;
    private CollisionDetectionMode originalCollisionMode;
    private float currentObjectWeight = 1f;
    private FirstPersonController playerController;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null) Debug.LogError("Камера не найдена!");

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<FirstPersonController>();
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null) playerCollider = player.GetComponent<CharacterController>();

            if (playerCollider != null) ignoreColliders = new Collider[] { playerCollider };
        }
    }

    void Update()
    {
        if (heldObject != null)
        {
            HandleScrolling();

            if (Input.GetMouseButton(1))
            {
                if (playerController != null) playerController.canRotateCamera = false;
                HandleRotation();
            }
            else
            {
                if (playerController != null) playerController.canRotateCamera = true;
            }

            if (Input.GetKeyUp(KeyCode.E) || CheckDropConditions())
            {
                DropObject();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                ThrowObject();
            }
        }
        else
        {
            if (playerController != null && !playerController.canRotateCamera)
            {
                playerController.canRotateCamera = true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickupObject();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldObject != null)
        {
            HoldObject();
        }
    }

    // Автоматическая проверка значений в инспекторе (защита от ошибок ввода)
    void OnValidate()
    {
        if (clampMinDistance < 0.1f) clampMinDistance = 0.1f;
        if (clampMaxDistance < clampMinDistance) clampMaxDistance = clampMinDistance;
        targetHoldDistance = Mathf.Clamp(targetHoldDistance, clampMinDistance, clampMaxDistance);
    }

    void HandleScrolling()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetHoldDistance += scroll * scrollSensitivity;
            // Ограничиваем дистанцию сразу при прокрутке
            targetHoldDistance = Mathf.Clamp(targetHoldDistance, clampMinDistance, clampMaxDistance);
        }
    }

    void HandleRotation()
    {
        float weightFactor = 1f / Mathf.Sqrt(currentObjectWeight);
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * weightFactor * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * weightFactor * Time.deltaTime;

        heldObject.transform.Rotate(Vector3.up, -mouseX, Space.World);
        heldObject.transform.Rotate(playerCamera.transform.right, mouseY, Space.World);
    }

    bool CheckDropConditions()
    {
        if (heldObject == null) return false;

        Vector3 objPos = GetObjectCenter();

        float distToPlayer = Vector3.Distance(playerCamera.transform.position, objPos);
        if (distToPlayer > autoDropDistance) return true;

        Vector3 targetDir = objPos - playerCamera.transform.position;
        float angle = Vector3.Angle(playerCamera.transform.forward, targetDir);
        if (angle > autoDropAngle) return true;

        Vector3 targetPos = playerCamera.transform.position + playerCamera.transform.forward * targetHoldDistance;
        float lag = Vector3.Distance(targetPos, objPos);
        if (lag > autoDropLag) return true;

        return false;
    }

    Vector3 GetObjectCenter()
    {
        if (heldObjectCollider != null) return heldObjectCollider.bounds.center;
        return heldObject.transform.position;
    }

    void TryPickupObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            if (hit.collider.CompareTag(pickupTag))
            {
                PickupObject(hit.collider.gameObject);
            }
        }
    }

    void PickupObject(GameObject obj)
    {
        heldObject = obj;
        heldObjectRb = obj.GetComponent<Rigidbody>();
        heldObjectCollider = obj.GetComponent<Collider>();

        if (heldObjectRb != null)
        {
            float currentDist = Vector3.Distance(playerCamera.transform.position, GetObjectCenter());

            // Устанавливаем начальную дистанцию с учетом ограничений
            targetHoldDistance = Mathf.Clamp(currentDist, clampMinDistance, clampMaxDistance);

            ItemPickup itemPickup = obj.GetComponent<ItemPickup>();
            if (itemPickup != null && itemPickup.itemData != null)
            {
                currentObjectWeight = Mathf.Max(0.1f, itemPickup.itemData.weight);
            }
            else
            {
                currentObjectWeight = Mathf.Max(0.1f, heldObjectRb.mass);
            }

            originalCollisionMode = heldObjectRb.collisionDetectionMode;
            heldObjectRb.useGravity = false;
            heldObjectRb.constraints = RigidbodyConstraints.FreezeRotation;

            if (ignoreColliders != null)
            {
                foreach (var ignoreCollider in ignoreColliders)
                {
                    if (ignoreCollider != null && heldObjectCollider != null)
                        Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, true);
                }
            }
        }
    }

    void HoldObject()
    {
        if (heldObjectRb != null)
        {
            Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * targetHoldDistance;
            Vector3 currentPos = GetObjectCenter();
            Vector3 direction = (targetPosition - currentPos);

            float dynamicSmoothness = moveSmoothness / currentObjectWeight;

            heldObjectRb.velocity = direction * dynamicSmoothness;
        }
    }

    void DropObject()
    {
        if (playerController != null) playerController.canRotateCamera = true;

        if (heldObjectRb != null)
        {
            heldObjectRb.useGravity = true;
            heldObjectRb.constraints = RigidbodyConstraints.None;
            heldObjectRb.collisionDetectionMode = originalCollisionMode;

            if (ignoreColliders != null)
            {
                foreach (var ignoreCollider in ignoreColliders)
                {
                    if (ignoreCollider != null && heldObjectCollider != null)
                        Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, false);
                }
            }
        }

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
        currentObjectWeight = 1f;
    }

    void ThrowObject()
    {
        if (playerController != null) playerController.canRotateCamera = true;

        if (heldObjectRb != null)
        {
            heldObjectRb.useGravity = true;
            heldObjectRb.constraints = RigidbodyConstraints.None;
            heldObjectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            float weightedForce = throwForce / Mathf.Sqrt(currentObjectWeight);
            heldObjectRb.AddForce(playerCamera.transform.forward * weightedForce, ForceMode.Impulse);

            if (ignoreColliders != null)
            {
                foreach (var ignoreCollider in ignoreColliders)
                {
                    if (ignoreCollider != null && heldObjectCollider != null)
                        Physics.IgnoreCollision(heldObjectCollider, ignoreCollider, false);
                }
            }
        }

        heldObject = null;
        heldObjectRb = null;
        heldObjectCollider = null;
        currentObjectWeight = 1f;
    }
}