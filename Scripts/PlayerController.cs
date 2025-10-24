using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Main Settings")]
    public CharacterStats characterStats; // Ссылка на ScriptableObject

    public float lookSpeed = 2.0f;
    public float gravity = -9.81f;
    public float crouchHeight = 0.5f;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float yVelocity = 0f;
    private float originalHeight;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    private float xRotation = 0f;

    [Header("Camera Bobbing")]
    public float cameraBobFrequency = 2f;
    public float cameraBobAmplitude = 0.05f;
    public AudioClip[] footstepSounds;
    public AudioSource audioSource;

    private Vector3 initialCameraPosition;
    private float bobTimer = 0f;
    private bool isStepSoundPlayed = false;

    void Start()
    {
        // Проверка, что статы назначены
        if (characterStats == null)
        {
            Debug.LogError("CharacterStats не назначен в инспекторе! Пожалуйста, добавьте ассет.");
            this.enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("Камера не найдена! Убедитесь, что Main Camera существует или назначьте её вручную.");
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.localPosition;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleCameraBob();
        RotateCamera();
    }

    private void HandleMovement()
    {
        if (controller.isGrounded)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");

            // --- ИЗМЕНЕНИЯ ЗДЕСЬ ---
            // Все значения скорости и прыжка теперь берутся из ассета characterStats
            float currentSpeed = characterStats.speed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= characterStats.sprintSpeedMultiplier;
            }
            if (Input.GetKey(KeyCode.LeftControl))
            {
                controller.height = Mathf.Lerp(controller.height, crouchHeight, Time.deltaTime * 10f);
                currentSpeed *= characterStats.crouchSpeedMultiplier;
            }
            else
            {
                controller.height = Mathf.Lerp(controller.height, originalHeight, Time.deltaTime * 10f);
            }

            moveDirection = transform.right * moveHorizontal + transform.forward * moveVertical;
            moveDirection *= currentSpeed;

            if (Input.GetButtonDown("Jump"))
            {
                yVelocity = Mathf.Sqrt(characterStats.jumpHeight * -2f * gravity);
            }
        }
        else
        {
            moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
        }

        yVelocity += gravity * Time.deltaTime;
        moveDirection.y = yVelocity;

        controller.Move(moveDirection * Time.deltaTime);
    }

    private void HandleCameraBob()
    {
        if (controller.isGrounded && (moveDirection.x != 0 || moveDirection.z != 0))
        {
            float currentBobFrequency = Input.GetKey(KeyCode.LeftShift) ? cameraBobFrequency * 1.5f : cameraBobFrequency;

            bobTimer += Time.deltaTime * currentBobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * cameraBobAmplitude;

            if (Mathf.Sin(bobTimer) < -0.9f && !isStepSoundPlayed)
            {
                PlayFootstepSound();
                isStepSoundPlayed = true;
            }
            else if (Mathf.Sin(bobTimer) > -0.1f)
            {
                isStepSoundPlayed = false;
            }

            cameraTransform.localPosition = initialCameraPosition + new Vector3(0, bobOffset, 0);
        }
        else
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialCameraPosition, Time.deltaTime * 10f);
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            AudioClip footstep = footstepSounds[randomIndex];
            audioSource.PlayOneShot(footstep);
        }
    }

    private void RotateCamera()
    {
        float rotateHorizontal = Input.GetAxis("Mouse X") * lookSpeed;
        float rotateVertical = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(Vector3.up * rotateHorizontal);

        xRotation -= rotateVertical;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}