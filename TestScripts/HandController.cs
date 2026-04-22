using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("Настройки удержания")]
    public Transform holdParent; // Пустышка перед камерой игрока
    public float moveSpeed = 15f;
    public float rotationSpeed = 500f;
    public float throwForce = 500f;

    private GameObject heldObj;
    private Rigidbody heldRb;

    // Свойства для проверки состояния
    public bool IsHolding => heldObj != null;

    void Update()
    {
        if (heldObj != null)
        {
            MoveObject();
            HandleInput();
        }
    }

    private void HandleInput()
    {
        // Бросить предмет (Левая кнопка мыши). 
        // Сработает ТОЛЬКО если мы держим физический объект (IsHolding == true)
        if (Input.GetMouseButtonDown(0) && IsHolding)
        {
            ThrowObject();
        }

        // Вращать предмет (Удерживать правую кнопку мыши)
        if (Input.GetMouseButton(1) && IsHolding)
        {
            RotateObject();
        }
    }

    // Проверяет, держим ли мы конкретно этот предмет (нужно для переноса из рук в инвентарь)
    public bool IsHoldingThis(GameObject obj)
    {
        return heldObj == obj;
    }

    public void PickUpObject(GameObject obj)
    {
        if (heldObj != null) return;

        heldObj = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        if (heldRb != null)
        {
            heldRb.useGravity = false;
            heldRb.drag = 10;
            heldRb.constraints = RigidbodyConstraints.FreezeRotation;
            heldRb.transform.parent = holdParent;
        }
    }

    public void DropObject()
    {
        if (heldObj == null) return;

        if (heldRb != null)
        {
            heldRb.useGravity = true;
            heldRb.drag = 1;
            heldRb.constraints = RigidbodyConstraints.None;
            heldObj.transform.parent = null;
        }

        heldObj = null;
        heldRb = null;
    }

    private void ThrowObject()
    {
        Rigidbody rbToThrow = heldRb;
        DropObject();

        if (rbToThrow != null)
        {
            rbToThrow.AddForce(holdParent.forward * throwForce);
        }
    }

    private void MoveObject()
    {
        // Плавно тянем объект к точке перед камерой
        if (Vector3.Distance(heldObj.transform.position, holdParent.position) > 0.1f)
        {
            Vector3 moveDirection = (holdParent.position - heldObj.transform.position);
            heldRb.AddForce(moveDirection * moveSpeed);
        }
    }

    private void RotateObject()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        heldObj.transform.Rotate(Vector3.up, -mouseX, Space.World);
        heldObj.transform.Rotate(Vector3.right, mouseY, Space.World);
    }
}